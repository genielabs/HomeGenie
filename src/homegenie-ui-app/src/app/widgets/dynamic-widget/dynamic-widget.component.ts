import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Module} from "../../services/hgui/module";
import {WidgetOptionsDialogComponent} from "../common/widget-options-dialog/widget-options-dialog.component";
import {CMD} from "../../services/hgui/hgui.service";

@Component({
  selector: 'app-dynamic-widget',
  templateUrl: './dynamic-widget.component.html',
  styleUrls: ['./dynamic-widget.component.scss']
})
export class DynamicWidgetComponent implements OnInit {
  @Input()
  module: Module;
  @Output()
  showOptions: EventEmitter<Module> = new EventEmitter();

  constructor() { }

  ngOnInit(): void {
  }

  onShowOptions(): void {
    this.showOptions.emit(this.module);
  }
}
