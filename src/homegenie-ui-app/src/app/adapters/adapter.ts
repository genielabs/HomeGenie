import {Module} from '../services/hgui/module';
import {Subject} from 'rxjs';
import {HguiService} from '../services/hgui/hgui.service';
import {ZwaveAdapter} from '../components/zwave/zwave-adapter';

export interface Adapter {
  hgui: HguiService;
  className: string;
  id: string;

  options: any;
  groups: any;
  modules: any;

  zwaveAdapter?: ZwaveAdapter;

  connect(): Subject<any>;

  control(module: Module, command: string, options: any): Subject<any>;
}
