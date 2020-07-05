import {Component, Inject, OnDestroy, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {Subscription} from 'rxjs';
import {Module} from '../../../services/hgui/module';
import {Adapter} from '../../../adapters/adapter';

class PageId {
  static MANAGEMENT = 0;
  static NODE_CONFIG = 1;
  static NODE_ADD = 2;
  static NODE_REMOVE = 3;
}
class PageStatus {
  static READY = 0;
  static REQUEST = 1;
  static SUCCESS = 2;
  static FAILURE = 3;
}

@Component({
  selector: 'app-zwave-manager-dialog',
  templateUrl: './zwave-manager-dialog.component.html',
  styleUrls: ['./zwave-manager-dialog.component.scss']
})
export class ZwaveManagerDialogComponent implements OnInit, OnDestroy {
  isNetworkBusy: boolean;

  currentPage = PageId.MANAGEMENT;
  currentModule: any;
  PageId = PageId;
  PageStatus = PageStatus;
  status = PageStatus.READY;
  operationTimeoutHandle: any;
  operationTimeoutSeconds = 30;
  operationNodeAddress = 0;
  modules: Array<Module> = [];

  private subscriptions: Array<Subscription> = [];

  get translateParams(): any {
    return {
      node: this.operationNodeAddress,
      timeout: (30 - this.operationTimeout)
    };
  }

  private moduleEventSubscription: Subscription;
  private operationTimeout = 0;
  private operationTick = (): void => {
    this.operationTimeout++;
    if (this.operationTimeout > this.operationTimeoutSeconds) {
      this.timeoutStop();
    } else if (this.isNetworkBusy) {
      clearTimeout(this.operationTimeoutHandle);
      this.operationTimeoutHandle = setTimeout(this.operationTick.bind(this), 1000);
    }
  }

  constructor(
    public dialogRef: MatDialogRef<ZwaveManagerDialogComponent>,
    @Inject(MAT_DIALOG_DATA) private adapter: Adapter
  ) {
  }

  ngOnInit(): void {
    const zwaveAdapter = this.adapter.zwaveAdapter;
    if (zwaveAdapter) {
      this.subscriptions.push(zwaveAdapter.onDiscoveryStart.subscribe(() => {
        this.isNetworkBusy = true;
      }));
      this.subscriptions.push(zwaveAdapter.onDiscoveryComplete.subscribe(() => {
        this.isNetworkBusy = false;
      }));
      this.subscriptions.push(zwaveAdapter.onNodeAddReady.subscribe(() => {
        this.status = PageStatus.READY;
        this.timeoutStart(this.operationTimeoutSeconds);
      }));
      this.subscriptions.push(zwaveAdapter.onNodeAddStarted.subscribe((nodeId) => {
        this.operationNodeAddress = nodeId;
      }));
      this.subscriptions.push(zwaveAdapter.onNodeAddDone.subscribe((nodeId) => {
        this.operationNodeAddress = nodeId;
      }));
      this.subscriptions.push(zwaveAdapter.onNodeRemoveReady.subscribe(() => {
        this.status = PageStatus.READY;
        this.timeoutStart(this.operationTimeoutSeconds);
      }));
      this.subscriptions.push(zwaveAdapter.onNodeRemoveStarted.subscribe((nodeId) => {
        this.operationNodeAddress = nodeId;
      }));
      this.subscriptions.push(zwaveAdapter.onNodeRemoveDone.subscribe((nodeId) => {
        this.operationNodeAddress = nodeId;
      }));
      zwaveAdapter.discovery().subscribe((modules) => {
        this.modules = modules;
        modules.map((m) => {
          this.adapter.zwaveAdapter.getDeviceInfo(m).subscribe((info) => {
            if (info) {
              let description = info.deviceDescription;
              try {
                description = this.adapter.zwaveAdapter.getLocaleText(description.description);
                m.description = description;
              } catch (e) {
                // noop
              }
              // brandName, productLine, productName
              // TODO: display device info
            }
          });
        });
      });
    } else {
      // TODO: throw not implemented exception
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.map((s) => s.unsubscribe());
  }

  discovery(e): void {
    if (this.isNetworkBusy) { return; }
    this.adapter.zwaveAdapter.discovery().subscribe((modules) => {
      this.modules = modules;
    });
    this.currentPage = PageId.MANAGEMENT;
  }
  nodeAdd(e): void {
    if (this.isNetworkBusy) { return; }
    this.isNetworkBusy = true;
    this.operationTimeout = 0;
    this.operationNodeAddress = 0;
    this.status = PageStatus.REQUEST;
    this.adapter.zwaveAdapter.addNode().subscribe((res) => {
      this.timeoutStop();
    });
    this.currentPage = PageId.NODE_ADD;
  }
  nodeRemove(e): void {
    if (this.isNetworkBusy) { return; }
    this.isNetworkBusy = true;
    this.operationTimeout = 0;
    this.operationNodeAddress = 0;
    this.status = PageStatus.REQUEST;
    this.adapter.zwaveAdapter.removeNode().subscribe((res) => {
      this.timeoutStop();
    });
    this.currentPage = PageId.NODE_REMOVE;
  }

  retryOperation(e): void {
    if (this.currentPage === PageId.NODE_ADD) {
      this.nodeAdd(e);
    } else {
      this.nodeRemove(e);
    }
  }

  showNodeConfig(module: Module): void {
    this.currentModule = module;
    this.currentPage = PageId.NODE_CONFIG;
  }

  private timeoutStart(seconds: number): void {
    this.isNetworkBusy = true;
    this.operationTick();
  }
  private timeoutStop(): void {
    this.isNetworkBusy = false;
    this.status = (this.operationNodeAddress > 0) ? PageStatus.SUCCESS : PageStatus.FAILURE;
  }
}
