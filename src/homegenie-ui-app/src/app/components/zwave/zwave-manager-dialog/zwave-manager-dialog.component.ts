import {Component, Inject, OnDestroy, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {Subscription} from 'rxjs';
import {Module} from '../../../services/hgui/module';
import {Adapter} from '../../../adapters/adapter';

class PageId {
  static MANAGEMENT = 0;
  static NODE_DETAILS = 1;
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
      });
    } else {
      // TODO: throw not implemented exception
    }
    /*
    this.moduleEventSubscription = this.adapter.onModuleEvent.subscribe((e) => {
      if (this.isMasterNode(e.module as any) && e.event.Property === 'Controller.Status') {
        if (e.event.Value.startsWith('Added node ')) {
          const node = +e.event.Value.substring(11);
          if (node > 1) {
            this.operationNodeAddress = node;
          }
        } else if (e.event.Value.startsWith('Removed node ')) {
          const node = +e.event.Value.substring(13);
          if (node > 1) {
            this.operationNodeAddress = node;
          }
        } else if (e.event.Value.indexOf('NodeAddStarted') > 0 || e.event.Value.indexOf('NodeRemoveStarted') > 0
          || e.event.Value.indexOf('NodeAddDone') > 0 || e.event.Value.indexOf('NodeRemoveDone') > 0) {
          const sourceNode = +e.event.Value.split(' ')[1];
          if (sourceNode > 1) {
            this.operationNodeAddress = sourceNode;
          }
        }
        const operationStatus = e.event.Value.split(' ').splice(-1)[0];
        switch (operationStatus) {
          case 'Started': // Discovery Started
            break;
          case 'Complete': // Discovery Complete
            break;
          case 'NodeAddFailed': // Node <n> Status NodeAddFailed
          case 'NodeRemoveFailed': // Node <n> Status NodeRemoveFailed
            break;
          case 'NodeAddReady': // Node <n> Status NodeAddReady
          case 'NodeRemoveReady': // Node <n> Status NodeRemoveReady
            this.status = PageStatus.READY;
            this.timeoutStart(this.operationTimeoutSeconds);
            break;
          case 'NodeAddDone': // Node <n> Status NodeAddDone
            // const sourceNode = +e.event.Value.split(' ')[1];
            // if (sourceNode === 1) {
            //   this.timeoutStop();
            // }
            break;
          case 'NodeRemoveDone': // Node <n> Status NodeRemoveDone
            // this.timeoutStop();
            break;
        }
      }
    });
    */
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

  showNodeDetails(module: Module): void {
    this.currentModule = module;
    this.currentPage = PageId.NODE_DETAILS;
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
