import { Component } from '@angular/core';

import { environment } from '../environments/environment';

import { HomegenieAdapter } from 'src/app/adapters/homegenie/homegenie-adapter';
import AdapterFactory from './adapters/adapter-factory';
import { HguiService } from './services/hgui/hgui.service';
import {TranslateService} from '@ngx-translate/core';
import {Module} from './services/hgui/module';
import {Subject} from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  title = 'homegenie-ui-app';
  isNetworkBusy = false;

  constructor(public hgui: HguiService, translate: TranslateService) {
    // Configure HGUI adapters
    AdapterFactory.setClasses({
      // only HomeGenie adapter currently implemented
      HomegenieAdapter,
      // ...
    });
    this.isNetworkBusy = true;
    // testing HGUI service methods
    // hgui.onModuleAdded.subscribe((m) => console.log('Added module', m));
    // hgui.onGroupAdded.subscribe((g) => console.log('Added group', g));
    // hgui.onGroupModuleAdded.subscribe((e) => console.log('Added module to group', e.group, e.module));
    // hgui.onAdapterAdded.subscribe((adapter) => console.log('Added adapter', adapter));
    hgui.loadConfiguration().subscribe((config) => {
      if (config == null) {
        this.configure(hgui).subscribe((res) => {
          this.isNetworkBusy = false;
        });
      } else {
        this.isNetworkBusy = false;
      }
    });
    // set translation language
    const browserLang = translate.getBrowserLang();
    translate.use(browserLang.match(/en|it/) ? browserLang : 'en');
  }
  /**
   * Creates a default configuration with one adapter (HomeGenie API adapter) pointing to localhost:8080
   * @param hgui HGUI service instance
   */
  configure(hgui: HguiService): Subject<any> {
    const subject = new Subject<any>();
    const homegenieAdapter = new HomegenieAdapter(hgui);
    // config for connection through angular proxy (see: 'src/proxy.conf.json')

    // TODO: following code is provisory, to be completed...
    if (environment.production) {
      // config for direct connection to HG API on the same http server as app
      homegenieAdapter.options = {
        config: {
          connection: {
            localRoot: '/',
            address: 'localhost',
            port: 8080,
            websocketPort: 8188
          },
        },
      };
    } else if (environment.proxy) {
      // config to proxy HG to local Angular http service
      homegenieAdapter.options = {
        config: {
          connection: {
            address: 'localhost',
            port: 4200,
            websocketPort: 4200
          },
        },
      };
    } else {
      // config for direct connection to HG API on different http server
      homegenieAdapter.options = {
        config: {
          connection: {
            address: 'localhost',
            port: 8080,
            websocketPort: 8188
          },
        },
      };
    }
    hgui.addAdapter(homegenieAdapter);
    homegenieAdapter.connect().subscribe(() => {
      console.log('connected', homegenieAdapter);
      // get modules and groups list
      homegenieAdapter.groups.map((g) => {
        // add group to HGUI
        const hguiGroup = hgui.addGroup(g.Name);
        // add modules and groups to HGUI
        g.Modules.map((moduleLink) => {
          // in HomeGenie Server group modules are just links, so we need to get the module instance from `moduleList`
          const module = homegenieAdapter.modules.find(
            (m) =>
              m.Domain === moduleLink.Domain && m.Address === moduleLink.Address
          );
          // if the module type is not supported it won't be found in the modules list
          if (module == null) { return; }
          const moduleId = module.Domain + '/' + module.Address;
          const adapterId = homegenieAdapter.id;
          const hguiModule = hgui.addModule(new Module({
            id: moduleId,
            adapterId,
            type: module.DeviceType.toLowerCase(),
            name: module.Name,
            description: module.Description,
            fields: [],
          }));
          // Update modules fields (hgui fields = hg Properties)
          module.Properties.map((p) => {
            hguiModule.field(p.Name, p.Value, p.UpdateTime);
          });
          hgui.addGroupModule(hguiGroup, hguiModule);
        });
      });
      hgui.saveConfiguration().subscribe((config) => {
        console.log('Config saved', config);
        subject.next(config);
      });
    });
    return subject;
  }
}
