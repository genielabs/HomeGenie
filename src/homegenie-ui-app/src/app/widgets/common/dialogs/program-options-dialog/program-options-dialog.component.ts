import {Component, OnInit} from '@angular/core';
import {ModuleOptions} from "../../../../services/hgui/module";
import {CMD} from "../../../../services/hgui/hgui.service";
import {OptionsDialogBase} from "../options-dialog-base";

@Component({
  selector: 'app-program-options-dialog',
  templateUrl: './program-options-dialog.component.html',
  styleUrls: ['./program-options-dialog.component.scss']
})
export class ProgramOptionsDialogComponent extends OptionsDialogBase implements OnInit {

  options: ModuleOptions = new ModuleOptions();

  ngOnInit(): void {
    this.module.control(CMD.Options.Get).subscribe((res: ModuleOptions) => {
      this.options = res;
      const o = this.options;
      setTimeout(() => {
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
  }

}
