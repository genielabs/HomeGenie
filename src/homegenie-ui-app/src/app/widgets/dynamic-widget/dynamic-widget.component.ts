import {Component, EventEmitter, Input, Output} from '@angular/core';
import {Module, ModuleType} from "../../services/hgui/module";

@Component({
  selector: 'app-dynamic-widget',
  templateUrl: './dynamic-widget.component.html',
  styleUrls: ['./dynamic-widget.component.scss']
})
export class DynamicWidgetComponent {
  @Input()
  module: Module;
  @Output()
  showOptions: EventEmitter<Module> = new EventEmitter();

  ModuleType = ModuleType;

  onShowOptionsClick(): void {
    this.showOptions.emit(this.module);
  }
}
