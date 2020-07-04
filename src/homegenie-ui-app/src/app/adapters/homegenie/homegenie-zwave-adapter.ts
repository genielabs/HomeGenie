import {ZwaveAdapter} from '../../components/zwave/zwave-adapter';
import {Subject, Subscription} from 'rxjs';
import {Module as HguiModule} from '../../services/hgui/module';
import {HomegenieAdapter, Module} from './homegenie-adapter';
import {ZwaveApi} from '../../components/zwave/zwave-api';
import {HomegenieZwaveApi} from './homegenie-zwave-api';

export class HomegenieZwaveAdapter implements ZwaveAdapter {
  onDiscoveryComplete = new Subject<any>();
  onDiscoveryStart = new Subject<any>();

  onNodeAddReady = new Subject<any>();
  onNodeAddStarted = new Subject<any>();
  onNodeAddDone = new Subject<any>();

  onNodeRemoveReady = new Subject<any>();
  onNodeRemoveStarted = new Subject<any>();
  onNodeRemoveDone = new Subject<any>();

  private moduleEventSubscription: Subscription;

  constructor(private hg: HomegenieAdapter) {
    this.moduleEventSubscription = this.hg.onModuleEvent.subscribe((e) => {
      if (this.isMasterNode(e.module as any) && e.event.Property === 'Controller.Status') {
        if (e.event.Value.startsWith('Added node ')) {
          const node = +e.event.Value.substring(11);
          if (node > 1) {
            this.onNodeAddDone.next(node);
          }
        } else if (e.event.Value.startsWith('Removed node ')) {
          const node = +e.event.Value.substring(13);
          if (node > 1) {
            this.onNodeRemoveDone.next(node);
          }
        } else if (e.event.Value.indexOf('NodeAddStarted') > 0 || e.event.Value.indexOf('NodeRemoveStarted') > 0
          || e.event.Value.indexOf('NodeAddDone') > 0 || e.event.Value.indexOf('NodeRemoveDone') > 0) {
          const sourceNode = +e.event.Value.split(' ')[1];
          if (sourceNode > 1) {
            if (e.event.Value.indexOf('NodeAdd') > 0) {
              this.onNodeAddStarted.next(sourceNode);
            } else {
              this.onNodeRemoveStarted.next(sourceNode);
            }
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
            this.onNodeAddReady.next();
            break;
          case 'NodeRemoveReady': // Node <n> Status NodeRemoveReady
            this.onNodeRemoveReady.next();
            break;
          case 'NodeAddDone': // Node <n> Status NodeAddDone
            break;
          case 'NodeRemoveDone': // Node <n> Status NodeRemoveDone
            break;
        }
      }
    });
  }

  // TODO: where should this 'this.moduleEventSubscription' be unsubscribed?

  discovery(): Subject<Array<HguiModule>> {
    this.onDiscoveryStart.next();
    const subject = new Subject<Array<HguiModule>>();
    this.hg.apiCall(HomegenieZwaveApi.Master.Controller.Discovery)
      .subscribe((res) => {
        this.hg.reloadModules().subscribe((modules) => {
          const zwaveModules: Array<HguiModule> = modules.map((m) => {
            if (m.Domain === 'HomeAutomation.ZWave') {
              const moduleId = this.hg.getModuleId(m);
              const hguiModule = this.hg.hgui.getModule(moduleId, this.hg.id);
              this.getDeviceInfo(m).subscribe((info) => {
                if (info) {
                  let description = info.deviceDescription;
                  try {
                    description = description.description.lang.find((f) => f['@xml:lang'] === 'en')['#text'];
                    m.Description = description;
                    hguiModule.description = description;
                  } catch (e) {
                    // noop
                  }
                  console.log(hguiModule);
                  // brandName, productLine, productName
                  // TODO: display device info
                }
              });
              return hguiModule;
            }
          });
          subject.next(zwaveModules);
          subject.complete();
          this.onDiscoveryComplete.next();
        });
      });
    return subject;
  }

  getNode(id: string): HguiModule {
    return undefined;
  }

  listNodes(): Array<HguiModule> {
    return undefined;
  }

  addNode(): Subject<any> {
    const subject = new Subject<any>();
    this.hg.apiCall(HomegenieZwaveApi.Master.Controller.NodeAdd)
      .subscribe((res) => {
        subject.next();
        subject.complete();
      });
    return subject;
  }

  removeNode(): Subject<any> {
    const subject = new Subject<any>();
    this.hg.apiCall(HomegenieZwaveApi.Master.Controller.NodeRemove)
      .subscribe((res) => {
        subject.next();
        subject.complete();
      });
    return subject;
  }

  private isMasterNode(module: HguiModule): boolean {
    // TODO: make this generic
    return (module == null || (module.id === 'HomeAutomation.ZWave/1'));
  }
  private getDeviceInfo(module: Module): Subject<any> {
    const subject = new Subject<any>();
    let nodeInfo: any = module.Properties.find((p) => p.Name === ZwaveApi.fields.NodeInfo);
    let manufacturer: any = module.Properties.find((p) => p.Name === ZwaveApi.fields.ManufacturerSpecific);
    let version: any = module.Properties.find((p) => p.Name === ZwaveApi.fields.VersionReport);
    if (nodeInfo && manufacturer && version) {
      nodeInfo = manufacturer.Value.split(' ');
      manufacturer = manufacturer.Value.toLowerCase();
      version = JSON.parse(version.Value);
      const applicationVersion = ('00' + version.ApplicationVersion).slice (-2) + '.' + ('00' + version.ApplicationSubVersion).slice (-2);
      this.hg.apiCall(`${HomegenieZwaveApi.Master.Db.GetDevice}/${manufacturer}/${applicationVersion}`)
        .subscribe((res) => {
          let deviceInfo = JSON.parse(res.response.ResponseValue)[0];
          if (deviceInfo) {
            deviceInfo = deviceInfo.ZWaveDevice;
          }
          subject.next(deviceInfo);
          subject.complete();
        });
    } else {
      subject.next(null);
      subject.complete();
    }
    return subject;
  }
}
