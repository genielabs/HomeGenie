import { Adapter } from '../adapter';
import { HguiService, CMD, FLD } from 'src/app/services/hgui/hgui.service';
import { Module as HguiModule } from 'src/app/services/hgui/module';
import { Subject } from 'rxjs';

class Group {
  Name: string;
  Wallpaper: string;
  Modules: Array<{ Domain: string; Address: string }>;
}
class Module {
  Domain: string;
  Address: string;
  Name: string;
  DeviceType: string;
  Properties: Array<ModuleParameter>;
}
class ModuleParameter {
  Name: string;
  Value: any;
  UpdateTime: number;
}
class Program {
  // TODO: ...
}

export class HomegenieAdapter implements Adapter {
  private EnableWebsocketStream = true;
  private ImplementedWidgets = [
    'Dimmer',
    'Switch',
    'Light',
    'Siren',
    'Program',
    'Sensor',
    'DoorWindow',
  ];
  private eventSource;
  private webSocket;

  className = 'HomegenieAdapter';
  constructor(private hgui: HguiService) {}

  private _id: string;
  get id(): string {
    let address = '0.0.0.0';
    const cfg = this.options.config;
    if (cfg != null && cfg.connection != null) {
      address = cfg.connection.address + ':' + cfg.connection.port;
    }
    return address;
  }

  private _options = {};
  get options(): any {
    return this._options;
  }
  set options(opts: any) {
    this._options = opts;
  }

  private _groups: Array<Group> = [];
  get groups(): any {
    return this._groups;
  }

  private _modules: Array<Module> = [];
  get modules(): any {
    return this._modules;
  }

  private _programs: Array<Program> = [];

  connect(): Subject<any> {
    const subject = new Subject<any>();
    this.apiCall(
      'HomeAutomation.HomeGenie/Config/Modules.List',
      (status, mods: Array<Module>) => {
        if (status == 200) {
          // filter out unsupported modules
          mods.map((m) => {
            if (this.ImplementedWidgets.includes(m.DeviceType)) {
              const domainShort = m.Domain.substring(
                m.Domain.lastIndexOf('.') + 1
              );
              if (m.Name == '') m.Name = domainShort + ' ' + m.Address;
              this._modules.push(m);
              // update fields of associated HGUI module
              m.Properties.map((p) => {
                const moduleId = m.Domain + '/' + m.Address;
                const module = this.hgui.getModule(moduleId, this.id);
                if (module != null) {
                  this.hgui.updateModuleField(
                    module,
                    p.Name,
                    p.Value,
                    p.UpdateTime
                  );
                }
              });
            }
          });
          this.apiCall(
            'HomeAutomation.HomeGenie/Config/Groups.List',
            (status, groups: Array<Group>) => {
              this._groups = groups;
              // finally connect to the real-time event stream
              if (this.EnableWebsocketStream) {
                this.connectWebSocket();
              } else {
                this.connectEventSource();
              }
              subject.next();
            }
          );
        } else {
          subject.next(status);
        }
      }
    );
    return subject;
  }

  control(m: HguiModule, command: string, options: any): Subject<any> {
    const subject = new Subject<any>();
    //const moduleDetailDialog = zuix.context('module-detail');
    // adapter-specific implementation
    if (command === CMD.Options.Show) {
      // TODO: ... implement something like this
      //if (moduleDetailDialog.isOpen()) {
      //    moduleDetailDialog.close();
      //    return;
      //}
      // TODO: ....
      //hgui.showLoader(true);
      let module = this.getModule(m.id);
      this.apiCall(
        'HomeAutomation.HomeGenie/Automation/Programs.List',
        (status, res) => {
          this._programs = res;
          res.map((p) => {
            if (p.IsEnabled && p.Features != null) {
              for (let i = 0; i < p.Features.length; i++) {
                const f = p.Features[i];
                f.ForTypes = f.ForTypes.replace('|', ',');
                f.ForDomains = f.ForDomains.replace('|', ',');
                let matchFeature =
                  f.ForTypes.length === 0 ||
                  `,${f.ForTypes},`.indexOf(`,${module.DeviceType},`) >= 0;
                matchFeature =
                  matchFeature &&
                  (f.ForDomains.length === 0 ||
                    `,${f.ForDomains},`.indexOf(`,${module.Domain},`) >= 0);
                if (matchFeature) {
                  // TODO: ....
                  /*
                                zuix.load('adapters/homegenie/options_view', {
                                    model: {
                                        name: p.Name,
                                        description: p.Description
                                    },
                                    ready: (ctx) => {
                                        zuix.context('module-detail').addOptionsView(ctx.view());
                                    }
                                });
                                */
                  break;
                }
              }
            }
            // TODO: hgui.hideLoader();
          });
          // show module options and statistics page
          // TODO: ...
          //zuix.context('module-detail')
          //    .open(options.view);
          subject.next();
          subject.complete();
        }
      );
      return subject;
    }
    if (m.type === 'program') {
      const programAddress = m.id.substring(m.id.lastIndexOf('/') + 1);
      options = programAddress + '/' + options;
      this.apiCall(
        `HomeAutomation.HomeGenie/Automation/${command}/${options}`,
        (code, res) => {
          // TODO: ... cp.log.info(code, res);
          subject.next();
          subject.complete();
        }
      );
    } else {
      this.apiCall(`${m.id}/${command}/${options}`, (code, res) => {
        // TODO: ... cp.log.info(code, res);
        subject.next();
        subject.complete();
      });
    }
    return subject;
  }

