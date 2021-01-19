import {Module as HguiModule, Module} from '../services/hgui/module';
import {Subject} from 'rxjs';
import {CMD, HguiService} from '../services/hgui/hgui.service';
import {ZwaveAdapter} from './zwave-adapter';

// TODO: document this class by adding inline comments
// TODO: document this class by adding inline comments
// TODO: document this class by adding inline comments
// TODO: document this class by adding inline comments

export interface Adapter {
  hgui: HguiService;
  className: string;
  id: string;
  translationPrefix: string;

  options: any;

  // TODO: remove these 3 props
  //groups: any;
  //modules: any;
  //scenarios: any;

  onModuleEvent?: Subject<{ module: Module, event: any }>;

  zwaveAdapter?: ZwaveAdapter;
  // TODO: ....
//  x10Adapter?: X10Adapter;

  connect(): Subject<any>;

  system(command: CMD, options?: any): Subject<any>;

  control(module: Module, command: CMD, options?: any): Subject<any>;

  getWidgetOptions(module: HguiModule): any;
}
