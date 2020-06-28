import { Module } from './module';
import { Group } from './group';

export class Configuration {
  groups: Array<Group>;
  modules: Array<Module>;
  adapters: Array<AdapterConfiguration>;
}
export class AdapterConfiguration {
  id: string;
  type: string;
  config: any;
}
