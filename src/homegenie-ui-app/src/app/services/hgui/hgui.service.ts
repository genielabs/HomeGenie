import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Subject } from 'rxjs';
import { StorageMap } from '@ngx-pwa/local-storage';

import { Adapter } from '../../adapters/adapter';
import AdapterFactory from '../../adapters/adapter-factory';

import { Configuration, AdapterConfiguration } from './configuration';
import { Group, ModuleReference } from './group';
import { Module, ModuleField } from './module';

export class CMD {
  static Control = {
    On: 'Control.On',
    Off: 'Control.Off',
    Level: 'Control.Level',
    Toggle: 'Control.Toggle'
  };
  static Drivers = {
    List: 'Drivers.List'
  };
  static Options = {
    Show: 'Options.Show'
  };
  static Programs = {
    Toggle: 'Programs.Toggle'
  };
}
export class FLD {
  static Meter = {
    Watts: 'Meter.Watts',
  };
  static Sensor = {
    Humidity: 'Sensor.Humidity',
    Luminance: 'Sensor.Luminance',
    Temperature: 'Sensor.Temperature',
  };
  static Status = {
    Level: 'Status.Level',
  };
  static Program = {
    Status: 'Program.Status',
    Error: 'Program.Error',
  };
}

@Injectable({
  providedIn: 'root',
})
export class HguiService implements OnDestroy {
  adapters: Array<Adapter> = [];
  groups: Array<Group> = [];
  modules: Array<Module> = [];

  currentGroup = 0;

  onModuleAdded = new Subject<Module>();
  onModuleRemoved = new Subject<Module>();
  onGroupAdded = new Subject<Group>();
  onGroupRemoved = new Subject<Group>();
  onGroupModuleAdded = new Subject<{ group: Group; module: Module }>();
  onAdapterAdded = new Subject<Adapter>();

  private configStorage = 'config';

  constructor(public storage: StorageMap, public http: HttpClient) {}

  ngOnDestroy() {
    this.saveConfiguration();
    this.onModuleAdded.complete();
    this.onModuleRemoved.complete();
    this.onGroupAdded.complete();
    this.onGroupRemoved.complete();
    this.onGroupModuleAdded.complete();
    this.onAdapterAdded.complete();
  }
  /**
   * Loads HGUI configuration
   */
  loadConfiguration(): Subject<Configuration> {
    const subject = new Subject<Configuration>();
    this.storage
      .get<Configuration>(this.configStorage)
      .subscribe((config: Configuration) => {
        if (config != null) {
          this.groups = config.groups;
          this.modules = [];
          config.modules.map((m) => {
            this.modules.push(m);
            this.onModuleAdded.next(m);
          });
          this.groups.map((g) => {
            this.onGroupAdded.next(g);
            g.modules.map((mr) => {
              this.onGroupModuleAdded.next({
                group: g,
                module: this.modules.find((m) => m.id === mr.moduleId),
              });
            });
          });
        }
        // Connect adapters
        if (config && config.adapters) {
          config.adapters.map((ac) => {
            const adapter = this.getAdapter(ac.id, ac.type);
            adapter.options.config = ac.config;
            adapter.connect().subscribe((status) => {
              if (status) {
                // TODO: handle adapter connect error
              } else {
                // TODO: log 'adapter connected'
              }
              this.onAdapterAdded.next(adapter);
            });
          });
          subject.next(config);
        } else {
          subject.next(null);
        }
        subject.complete();
      });
    return subject;
  }
  /**
   * Saves HGUI configuration
   */
  saveConfiguration(): Subject<Configuration> {
    const subject = new Subject<Configuration>();
    const adaptersConfig = [];
    this.adapters.map((adapter) => {
      const ac: AdapterConfiguration = {
        id: adapter.id,
        type: adapter.className,
        config: adapter.options.config,
      };
      adaptersConfig.push(ac);
    });
    const config: Configuration = {
      groups: this.groups,
      modules: this.modules,
      adapters: adaptersConfig,
    };
    this.storage.set(this.configStorage, config).subscribe((status) => {
      // TODO: test status value
      subject.next(config);
      subject.complete();
    });
    return subject;
  }
  /**
   * Adds a new adapter
   * @param adapter The new HGUI adapter
   */
  addAdapter(adapter: Adapter): boolean {
    if (this.adapters.find((a) => a.id === adapter.id) != null) {
      return false;
    }
    this.adapters.push(adapter);
    this.onAdapterAdded.next(adapter);
    return true;
  }
  /**
   * Gets the adapter with `id` = `adapterId` or creates a new one of type `className`
   * @param adapterId The adapter id
   * @param className The adapter class name
   */
  getAdapter(adapterId: string, className?: string): Adapter {
    let adapter = this.adapters.find((a) => a.id === adapterId);
    // create a new instance if 'adapterId' was not found and a 'typeName' was given
    if (className != null && adapter == null) {
      adapter = AdapterFactory.create(className, this);
      //this.addAdapter(adapter);
    }
    return adapter;
  }
  /**
   * Gets the list of adapters in the actual HGUI configuration
   */
  getAdapters(): Array<Adapter> {
    return this.adapters;
  }

