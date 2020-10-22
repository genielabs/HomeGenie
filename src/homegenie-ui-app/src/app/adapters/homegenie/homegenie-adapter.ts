import {Subject} from 'rxjs';

import {Adapter} from '../adapter';
import {CMD, HguiService} from 'src/app/services/hgui/hgui.service';
import {
  Module as HguiModule,
  ModuleField, ModuleOptions, ModuleType,
  OptionField
} from 'src/app/services/hgui/module';
import {HomegenieApi, Module, Group, Program, ModuleParameter} from './homegenie-api';
import {HomegenieZwaveAdapter} from './homegenie-zwave-adapter';
import {ZwaveAdapter} from '../zwave-adapter';
import {map} from 'rxjs/operators';
import {Scenario} from "../../services/hgui/automation";

export {Module, Group, Program};

export class ApiResponse {
  code: number;
  response: any;
}
export enum ResponseCode {
  Success = 200
}

export class HomegenieAdapter implements Adapter {
  className = 'HomegenieAdapter';
  translationPrefix = 'HOMEGENIE';
  onModuleEvent = new Subject<{ module: HguiModule, event: any }>();
  private EnableWebsocketStream = true;
  private ImplementedWidgets = [
    'Dimmer',
    'Switch',
    'Light',
    'Siren',
    'Program',
    'Sensor',
    'DoorWindow',
//    "Thermostat"
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
      address = cfg.connection.localRoot ? 'local' : cfg.connection.address + ':' + cfg.connection.port;
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
          if (+status === ResponseCode.Success) {
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
                  let module = this._hgui.getModule(moduleId, this.id);
                  if (module == null) {
                    // add new module to HGUI modules if missing
                    module = this.hgui.addModule(new HguiModule({
                      id: moduleId,
                      adapterId: this.id,
                      type: m.DeviceType.toLowerCase(),
                      name: m.Name,
                      description: m.Description,
                      fields: [],
                    }));
                  }
                  module.field(p.Name, p.Value, p.UpdateTime);
                });
                // TODO: optimize this ^^^^


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

  system(command: string, options?: any): Subject<Scenario> {
    const subject = new Subject<any>();
    switch (command) {
      case CMD.Automation.Scenarios.List:
        this.apiCall(HomegenieApi.Automation.Programs.List)
          .subscribe((res) => {
            this._programs = res.response;
            subject.next(this._programs.filter((p) => {
              if (!p.IsEnabled) return;
              const programModule = this.getModule(`${p.Domain}/${p.Address}`);
              if (!programModule) return;
              const hasProgramWidget = programModule.Properties
                .find((prop) => prop.Name === 'Widget.DisplayModule' && prop.Value === 'homegenie/generic/program');
              if (hasProgramWidget) return p;
            }).map((p) => ({
              id: `${p.Address}`,
              name: p.Name,
              description: p.Description
            }) as Scenario));
            subject.complete();
          });
    }
    return subject;
  }

  control(m: HguiModule, command: string, options?: any): Subject<any> {
    // adapter-specific implementation
    switch (command) {
      case CMD.Options.Get:
        if (m.type === ModuleType.Program) {
          return this.getProgramOptions(m);
        } else {
          return this.getModuleFeatures(m);
        }
      case CMD.Options.Set:
        return this.apiCall(HomegenieApi.Config.Modules.ParameterSet(m), options);
      case CMD.Statistics.Field.Get:
        return this.apiCall(HomegenieApi.Config.Modules.StatisticsGet(m.id, options));
    }

    if (options == null) {
      options = '';
    }
    if (m.type === ModuleType.Program) {
      // program API command
      const programAddress = m.id.substring(m.id.lastIndexOf('/') + 1);
      options = programAddress + '/' + options;
      return this.apiCall(HomegenieApi.Automation.Command(command, options));
    } else {
      // module API command
      return this.apiCall(`${m.id}/${command}/${options}`);
    }
  }

  getModuleIcon(module: HguiModule): string {
    return this.getBaseUrl() + 'hg/html/pages/control/widgets/homegenie/generic/images/unknown.png';
  }

