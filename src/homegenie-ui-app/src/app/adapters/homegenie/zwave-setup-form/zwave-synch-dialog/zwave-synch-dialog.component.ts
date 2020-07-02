import {Component, Inject, OnDestroy, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {HomegenieAdapter, Module} from '../../homegenie-adapter';
import {ZWaveApi} from '../../homegenie-api';
import {Subscription} from 'rxjs';

class PageId {
  static MANAGEMENT = 0;
  static NODE_ADD = 1;
  static NODE_REMOVE = 2;
}
class PageStatus {
  static READY = 0;
  static REQUEST = 1;
  static SUCCESS = 2;
  static FAILURE = 3;
}

@Component({
  selector: 'app-zwave-synch-dialog',
  templateUrl: './zwave-synch-dialog.component.html',
  styleUrls: ['./zwave-synch-dialog.component.scss']
})
export class ZwaveSynchDialogComponent implements OnInit, OnDestroy {
  isNetworkBusy: boolean;

  currentPage = PageId.MANAGEMENT;
  PageId = PageId;
  PageStatus = PageStatus;
  status = PageStatus.READY;
  operationTimeoutHandle: any;
  operationTimeoutSeconds = 30;
  operationNodeAddress = 0;
  modules: Array<Module> = [];

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
    public dialogRef: MatDialogRef<ZwaveSynchDialogComponent>,
    @Inject(MAT_DIALOG_DATA) private adapter: HomegenieAdapter
  ) {
  }

  ngOnInit(): void {
    this.moduleEventSubscription = this.adapter.onModuleEvent.subscribe((e) => {
      if (this.isMasterNode(e.module) && e.event.Property === 'Controller.Status') {
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
            this.isNetworkBusy = true;
            break;
          case 'Complete': // Discovery Complete
            this.isNetworkBusy = false;
            break;
          case 'NodeAddFailed': // Node <n> Status NodeAddFailed
          case 'NodeRemoveFailed': // Node <n> Status NodeRemoveFailed
            // this.timeoutStop();
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
    this.discovery(null);
  }

  ngOnDestroy(): void {
    this.moduleEventSubscription.unsubscribe();
  }

  discovery(e): void {
    this.isNetworkBusy = true;
    this.adapter.apiCall(ZWaveApi.Master.Controller.Discovery)
      .subscribe((res) => {
        this.adapter.reloadModules().subscribe(() => {
          this.modules = this.adapter.modules.map((m) => {
            if (m.Domain === 'HomeAutomation.ZWave') {
              return m;
            }
          });
          this.isNetworkBusy = false;
        });
        // TODO: ...
        console.log(res);
      });
    this.currentPage = PageId.MANAGEMENT;
  }
  nodeAdd(e): void {
    this.isNetworkBusy = true;
    this.operationTimeout = 0;
    this.operationNodeAddress = 0;
    this.status = PageStatus.REQUEST;
    this.adapter.apiCall(ZWaveApi.Master.Controller.NodeAdd)
      .subscribe((res) => {
        this.timeoutStop();
      });
    this.currentPage = PageId.NODE_ADD;
  }
  nodeRemove(e): void {
    this.isNetworkBusy = true;
    this.operationTimeout = 0;
    this.operationNodeAddress = 0;
    this.status = PageStatus.REQUEST;
    this.adapter.apiCall(ZWaveApi.Master.Controller.NodeRemove)
      .subscribe((res) => {
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

  private isMasterNode(module: Module): boolean {
    return (module == null || (module.Domain === 'HomeAutomation.ZWave' && module.Address === '1'));
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
