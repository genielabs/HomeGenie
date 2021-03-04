import {Component, EventEmitter, OnInit, Input, Output} from '@angular/core';
import {Module} from "../../../../services/hgui/module";
import {CMD} from "../../../../services/hgui/hgui.service";

@Component({
  selector: 'app-widget-options-menu',
  templateUrl: './widget-options-menu.component.html',
  styleUrls: ['./widget-options-menu.component.scss']
})
export class WidgetOptionsMenuComponent {
  @Input()
  module: Module;
  @Output()
  menuOptionSelect = new EventEmitter<{module: Module, option: 'statistics' | 'settings' | 'schedule'}>();

  schedulesCount = 0;

  onMenuOpened(): void {
    this.module.getAdapter()
      .system(CMD.Automation.Scheduling.List, {
        type: this.module.type
      })
      .subscribe((res) => {
        this.schedulesCount = res.length;
      });
  }

  public onOptionsButtonClick(option): void {
    this.menuOptionSelect.emit({module: this.module, option});
  }
}
