import {Component, EventEmitter, Input, OnInit, Output} from "@angular/core";
import {TranslateService} from "@ngx-translate/core";
import {MatSnackBar} from "@angular/material/snack-bar";

import {Module, ModuleField} from "../../../services/hgui/module";
import {OptionField} from "../../../services/hgui/module-options";
import {HguiService} from "../../../services/hgui/hgui.service";

export class FieldChangeEvent {
  field: ModuleField;
  value: any;
}

@Component({
  selector: '-control-field-base',
  template: 'no-ui'
})
export class ControlFieldBase implements OnInit {
  translationPrefix: string;
  @Input()
  module: Module;
  @Input()
  data: OptionField;
  @Output()
  fieldChange: EventEmitter<FieldChangeEvent> = new EventEmitter();
  @Input()
  multiple: boolean = false;

  constructor(
    protected _translate: TranslateService,
    protected _snackBar: MatSnackBar,
    public hgui: HguiService
  ) { }

  private _description = '';
  get description(): string {
    return this._description;
  }
  get value(): any {
    return this.data.field && this.data.field.value ? this.data.field.value : this.default;
  }
  get default(): string {
    return '';
  }

  ngOnInit(): void {
    if (this.module) {
      this.translationPrefix = this.module.getAdapter().translationPrefix;
    }
    this._description = this.data.description;
    if (this.data.field) {
      const key = `${this.translationPrefix}.$options.${this.data.pid}.${this.data.field.key}`;
      this._translate.get(key).subscribe((res) => {
        if (res !== key) {
          this._description = res;
        }
      });
    }
  }

  onFieldChange(e): void {
    this.fieldChange.emit({ field: this.data.field, value: e.value });
  }
}
