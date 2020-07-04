import {Component, Input, OnInit} from '@angular/core';
import {Module, ModuleField} from '../../../services/hgui/module';
import {ZwaveApi} from '../zwave-api';

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
  constructor() { }
  commandClasses: Array<CommandClass> = [];

  ngOnInit(): void {
    const nif: ModuleField = this.module.fields.find((f) => f.key === ZwaveApi.fields.NodeInfo);
    if (nif) {
      const nodeInformationFrame: string[] = nif.value.split(' ').slice(3);
      this.commandClasses = nodeInformationFrame.map((c) => ({
        id: c,
        description: ZwaveApi.classes[c]
      }));
      console.log('NIF', nodeInformationFrame, this.commandClasses);
    } else {
      // TODO: should throw error
    }
  }

}