  apiCall(apiMethod: string, postData?: any): Subject<ApiResponse> {
    const oc = this.options.config.connection;
    if (oc == null) {
      return;
    }
    const url = this.getBaseUrl() + `api/${apiMethod}`;
    // TODO: implement a global service logger
    // cp.log.info(url);
    if (postData) {
      return this._hgui.http
        .post<any>(url, postData, {
          // TODO: basic authentication
          headers: {
            'Content-Type' : 'application/json',
            'Cache-Control': 'no-cache'
          //    Authorization: 'Basic ' + btoa(oc.credentials.username + ':' + oc.credentials.password)
          }
        }).pipe(
          // tap(() => console.log('HTTP request executed')),
          map(res => ({code: ResponseCode.Success, response: res}))
        ) as Subject<ApiResponse>;
    }
    return this._hgui.http
      .get(url, {
        // TODO: basic authentication
        headers: {
        //    Authorization: 'Basic ' + btoa(oc.credentials.username + ':' + oc.credentials.password)
        }
      }).pipe(
        // tap(() => console.log('HTTP request executed')),
        map(res => ({code: ResponseCode.Success, response: res}))
      ) as Subject<ApiResponse>;
  }

  reloadModules(): Subject<Array<Module>> {
    const subject = new Subject<Array<Module>>();
    this.apiCall(HomegenieApi.Config.Modules.List)
      .subscribe((res) => {
          const status = res.code;
          const mods: Array<Module> = res.response;
          if (+status === ResponseCode.Success) {
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
                if (!existingModule) {
                  this._modules.push(m);
                } else {
                  existingModule.Properties = m.Properties;
                }

                // TODO: optimize this
                // Export module to HGUI
                let hguiModule = this.hgui.getModule(moduleId, this.id);
                if (hguiModule == null) {
                  // add new module to HGUI modules if missing
                  hguiModule = this.hgui.addModule(new HguiModule({
                    id: moduleId,
                    adapterId: this.id,
                    type: m.DeviceType.toLowerCase(),
                    name: m.Name,
                    description: m.Description,
                    fields: [],
                  }));
                }

                // Update modules fields (hgui fields = hg Properties)
                m.Properties.map((p) => {
                  hguiModule.field(p.Name, p.Value, p.UpdateTime);
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

  getModule(id: string): Module {
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
          let port = 8188; // default port
          const oc = this.options.config.connection;
          if (oc != null && oc.websocketPort) {
            port = oc.websocketPort;
          }
          const r = res.response;
          this.webSocket = new WebSocket(
            `ws://${o.address}:${port}/events?at=${r.ResponseValue}`
          );
          this.webSocket.onopen = (e) => {
            // TODO: not implemented
          };
          this.webSocket.onclose = (e) => {
            setTimeout(this.connectWebSocket.bind(null), 1000);
          };
          this.webSocket.onmessage = (e) => {
            const event = JSON.parse(e.data);
            this.processEvent(event);
          };
          this.webSocket.onerror = (e) => {
            setTimeout(this.connectWebSocket.bind(null), 1000);
          };
        });
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
    return oc.localRoot ? oc.localRoot : `http://${oc.address}:${oc.port}/`;
  }

  private processEvent(event /*: MigEvent*/): void {
    const moduleId = event.Domain + '/' + event.Source;
    const m: HguiModule = this._hgui.getModule(moduleId, this.id);
    this.onModuleEvent.next({module: m, event});
    if (m != null) {
      m.field(event.Property, event.Value, event.UnixTimestamp);
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

  private MatchValues(valueList, matchValue): boolean {
    // regexp matching
    if (valueList.trim().startsWith('/')) {
      valueList = valueList.replace(/^\/+|\/+$/g, '');
      return matchValue.match(valueList);
    }
    // classic comma separated value list matching
    valueList = valueList.toLowerCase();
    matchValue = matchValue.toLowerCase();
    let inclusionList = [valueList];
    if (valueList.indexOf(',') > 0) {
      inclusionList = valueList.split(',');
    } else if (valueList.indexOf('|') > 0) {
      inclusionList = valueList.split('|');
    }
    // build exclusion list and remove empty entries
    const exclusionList = [];
    for (let idx = 0; idx < inclusionList.length; idx++){
      const val = inclusionList[idx];
      if (val.trim().indexOf('!') === 0) {
        inclusionList.splice(idx, 1);
        exclusionList.push(val.trim().substring(1));
      } else if (val.trim().length === 0) {
        inclusionList.splice(idx, 1);
      }
    }
    // check if matching
    let isMatching = (inclusionList.length === 0);
    for (let idx = 0; idx < inclusionList.length; idx++){
      const val = inclusionList[idx];
      if (val.trim() === matchValue.trim()) {
        isMatching = true;
        break;
      }
    }
    // check if not in exclusion list
    for (let idx = 0; idx < exclusionList.length; idx++){
      const val = exclusionList[idx];
      if (val.trim() === matchValue.trim()) {
        isMatching = false;
        break;
      }
    }
    return isMatching;
  }

  private getProgramOptions(m: HguiModule): Subject<ModuleOptions> {
    const subject = new Subject<ModuleOptions>();
    const programModule = this.getModule(m.id);
    if (!programModule) {
      console.log('WARNING', 'No module associated with this program.');
      setTimeout(() => {
        subject.next();
        subject.complete();
      }, 10);
    } else {
      const configOptions = programModule.Properties.filter((p: ModuleParameter) => p.Name.startsWith('ConfigureOptions.'));
      const options: OptionField[] = configOptions.map((o) => {
        const fieldType = o.FieldType.split(':');
        if (!m.field(o.Name)) {
          console.log('WARNING', m, o.Name);
        }
        return {
          pid: programModule.Address,
          name: o.Name,
          field: m.field(o.Name),
          description: o.Description,
          type: {
            id: fieldType[0],
            options: fieldType.slice(1)
          },
        } as OptionField;
      });
      setTimeout(() => {
        subject.next({
          id: programModule.Address,
          name: programModule.Name,
          description: programModule.Description,
          items: options
        });
        subject.complete();
      });
    }
    return subject;
  }

  private getModuleFeatures(m: HguiModule): Subject<ModuleOptions[]> {
    const subject = new Subject<ModuleOptions[]>();
    const module = this.getModule(m.id);
    this.apiCall(HomegenieApi.Automation.Programs.List)
      .subscribe((res) => {
          const programFeatures: ModuleOptions[] = [];
          this._programs = res.response;
          this._programs.map((p) => {
            if (p.IsEnabled && p.Features != null) {
              const pf: ModuleOptions = {
                id: p.Address,
                name: p.Name,
                description: p.Description,
                items: [] as OptionField[]
              };
              for (let i = 0; i < p.Features.length; i++) {
                const f = p.Features[i];
                let matchFeature = this.MatchValues(f.ForDomains, module.Domain);
                let forTypes = f.ForTypes;
                let forProperties: any = false;
                const propertyFilterIndex = forTypes.indexOf(':');
                if (propertyFilterIndex >= 0) {
                  forProperties = forTypes.substring(propertyFilterIndex + 1).trim();
                  forTypes = forTypes.substring(0, propertyFilterIndex).trim();
                }
                matchFeature = matchFeature && this.MatchValues(forTypes, module.DeviceType);
                if (forProperties !== false) {
                  let matchProperty = false;
                  for (let idx = 0; idx < module.Properties.length; idx++) {
                    const mp = module.Properties[idx];
                    if (this.MatchValues(forProperties, mp.Name)) {
                      matchProperty = true;
                      break;
                    }
                  }
                  matchFeature = matchFeature && matchProperty;
                }
                if (matchFeature) {
                  const type = f.FieldType.split(':');
                  let mf: ModuleField = m.field(f.Property);
                  // add the field if does not exist
                  if (mf == null) {
                    mf = {
                      key: f.Property,
                      value: null,
                      timestamp: null
                    };
                    m.fields.push(mf);
                  }
                  pf.items.push({
                    pid: p.Address,
                    field: mf,
                    type: {
                      id: type[0],
                      options: type.slice(1)
                    },
                    name: p.Name,
                    description: f.Description
                  });
                }
              }
              if (pf.items.length > 0) {
                programFeatures.push(pf);
              }
            }
            subject.next(programFeatures);
            subject.complete();
          });
          subject.next();
          subject.complete();
        }
      );
    return subject;
  }
}
