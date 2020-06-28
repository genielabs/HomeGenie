export class Group {
  name: string;
  description: string;
  modules: Array<ModuleReference>;
}

export class ModuleReference {
  moduleId: string;
  adapterId: string;
}
