import {Component, Input, OnInit} from '@angular/core';
import {Module} from '../../../services/hgui/module';
import {ZwaveApi, ZWaveAssociation, ZWaveAssociationGroup, ZwaveConfigParam} from '../zwave-api';
import {HguiService} from '../../../services/hgui/hgui.service';
import {concat, Subject} from 'rxjs';
import {COMMA, ENTER} from '@angular/cdk/keycodes';

export class CommandClass {
  id: any;
  description: string;
}

@Component({
  selector: 'app-zwave-node-config',
  templateUrl: './zwave-node-config.component.html',
  styleUrls: ['./zwave-node-config.component.scss']
})
export class ZwaveNodeConfigComponent implements OnInit {
  @Input() module: Module;
  associations: ZWaveAssociation;
  associationsSeparator: number[] = [ COMMA, ENTER ];
  commandClasses: Array<CommandClass> = [];
  configurationParameters: Array<ZwaveConfigParam> = [];
  isNetworkBusy = false;

  constructor(private hgui: HguiService) { }

  ngOnInit(): void {
    this.syncAssociations();
    this.syncConfigParams();
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter.getCommandClasses(this.module).subscribe((classes) => {
      this.commandClasses = classes;
    });
  }

  onConfigParameterChange(e, p: ZwaveConfigParam): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    p.status = 1; // status = 1 -> loading
    if (e.target) { e = e.target; }
    p.field.value = e.value;
    adapter.zwaveAdapter
      .setConfigParam(this.module, p)
      .subscribe();
  }

  onGroupAssociationsAdd(e, group): void {
    const input = e.target || e.input;
    if (input == null || input.value.length === 0) { return; }
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter
      .addAssociationGroup(this.module, group, input.value)
      .subscribe(null, null, () => {
        if (input) {
          input.value = '';
        }
      });
  }
  onGroupAssociationsRemove(e, group, nodeNumber): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter
      .removeAssociationGroup(this.module, group, nodeNumber)
      .subscribe();
  }

  // TODO: move this method body to a new ZWaveApi method `getAssociationGroups`
  private syncAssociations(): void {
    // TODO: read from deviceInfo->assocGroups when availbale (including group description)
    this.associations = null;
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter.getDeviceInfo(this.module).subscribe((info) => {
      if (info) {
        let assocGroups = info.assocGroups.assocGroup;
        if (assocGroups.length == null) {
          assocGroups = [ assocGroups ];
        }
        this.associations = new ZWaveAssociation();
        this.associations.count = assocGroups.length;
        assocGroups.map((ag) => {
          const n = +ag['@number'];
          const groupField = this.module.fields.find((f) => f.key === ZwaveApi.fields.Associations + '.' + n);
          const group = new ZWaveAssociationGroup(n, groupField);
          group.description = adapter.zwaveAdapter.getLocaleText(ag.description);
          group.max = +ag['@maxNodes'];
          adapter.zwaveAdapter.getAssociationGroup(this.module, group).subscribe((ag2) => {
            this.associations.groups.push(group);
          });
        });
      } else {
        const count = this.module.fields.find((f) => f.key === ZwaveApi.fields.Associations + '.Count');
        if (count) {
          this.associations = new ZWaveAssociation();
          this.associations.count = +count.value === 0 ? 1 : +count.value;
          // TODO: should this be different for each group? (eg. 'ZwaveApi.fields.Associations + '.' + groupNumber + '.Max'
          const max = this.module.fields.find((f) => f.key === ZwaveApi.fields.Associations + '.Max');
          if (max) {
            this.associations.max = +max.value;
          }
          for (let g = 0; g < this.associations.count; g++) {
            const groupField = this.module.fields.find((f) => f.key === ZwaveApi.fields.Associations + '.' + (g + 1));
            if (groupField) {
              const group = new ZWaveAssociationGroup(this.associations.groups.length + 1, groupField);
              adapter.zwaveAdapter.getAssociationGroup(this.module, group).subscribe((ag) => {
                this.associations.groups.push(group);
              });
            }
          }
        }
      }
    });
  }
  // TODO: make this cancellable (eventually)
  private syncConfigParams(): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    this.isNetworkBusy = true;
    adapter.zwaveAdapter.getConfigParams(this.module).subscribe((params) => {
      this.isNetworkBusy = false;
      this.configurationParameters = params;
      const requestSequence: Subject<any>[] = [];
      params.map((cp) => {
        if (cp.field.value == null || cp.field.value.length === 0) {
          requestSequence.push(
            adapter.zwaveAdapter.getConfigParam(this.module, cp)
          );
        }
      });
      if (requestSequence.length > 0) {
        concat(...requestSequence).subscribe((res) => {
          // TODO: this fires each time a request in sequence is completed
        }, (err) => {
          console.log(err);
        }, () => {
          // TODO: ?
        });
      }
    });
  }
}
