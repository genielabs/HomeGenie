import {ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {TranslateService} from '@ngx-translate/core';
import {Observable, Subject} from 'rxjs';
import {Module} from "../../../../services/hgui/module";

@Component({
  selector: 'app-slider',
  templateUrl: './slider.component.html',
  styleUrls: ['./slider.component.scss']
})
export class SliderComponent implements OnInit {
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
  get value(): number {
    const field = this.field;
    return field.field && field.field.value ? field.field.value : this.default;
  }
  get default(): number {
    return this.field.type.options[3] || this.field.type.options[0];
  }
  get isBinary(): boolean {
    return this.field.type.options[1] - this.field.type.options[0] === 1;
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
    this.isInitialized = true;
  }

  onFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.value });
  }
}
