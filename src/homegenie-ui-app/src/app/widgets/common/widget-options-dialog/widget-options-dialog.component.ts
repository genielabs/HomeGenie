import {Component, Inject, OnDestroy, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from '@angular/material/dialog';
import {Module, ModuleField} from '../../../services/hgui/module';
import {CMD} from '../../../services/hgui/hgui.service';
import {TranslateService} from '@ngx-translate/core';

@Component({
  selector: 'app-widget-options-dialog',
  templateUrl: './widget-options-dialog.component.html',
  styleUrls: ['./widget-options-dialog.component.scss']
})
export class WidgetOptionsDialogComponent implements OnInit, OnDestroy {

  options: any[] = [];
  changes: { field: ModuleField, value: any }[] = [];

  constructor(
    private translate: TranslateService,
    public dialogRef: MatDialogRef<WidgetOptionsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public module: Module
  ) { }

  ngOnInit(): void {
    this.module.control(CMD.Options.Show).subscribe((res: any[]) => {
      console.log(res);
      this.options = res;
      setTimeout(() => {
        this.options.forEach((o) => {
          const titleKey = `HOMEGENIE.programs.${o.id}.Title`;
          this.translate.get(titleKey).subscribe((tr) => {
            if (tr !== titleKey) {
              o.name = tr;
            }
          });
          const descriptionKey = `HOMEGENIE.programs.${o.id}.Description`;
          this.translate.get(descriptionKey).subscribe((tr) => {
            if (tr !== descriptionKey) {
              o.description = tr;
            }
          });
        });
      });
    });
  }
  ngOnDestroy(): void {
    console.log(this.changes);
  }

  onFieldChange(e): void {
    this.changes[e.field.key] = e;
    console.log(e);
  }

}
