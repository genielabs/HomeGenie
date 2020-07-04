import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Module} from '../../../services/hgui/module';

@Component({
  selector: 'app-zwave-node-list',
  templateUrl: './zwave-node-list.component.html',
  styleUrls: ['./zwave-node-list.component.scss']
})
export class ZwaveNodeListComponent implements OnInit {
  @Input() modules: any = [];
  @Output() itemClick = new EventEmitter<any>();
  constructor() { }

  ngOnInit(): void {
  }

  onItemClick(module: any): void {
    this.itemClick.emit(module);
  }
}
