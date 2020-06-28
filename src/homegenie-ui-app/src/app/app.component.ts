import { Component } from '@angular/core';

import { HomegenieAdapter } from 'src/app/adapters/homegenie/homegenie-adapter';
import AdapterFactory from './adapters/adapter-factory';
import { HguiService } from './services/hgui/hgui.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  title = 'homegenie-ui-app';

  constructor(hgui: HguiService) {
    AdapterFactory.setClasses({
      HomegenieAdapter,
      // ...
    });
    // testing HGUI service methods
    hgui.onModuleAdded.subscribe((m) => console.log('Added module', m));
    hgui.onGroupAdded.subscribe((g) => console.log('Added group', g));
    hgui.onGroupModuleAdded.subscribe((e) => console.log('Added module to group', e.group, e.module));
    hgui.onAdapterAdded.subscribe((adapter) => console.log('Added adapter', adapter));
    hgui.loadConfiguration().subscribe((config) => {
      if (config == null) {
        console.log('Creating default configuration');
        this.configure(hgui);
      } else {
        console.log('Config loaded', config);
      }
    });
  }
  /**
   * Creates a default configuration with one adapter (HomeGenie API adapter) pointing to localhost:8080
   * @param hgui HGUI service instance
   */
  configure(hgui: HguiService) {
    const homegenieAdapter = new HomegenieAdapter(hgui);
    homegenieAdapter.options = {
      config: {
        connection: {
          address: 'localhost',
          port: 8080,
        },
      },
    };
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
              m.Domain == moduleLink.Domain && m.Address == moduleLink.Address
          );
          // if the module type is not supported it won't be found in the modules list
          if (module == null) return;
          const moduleId = module.Domain + '/' + module.Address;
          const adapterId = homegenieAdapter.id;
          const hguiModule = hgui.addModule({
            id: moduleId,
            adapterId: adapterId,
            type: module.DeviceType.toLowerCase(),
            name: module.Name,
            description: module.Description,
            fields: [],
          });
          // Update modules fields (hgui fields = hg Properties)
          module.Properties.map((p) => {
            hgui.updateModuleField(hguiModule, p.Name, p.Value, p.UpdateTime);
          });
          hgui.addGroupModule(hguiGroup, hguiModule);
        });
      });
      hgui.saveConfiguration().subscribe((config) => {
        console.log('Config saved', config);
      });
    });
  }
}
