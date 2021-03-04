import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Module, ModuleField} from "../../../services/hgui/module";
import {TranslateService} from "@ngx-translate/core";
import {ModuleOptions} from "../../../services/hgui/module-options";
import {CMD} from "../../../services/hgui/hgui.service";

@Component({
  selector: '-dynamic-options-base',
  template: 'no-ui'
})
export class DynamicOptionsBase implements OnInit{
  @Input()
  module: Module;
  @Output()
  changesUpdate = new EventEmitter<any[]>();

  changes: { field: ModuleField, value: any }[] = [];

  optionsList: ModuleOptions[] = [];
  translationPrefix: string;

  get isChanged(): boolean {
    return this.changes.length > 0;
  }

  constructor(
    protected translate: TranslateService
  ) { }

  ngOnInit(): void {
    if (this.module && this.module.getAdapter()) {
      this.translationPrefix = this.module.getAdapter().translationPrefix;
      // populate module features list
      this.module.control(CMD.Options.Get).subscribe((res: ModuleOptions[]) => {
        if (this.module.type === 'program') {
          this.optionsList[0] = res as any;
        } else {
          this.optionsList = res;
        }
        setTimeout(() => {
          this.optionsList.forEach((o) => {
            this.translateModuleOption(o);
          });
        });
      });
    } else {
      // TODO: log this exception
    }
  }

  applyChanges(): void {
    throw new Error('Not implemented!');
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
    this.changesUpdate.emit(this.changes);
  }

  protected translateModuleOption(o: ModuleOptions) {
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
  }
}
