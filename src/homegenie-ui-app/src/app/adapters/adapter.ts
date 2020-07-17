import {Module as HguiModule, Module} from '../services/hgui/module';
import {Subject} from 'rxjs';
import {CMD, HguiService} from '../services/hgui/hgui.service';
import {ZwaveAdapter} from './zwave-adapter';

export interface Adapter {
  hgui: HguiService;
  className: string;
  id: string;

  options: any;
  groups: any;
  modules: any;

  onModuleEvent?: Subject<{ module: Module, event: any }>;

  zwaveAdapter?: ZwaveAdapter;
//  x10Adapter?: X10Adapter;

  connect(): Subject<any>;

  control(module: Module, command: CMD, options: any): Subject<any>;

  getModuleIcon(module: HguiModule): string;
}
