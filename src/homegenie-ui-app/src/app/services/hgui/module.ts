export class Module {
  private dataStore: any = {};
  adapterId: string;
  id: string; // id ::= '<domain>:<address>';
  type: string;
  name: string;
  description: string;
  fields: Array<ModuleField>;
  constructor(moduleData: Module) {
    this.adapterId = moduleData.adapterId;
    this.id = moduleData.id;
    this.type = moduleData.type;
    this.name = moduleData.name;
    this.description = moduleData.description;
    this.fields = moduleData.fields;
  }
  data(key: string, data?: any): any {
    if (data) {
      this.dataStore[key] = data;
      return this;
    }
    return this.dataStore[key];
  }
}

export class ModuleField {
  key: string;
  value?: any;
  timestamp = 0;
}
