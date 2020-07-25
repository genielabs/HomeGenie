import {ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {TranslateService} from '@ngx-translate/core';
import {Module} from "../../../../services/hgui/module";

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.component.html',
  styleUrls: ['./checkbox.component.scss']
})
export class CheckboxComponent implements OnInit {
  translationPrefix: string;
  @Input()
  module: Module;
  @Input()
  field: any;
  @Output()
  fieldChange: EventEmitter<any> = new EventEmitter();

  constructor(private translate: TranslateService) {}

  private _description = '';
  get description(): string {
    return this._description;
  }
  get value(): number {
    const field = this.field;
    return field.field ? field.field.value : '';
  }

  ngOnInit(): void {
    if (this.module) {
      this.translationPrefix = this.module.getAdapter().translationPrefix;
    }
    this._description = this.field.description;
    const key = `${this.translationPrefix}.$options.${this.field.pid}.${this.field.field.key}`;
    this.translate.get(key).subscribe((res) => {
      if (res !== key) {
        this._description = res;
      }
    });
  }

  onFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.checked ? 'On' : '' });
  }
}
