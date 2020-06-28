import { Module } from '../services/hgui/module';
import { Subject } from 'rxjs';

export interface Adapter {
  className: string;
  id: string;
  options: any;
  groups: any;
  modules: any;
  connect(): Subject<any>;
  control(module: Module, command: string, options: any): Subject<any>;
}