  /**
   * Gets the current group instance
   */
  getCurrentGroup(): Group {
    return this.groups[this.currentGroup];
  }
  /**
   * Sets the current group
   * @param groupIndex The group index
   */
  setCurrentGroup(groupIndex: number): void {
    this.currentGroup = groupIndex;
  }
  /**
   * Adds a new group to HGUI configuration
   * @param name The group name
   */
  addGroup(name): Group {
    let group = this.getGroup(name);
    if (group != null) return group;
    group = new Group();
    group.name = name;
    group.modules = [];
    this.groups.push(group);
    this.onGroupAdded.next(group);
    return group;
  }
  /**
   * Gets the group with the specified name
   * @param name The group name
   */
  getGroup(name: string): Group {
    return this.groups.find((item) => item.name === name);
  }
  /**
   * Removes the group with the specified name
   * @param name The group name
   */
  removeGroup(name: string): boolean {
    const group = this.getGroup(name);
    if (group != null) {
      this.groups = this.groups.filter((item) => item.name !== name);
      this.onGroupRemoved.next(group);
      return true;
    }
    return false;
  }
  /**
   * Checks if a group with the specified name exists in HGUI configuration
   * @return true if the group exists, false otherwise
   */
  hasGroup(name: string): boolean {
    return this.getGroup(name) != null;
  }
  /**
   * Adds a module to the given group
   * @param group The group object
   * @param m The module to add
   */
  addGroupModule(group: Group, m: Module): boolean {
    if (group.modules.find((em) => em.moduleId === m.id) != null) {
      // module already added
      return false;
    }
    const moduleReference: ModuleReference = {
      moduleId: m.id,
      adapterId: m.adapterId,
    };
    group.modules.push(moduleReference);
    this.onGroupModuleAdded.next({ group, module: m });
    return true;
  }

  /**
   * Adds a new module to HGUI configutation
   * @param module The module to add
   */
  addModule(module: Module): Module {
    const m = this.getModule(module.id, module.adapterId);
    if (m != null) return m;
    // TODO: module = zuix.observable(module).proxy;
    // TODO: subscribe to module events
    this.modules.push(module);
    this.onModuleAdded.next(module);
    return module;
  }
  /**
   * Gets a module given its id and adapter id
   * @param moduleId The module id
   * @param adapterId The adapter id
   */
  getModule(moduleId: string, adapterId: string): any {
    return this.modules.find(
      (item) => item.id === moduleId && item.adapterId === adapterId
    );
  }
  /**
   * Removes a module
   * @param module The module to remove
   */
  removeModule(module: Module): void {
    this.modules = this.modules.filter((item) => item !== module);
    this.onModuleRemoved.next(module);
  }
  /**
   * Checks if a module with the specified `moduleId` and `adapterId` exists in HGUI configuration
   * @return true if the module exists, false otherwise
   */
  hasModule(moduleId: string, adapterId: string): boolean {
    return this.getModule(moduleId, adapterId) != null;
  }
  /**
   * Gets a module field
   * @param module The module to get the field from
   * @param fieldKey The field key identifier
   */
  getModuleField(module: Module, fieldKey: string): ModuleField {
    if (module.fields == null) return null;
    return module.fields.find((f) => f.key === fieldKey);
  }
  /**
   * Updates a module field value and timestamp. If the specified field does not exists, the a new field is added.
   * @param module The module object
   * @param fieldKey The field key identifier
   * @param value The new field value
   * @param timestamp The updated timestamp
   */
  updateModuleField(
    module: Module,
    fieldKey: string,
    value: any,
    timestamp: number
  ): any {
    if (module.fields == null) module.fields = [];
    let field = this.getModuleField(module, fieldKey);
    if (field != null && field.timestamp === timestamp) {
      return;
    } else if (field == null) {
      field = { key: fieldKey };
      module.fields.push(field);
    }
    field.value = value;
    field.timestamp = timestamp;
  }

  /**
   * Gets the list of modules in actual HGUI configuration
   */
  getModules(): Array<Module> {
    return this.modules;
  }

  isBusy(): boolean {
    return false;
  }
}
