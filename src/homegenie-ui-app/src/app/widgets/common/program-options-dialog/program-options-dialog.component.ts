import {Component, Inject, OnInit} from '@angular/core';
import {TranslateService} from "@ngx-translate/core";
import {MAT_DIALOG_DATA, MatDialogRef} from "@angular/material/dialog";
import {Module, ModuleField, ProgramOptions} from "../../../services/hgui/module";
import {CMD} from "../../../services/hgui/hgui.service";

@Component({
  selector: 'app-program-options-dialog',
  templateUrl: './program-options-dialog.component.html',
  styleUrls: ['./program-options-dialog.component.scss']
})
export class ProgramOptionsDialogComponent implements OnInit {

  options: ProgramOptions = new ProgramOptions();
  changes: { field: ModuleField, value: any }[] = [];
  translationPrefix: string;

  constructor(
    private translate: TranslateService,
    public dialogRef: MatDialogRef<ProgramOptionsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public module: Module
  ) {
    if (module.getAdapter()) {
      this.translationPrefix = module.getAdapter().translationPrefix;
    } else {
      // TODO: log this exception
    }
  }

  ngOnInit(): void {
    this.module.control(CMD.Options.Get).subscribe((res: ProgramOptions) => {
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

  // TODO: move this method to a OptionsDialog base class
  onFieldChange(e): void {
    if (e.field.value === e.value) {
      this.changes = this.changes.filter((c) => c.field.key !== e.field.key);
    } else {
      let change = this.changes.find((c) => c.field.key === e.field.key);
      if (change) {
        change.value = e.value;
      } else {
        this.changes.push(e);
      }
    }
  }

}
