import {Component, OnInit} from '@angular/core';
import {ModuleOptions} from '../../../../services/hgui/module-options';
import {CMD} from '../../../../services/hgui/hgui.service';
import {OptionsDialogBase} from "../options-dialog-base";

@Component({
  selector: 'app-widget-options-dialog',
  templateUrl: './widget-options-dialog.component.html',
  styleUrls: ['./widget-options-dialog.component.scss']
})
export class WidgetOptionsDialogComponent extends OptionsDialogBase implements OnInit {
  optionsList: ModuleOptions[] = [];
  sectionTab = 0;

  ngOnInit(): void {
    // populate module features list
    this.module.control(CMD.Options.Get).subscribe((res: ModuleOptions[]) => {
      this.optionsList = res;
      setTimeout(() => {
        this.optionsList.forEach((o) => {
          this.translateModuleOption(o);
        });
      });
    });
  }

  onSectionTabChange(e): void {
    this.sectionTab = e;
  }
}