  private getModuleId(module: Module) {
    return `${module.Domain}/${module.Address}`;
  }

  private getModule(id: string) {
    const matchingModules = this._modules.filter(
      (i) => this.getModuleId(i) === id
    );
    if (matchingModules.length === 1) return matchingModules[0];
  }

  private connectWebSocket(): void {
    if (this.webSocket != null) {
      this.webSocket.onclose = null;
      this.webSocket.onerror = null;
      this.webSocket.close();
    }
    const o = this.options.config.connection;
    this.apiCall(
      'HomeAutomation.HomeGenie/Config/WebSocket.GetToken',
      (code, res) => {
        const r = res;
        console.log(`ws://${o.address}:8188/events?at=${r.ResponseValue}`);
        this.webSocket = new WebSocket(
          `ws://${o.address}:8188/events?at=${r.ResponseValue}`
        );
        this.webSocket.onopen = (e) => {
          // TODO: implement a global service logger
          // cp.log.info('WebSocket connected.');
          console.log('Websocket connected');
        };
        this.webSocket.onclose = (e) => {
          // TODO: implement a global service logger
          // cp.log.error('WebSocket closed.', e);
          setTimeout(this.connectWebSocket.bind(null), 1000);
        };
        this.webSocket.onmessage = (e) => {
          const event = JSON.parse(e.data);
          // TODO: implement a global service logger
          // cp.log.info('WebSocket data', event);
          this.processEvent(event);
        };
        this.webSocket.onerror = (e) => {
          // TODO: implement a global service logger
          // cp.log.error('WebSocket error.', e);
          setTimeout(this.connectWebSocket.bind(null), 1000);
        };
      }
    );
  }

  private connectEventSource(): void {
    let es = this.eventSource;
    if (es == null) {
      es = this.eventSource = new EventSource(this.getBaseUrl() + 'events');
    } else {
      try {
        es.close();
        es = this.eventSource = null;
      } catch (e) {}
      setTimeout(this.connectEventSource.bind(null), 1000);
      // TODO: implement a global service logger
      //cp.log.info('Reconnecting to HomeGenie SSE on ' + getBaseUrl());
    }
    es.onopen = (e) => {
      // TODO: implement a global service logger
      //cp.log.info('SSE connect');
    };
    es.onerror = (e) => {
      // TODO: implement a global service logger
      //cp.log.error('SSE error');
      es.close();
      es = this.eventSource = null;
      setTimeout(this.connectEventSource.bind(null), 1000);
    };
    es.onmessage = (e) => {
      const event = JSON.parse(e.data);
      // TODO: implement a global service logger
      //cp.log.info('SSE data', event);
      this.processEvent(event);
    };
  }

  private getBaseUrl() {
    const oc = this.options.config.connection;
    if (oc == null) {
      // TODO: report 'connector not configured' error and exit
      return;
    }
    return `http://${oc.address}:${oc.port}/`;
  }

  private apiCall(apiMethod, callback) {
    console.log(this.options);
    const oc = this.options.config.connection;
    if (oc == null) return;
    const url = this.getBaseUrl() + `api/${apiMethod}`;
    // TODO: implement a global service logger
    //cp.log.info(url);
    this.hgui.http
      .get(url, {
        // TODO: basic authentication
        //headers: {
        //    Authorization: 'Basic ' + btoa(oc.credentials.username + ':' + oc.credentials.password)
        //}
      })
      .subscribe((res) => {
        callback(200, res);
      });
  }

  private processEvent(event) {
    const moduleId = event.Domain + '/' + event.Source;
    const m = this.hgui.getModule(moduleId, this.id);
    console.log('Module event', m);
    if (m != null) {
      this.hgui.updateModuleField(
        m,
        event.Property,
        event.Value,
        event.UnixTimestamp
      );
    }
  }
}
