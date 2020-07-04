import {Component, Input, OnInit} from '@angular/core';
import {Module} from '../../../services/hgui/module';

@Component({
  selector: 'app-zwave-node-config',
  templateUrl: './zwave-node-config.component.html',
  styleUrls: ['./zwave-node-config.component.scss']
})
export class ZwaveNodeConfigComponent implements OnInit {
  @Input() module: Module;
  constructor() { }

  ngOnInit(): void {
  }

}
