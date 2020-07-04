import {Subject} from 'rxjs';

import {Adapter} from '../adapter';
import {CMD, HguiService} from 'src/app/services/hgui/hgui.service';
import {Module as HguiModule} from 'src/app/services/hgui/module';
import {HomegenieApi, Module, Group, Program, ModuleParameter} from './homegenie-api';
import {HomegenieZwaveAdapter} from './homegenie-zwave-adapter';
import {ZwaveAdapter} from '../../components/zwave/zwave-adapter';

export {Module, Group, Program};

export class ApiResponse {
  code: number;
  response: any;
}

export class HomegenieAdapter implements Adapter {
  className = 'HomegenieAdapter';
  onModuleEvent = new Subject<{ module: Module, event: any }>();
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
  private _programs: Array<Program> = [];

  private _zwaveAdapter = new HomegenieZwaveAdapter(this);
  get zwaveAdapter(): ZwaveAdapter {
    return this._zwaveAdapter;
  }

  constructor(private _hgui: HguiService) {
  }

  get hgui(): HguiService {
    return this._hgui;
  }

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

  connect(): Subject<any> {
    const subject = new Subject<any>();
    this.apiCall(HomegenieApi.Config.Modules.List)
      .subscribe((res) => {
          const status = res.code;
          const mods: Array<Module> = res.response;
          if (+status === 200) {
            this._modules.length = 0;
            // filter out unsupported modules
            mods.map((m) => {
              if (this.ImplementedWidgets.includes(m.DeviceType)) {
                const domainShort = m.Domain.substring(
                  m.Domain.lastIndexOf('.') + 1
                );
                if (m.Name === '') {
                  m.Name = domainShort + ' ' + m.Address;
                }
                this._modules.push(m);


                // TODO: optimize this

                // update fields of associated HGUI module if exists
                m.Properties.map((p) => {
                  const moduleId = m.Domain + '/' + m.Address;
                  const module = this._hgui.getModule(moduleId, this.id);
                  if (module != null) {
                    this._hgui.updateModuleField(
                      module,
                      p.Name,
                      p.Value,
                      p.UpdateTime
                    );
                  }
                });


              }
            });
            this.apiCall(HomegenieApi.Config.Groups.List)
              .subscribe((listRes) => {
                  const groups: Array<Group> = listRes.response;
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
    // const moduleDetailDialog = zuix.context('module-detail');
    // adapter-specific implementation
    if (command === CMD.Options.Show) {
      // TODO: ... implement something like this
      // if (moduleDetailDialog.isOpen()) {
      //    moduleDetailDialog.close();
      //    return;
      // }
      // TODO: ....
      // hgui.showLoader(true);
      const module = this.getModule(m.id);
      this.apiCall(HomegenieApi.Automation.Programs.List)
        .subscribe((res) => {
            this._programs = res.response;
            this._programs.map((p) => {
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
            // zuix.context('module-detail')
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
      this.apiCall(HomegenieApi.Automation.Command(command, options))
        .subscribe((res) => {
            // TODO: ... cp.log.info(res);
            subject.next();
            subject.complete();
          }
        );
    } else {
      this.apiCall(`${m.id}/${command}/${options}`)
        .subscribe((res) => {
          // TODO: ... cp.log.info(res);
          subject.next();
          subject.complete();
        });
    }
    return subject;
  }

  apiCall(apiMethod): Subject<ApiResponse> {
    const subject = new Subject<ApiResponse>();
    const oc = this.options.config.connection;
    if (oc == null) {
      return;
    }
    const url = this.getBaseUrl() + `api/${apiMethod}`;
    // TODO: implement a global service logger
    // cp.log.info(url);
    this._hgui.http
      .get(url, {
        // TODO: basic authentication
        // headers: {
        //    Authorization: 'Basic ' + btoa(oc.credentials.username + ':' + oc.credentials.password)
        // }
      })
      .subscribe((res) => {
        subject.next({code: 200, response: res});
        subject.complete();
      });
    return subject;
  }

  reloadModules(): Subject<Array<Module>> {
    const subject = new Subject<Array<Module>>();
    this.apiCall(HomegenieApi.Config.Modules.List)
      .subscribe((res) => {
          const status = res.code;
          const mods: Array<Module> = res.response;
          if (+status === 200) {
            this._modules.length = 0;
            // filter out unsupported modules
            mods.map((m) => {
              if (this.ImplementedWidgets.includes(m.DeviceType)) {
                const domainShort = m.Domain.substring(
                  m.Domain.lastIndexOf('.') + 1
                );
                if (m.Name === '') {
                  m.Name = domainShort + ' ' + m.Address;
                }
                const moduleId = this.getModuleId(m);
                const existingModule = this.getModule(moduleId);
                if (existingModule) {
                  // TODO: should update properties
                } else {
                  this._modules.push(m);
                }

                // TODO: optimize this

                let hguiModule = this.hgui.getModule(moduleId, this.id);
                if (hguiModule == null) {
                  // add new module to HGUI modules if missing
                  hguiModule = this.hgui.addModule({
                    id: moduleId,
                    adapterId: this.id,
                    type: m.DeviceType.toLowerCase(),
                    name: m.Name,
                    description: m.Description,
                    fields: [],
                  });
                }

                // Update modules fields (hgui fields = hg Properties)
                m.Properties.map((p) => {
                  this.hgui.updateModuleField(hguiModule, p.Name, p.Value, p.UpdateTime);
                });


              }
            });
            subject.next(this._modules);
          } else {
            subject.error(status);
          }
        }
      );
    return subject;
  }

  getModuleId(module: Module): string {
    return `${module.Domain}/${module.Address}`;
  }

  protected getModule(id: string): Module {
    const matchingModules = this._modules.filter(
      (i) => this.getModuleId(i) === id
    );
    if (matchingModules.length === 1) {
      return matchingModules[0];
    }
  }

  private connectWebSocket(): void {
    if (this.webSocket != null) {
      this.webSocket.onclose = null;
      this.webSocket.onerror = null;
      this.webSocket.close();
    }
    const o = this.options.config.connection;
    this.apiCall(HomegenieApi.Config.WebSocket.GetToken)
      .subscribe((res) => {
          const r = res.response;
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
      } catch (e) {
      }
      setTimeout(this.connectEventSource.bind(null), 1000);
      // TODO: implement a global service logger
      // cp.log.info('Reconnecting to HomeGenie SSE on ' + getBaseUrl());
    }
    es.onopen = (e) => {
      // TODO: implement a global service logger
      // cp.log.info('SSE connect');
    };
    es.onerror = (e) => {
      // TODO: implement a global service logger
      // cp.log.error('SSE error');
      es.close();
      es = this.eventSource = null;
      setTimeout(this.connectEventSource.bind(null), 1000);
    };
    es.onmessage = (e) => {
      const event = JSON.parse(e.data);
      // TODO: implement a global service logger
      // cp.log.info('SSE data', event);
      this.processEvent(event);
    };
  }

  private getBaseUrl(): string {
    const oc = this.options.config.connection;
    if (oc == null) {
      // TODO: report 'connector not configured' error and exit
      return;
    }
    return `http://${oc.address}:${oc.port}/`;
  }

  private processEvent(event): void {
    const moduleId = event.Domain + '/' + event.Source;
    const m = this._hgui.getModule(moduleId, this.id);
    this.onModuleEvent.next({module: m, event});
    if (m != null) {
      this._hgui.updateModuleField(
        m,
        event.Property,
        event.Value,
        event.UnixTimestamp
      );
    }
    // update local hg-module
    const module = this._modules.find((mod) => mod.Domain === event.Domain && mod.Address === event.Source);
    if (module) {
      let property: ModuleParameter = module.Properties.find((p) => p.Name === event.Property);
      if (property == null) {
        property = {
          Name: event.Property,
          Value: event.Value,
          UpdateTime: event.UnixTimestamp
        };
        module.Properties.push(property);
      } else {
        property.Value = event.Value;
        property.UpdateTime = event.UnixTimestamp;
      }
    }
  }
}
