export class Module {
  private dataStore?: any = {};
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
  /**
   * Gets the module field matching the given key or set it if the 'value' parameter was passed.
   * @param key The field key identifier
   * @param value? The value to set
   * @param timestamp? The optional unix timestamp (UTC)
   */
  field?(key: string, value?: any, timestamp?: any): any {
    if (timestamp == null) { timestamp = new Date().getTime(); }
    const field = this.fields.find((f) => f.key === key);
    if (field && value) {
      if (this.fields == null) { this.fields = []; }
      if (field.timestamp === timestamp) {
        return this;
      }
      field.value = value;
      field.timestamp = timestamp;
      return this;
    } else if (field == null && value) {
      this.fields.push({ key, value, timestamp });
      return this;
    }
    return field;
  }
  /**
   * Gets the module data matching the given key or set it if the 'data' parameter was passed.
   * @param key The field key identifier
   * @param data? The data to set
   */
  data?(key: string, data?: any): any {
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
