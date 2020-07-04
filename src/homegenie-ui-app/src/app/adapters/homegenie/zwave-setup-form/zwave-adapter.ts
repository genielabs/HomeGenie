import {Subject} from 'rxjs';
import {Module} from '../../../services/hgui/module';

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
}
