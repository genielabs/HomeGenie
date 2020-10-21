import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {Module} from "../../../../services/hgui/module";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-text',
  templateUrl: './text.component.html',
  styleUrls: ['./text.component.scss']
})
export class TextComponent implements OnInit {
  translationPrefix: string;
  @Input()
  module: Module;
  @Input()
  field: any;
  @Output()
  fieldChange: EventEmitter<any> = new EventEmitter();

  isInitialized = false;

  constructor(private translate: TranslateService) {}

  private _description = '';
  get description(): string {
    return this._description;
  }
  get value(): string {
    const field = this.field;
    return field.field && field.field.value ? field.field.value : this.default;
  }
  get default(): string {
    return this.field.type.options[3] || this.field.type.options[0];
  }

  ngOnInit(): void {
    if (this.module) {
      this.translationPrefix = this.module.getAdapter().translationPrefix;
    }
    this._description = this.field.description;
    if (this.field.field) {
      const key = `${this.translationPrefix}.$options.${this.field.pid}.${this.field.field.key}`;
      this.translate.get(key).subscribe((res) => {
        if (res !== key) {
          this._description = res;
        }
      });
    }
    this.isInitialized = true;
  }

  onFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.target.value });
  }
}
