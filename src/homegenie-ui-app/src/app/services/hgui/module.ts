export class Module {
  adapterId: string;
  id: string; // id ::= '<domain>:<address>';
  type: string;
  name: string;
  description: string;
  fields: Array<ModuleField>;
}

export class ModuleField {
  key: string;
  value?: any;
  timestamp?: number = 0;
}
