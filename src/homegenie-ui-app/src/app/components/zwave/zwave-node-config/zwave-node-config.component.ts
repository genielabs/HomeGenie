import {Component, Input, OnInit} from '@angular/core';
import {Module, ModuleField} from '../../../services/hgui/module';
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
  moduleInfo: any;

  associations: ZWaveAssociation;
  associationsSeparator: number[] = [ COMMA, ENTER ];
  commandClasses: Array<CommandClass> = [];
  configurationParameters: Array<ZwaveConfigParam> = [];

  customConfigParameter: ZwaveConfigParam;
  customParameterNumber: any;
  customParameterValue: any;

  isNetworkBusy = false;

  constructor(private hgui: HguiService) { }

  ngOnInit(): void {
    this.isNetworkBusy = true;
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter.getAssociations(this.module)
      .subscribe((assocs) => {
        this.associations = assocs;
      }, (err) => { }, () => {
        adapter.zwaveAdapter.getCommandClasses(this.module).subscribe((classes) => {
          this.commandClasses = classes;
          this.isNetworkBusy = false;
          this.syncConfigParams();
          // collect 'moduleInfo' data
          this.moduleInfo = {};
          const ms = this.module.field(ZwaveApi.fields.ManufacturerSpecific);
          if (ms) {
            this.moduleInfo.manufacturerSpecific = ms.value;
            const deviceInfo = this.module.data(ZwaveApi.DataCache.deviceInfo);
            if (deviceInfo) {
              this.moduleInfo.brandName = deviceInfo.deviceDescription.brandName;
              this.moduleInfo.productName = deviceInfo.deviceDescription.productName;
              this.moduleInfo.productLine = deviceInfo.deviceDescription.productLine;
            }
          }
        });
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
  onCustomParameterSend(param: { number: number, value: number }): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    let p = this.configurationParameters.find((cp) => +cp.number === +param.number);
    if (p == null) {
      p = new ZwaveConfigParam();
      p.number = param.number;
    }
    const key = ZwaveApi.fields.ConfigVariables + '.' + p.number;
    let field = this.module.field(key);
    if (field == null) {
      field = new ModuleField();
      field.key = key;
    }
    field.value = param.value;
    p.field = field;
    p.status = 1; // status = 1 -> loading
    this.customConfigParameter = p;
    adapter.zwaveAdapter
      .setConfigParam(this.module, p)
      .subscribe((res) => {
        console.log('Custom parameter SET', res);
        if (res && res.field) {
          this.customParameterValue = +res.field.value;
        } else {
          // TODO: error?
        }
        this.syncConfigParams();
      });
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

  // TODO: make this cancellable (eventually)
  private syncConfigParams(): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    this.isNetworkBusy = true;
    adapter.zwaveAdapter.getConfigParams(this.module).subscribe((params) => {
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
          this.isNetworkBusy = false;
        });
      } else {
        this.isNetworkBusy = false;
      }
    });
  }
}
