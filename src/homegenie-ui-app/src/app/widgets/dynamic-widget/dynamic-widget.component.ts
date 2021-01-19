import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Module, ModuleType} from "../../services/hgui/module";

@Component({
  selector: 'app-dynamic-widget',
  templateUrl: './dynamic-widget.component.html',
  styleUrls: ['./dynamic-widget.component.scss']
})
export class DynamicWidgetComponent implements OnInit {
  @Input()
  module: Module;
  @Input()
  options: any;
  @Output()
  showOptions: EventEmitter<Module> = new EventEmitter();

  ngOnInit() {
  }

  onShowOptionsClick(): void {
    this.showOptions.emit(this.module);
  }
}
