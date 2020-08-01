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
  translationPrefix: string;

  sectionTab = 0;

  constructor(
    private translate: TranslateService,
    public dialogRef: MatDialogRef<WidgetOptionsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public module: Module
  ) {
    if (module.getAdapter()) {
      this.translationPrefix = module.getAdapter().translationPrefix;
    } else {
      // TODO: log this exception
    }
  }

  ngOnInit(): void {
    this.module.control(CMD.Options.Get).subscribe((res: any[]) => {
      this.options = res;
      setTimeout(() => {
        this.options.forEach((o) => {
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
  ngOnDestroy(): void {
  }

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

  onSectionTabChange(e): void {
    this.sectionTab = e;
  }

}
