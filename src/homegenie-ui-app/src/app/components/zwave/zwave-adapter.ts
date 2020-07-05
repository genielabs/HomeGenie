import {Subject} from 'rxjs';
import {Module as HguiModule, Module} from '../../services/hgui/module';
import {ZwaveConfigParam} from './zwave-api';
import {CommandClass} from './zwave-node-config/zwave-node-config.component';

export interface ZwaveAdapter {
  onNodeAddReady: Subject<any>;
  onNodeAddStarted: Subject<any>;
  onNodeAddDone: Subject<any>;

  onNodeRemoveReady: Subject<any>;
  onNodeRemoveStarted: Subject<any>;
  onNodeRemoveDone: Subject<any>;

  onDiscoveryStart: Subject<any>;
  onDiscoveryComplete: Subject<any>;

  discovery(): Subject<Array<Module>>;
  addNode(): Subject<any>;
  removeNode(): Subject<any>;

  listNodes(): Array<Module>;
  getNode(id: string): Module;
  getDeviceInfo(module: HguiModule): Subject<any>;
  getLocaleText(langDefinitionObject: any): string;

  getCommandClasses(module: Module): Subject<Array<CommandClass>>;
  getConfigParams(module: Module): Subject<Array<ZwaveConfigParam>>;

  getConfigParam(module: HguiModule, parameterId: number): Subject<any>;
  setConfigParam(module: HguiModule, parameterId: number, parameterValue: number): Subject<any>;
}
