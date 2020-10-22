import {Component, OnDestroy, OnInit} from '@angular/core';
import {ModuleOptions} from '../../../../services/hgui/module';
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
          const titleKey = `${this.translationPrefix}.$options.${o.id}.Title`;
          this.translate.get(titleKey).subscribe((tr) => {
            if (tr !== titleKey) {
              o.name = tr;
            }
          });
          const descriptionKey = `${this.translationPrefix}.$options.${o.id}.Description`;
          this.translate.get(descriptionKey).subscribe((tr) => {
            if (tr !== descriptionKey) {
              o.description = tr;
            }
          });
        });
      });
    });
  }

  onSectionTabChange(e): void {
    this.sectionTab = e;
  }
}
