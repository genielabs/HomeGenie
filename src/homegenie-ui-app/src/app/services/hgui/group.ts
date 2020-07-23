export class Group {
  name: string;
  description: string;
  modules: Array<ModuleReference>;
  stats?: any;
}

export class ModuleReference {
  moduleId: string;
  adapterId: string;
}
