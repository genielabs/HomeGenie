import {ZwaveAdapter} from '../../components/zwave/zwave-adapter';
import {Subject, Subscription} from 'rxjs';
import {Module as HguiModule, ModuleField} from '../../services/hgui/module';
import {HomegenieAdapter, Module} from './homegenie-adapter';
import {ZwaveApi, ZwaveConfigParam} from '../../components/zwave/zwave-api';
import {HomegenieZwaveApi} from './homegenie-zwave-api';
import {CommandClass} from '../../components/zwave/zwave-node-config/zwave-node-config.component';

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
              return this.hg.hgui.getModule(moduleId, this.hg.id);
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

  getCommandClasses(module: HguiModule): Subject<Array<CommandClass>> {
    const subject = new Subject<Array<CommandClass>>();
    setTimeout(() => { // force async
      const nif: ModuleField = module.fields.find((f) => f.key === ZwaveApi.fields.NodeInfo);
      if (nif) {
        const nodeInformationFrame: string[] = nif.value.split(' ').slice(3);
        const commandClasses = nodeInformationFrame.map((c) => ({
          id: c,
          description: ZwaveApi.classes[c]
        } as CommandClass));
        commandClasses.sort((a, b) => parseInt(a.id, 16) - parseInt(b.id, 16));
        subject.next(commandClasses);
        subject.complete();
      } else {
        subject.next(null);
        subject.complete();
      }
    });
    return subject;
  }
  getConfigParams(module: HguiModule): Subject<Array<ZwaveConfigParam>> {
    const subject = new Subject<Array<ZwaveConfigParam>>();
    const params: Array<ZwaveConfigParam> = [];
    module.fields.map((f) => {
      const paramPrefix = ZwaveApi.fields.ConfigVariables + '.';
      if (f.key.startsWith(paramPrefix)) {
        const n = f.key.substring(f.key.lastIndexOf('.') + 1);
        params.push({
          number: n,
          name: 'Generic parameter',
          description: 'No specifications available about this parameter.',
          size: null,
          type: { id: 'range', values: { from: 0, to: 65535, description: '' } },
          field: f
        } as ZwaveConfigParam);
      }
    });
    params.sort((a, b) => +a.number - +b.number);
    this.getDeviceInfo(module).subscribe((info) => {
      if (info) {
        info.configParams.configParam.map((cp) => {
          const n = cp['@number'];
          let param = params.find((p) => p.number === n);
          if (param == null) {
            param = new ZwaveConfigParam();
            params.push(param);
          }
          param.number = n;
          param.name = this.getLocaleText(cp.name);
          param.description = this.getLocaleText(cp.description);
          param.size = cp['@size'];
          param.type = { id: cp['@type'], values: { from: 0, to: 65535 } };
          param.field = param.field || new ModuleField();
          if (Array.isArray(cp.value)) {
            param.type.values = cp.value.map((rv) => ({
              from: parseInt(rv['@from'], 16),
              to: parseInt(rv['@to'], 16),
              unit: rv['@unit'],
              description: this.getLocaleText(rv.description)
            }));
          } else {
            param.type.values = {
              from: parseInt(cp.value['@from'], 16),
              to: parseInt(cp.value['@to'], 16),
              unit: cp.value['@unit'],
              description: this.getLocaleText(cp.description)
            };
          }
        });
        params.sort((a, b) => +a.number - +b.number);
      }
      subject.next(params);
      subject.complete();
    });
    return subject;
  }

  getConfigParam(module: HguiModule, parameterId: number): Subject<any> {
    const subject = new Subject<any>();
    // http://localhost:8080/api/HomeAutomation.ZWave/28/Config.ParameterGet/1/?_=1593868554165
    const command = HomegenieZwaveApi.Config.Parameter.Get
      .replace('{{nodeId}}', module.id)
      .replace('{{parameterId}}', parameterId.toString());
    this.hg.apiCall(command)
      .subscribe((res) => {
        subject.next(res);
        subject.complete();
      });
    return subject;
  }
  setConfigParam(module: HguiModule, parameterId: number, parameterValue: number): Subject<any> {
    const subject = new Subject<any>();
    const command = HomegenieZwaveApi.Config.Parameter.Set
      .replace('{{nodeId}}', module.id)
      .replace('{{parameterId}}', parameterId.toString())
      .replace('{{parameterValue}}', parameterValue.toString());
    this.hg.apiCall(command)
      .subscribe((res) => {
        this.getConfigParam(module, parameterId).subscribe((res2) => {
          subject.next(res2);
          subject.complete();
        });
      });
    return subject;
  }

  getDeviceInfo(module: HguiModule): Subject<any> {
    const subject = new Subject<any>();
    if (module == null) {
      subject.next();
      subject.complete();
      return subject;
    }
    let manufacturer: any = module.fields.find((p) => p.key === ZwaveApi.fields.ManufacturerSpecific);
    let version: any = module.fields.find((p) => p.key === ZwaveApi.fields.VersionReport);
    if (manufacturer && version) {
      manufacturer = manufacturer.value.toLowerCase();
      version = JSON.parse(version.value);
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

 getLocaleText(xmlNode: { lang: any }): string {
    // TODO: implement lookup of current language and fallback to 'en'
    if (xmlNode) {
      return xmlNode.lang.find((f) => f['@xml:lang'] === 'en')['#text'];
    }
  }

  private isMasterNode(module: HguiModule): boolean {
    // TODO: make this generic
    return (module == null || (module.id === 'HomeAutomation.ZWave/1'));
  }
}
