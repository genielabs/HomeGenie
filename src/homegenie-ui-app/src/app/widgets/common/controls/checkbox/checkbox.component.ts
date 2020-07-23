import {ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {TranslateService} from '@ngx-translate/core';

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.component.html',
  styleUrls: ['./checkbox.component.scss']
})
export class CheckboxComponent implements OnInit {
  @Input()
  field: any;
  @Output()
  fieldChange: EventEmitter<any> = new EventEmitter();

  constructor(private translate: TranslateService) { }

  private _description = '';
  get description(): string {
    return this._description;
  }
  get value(): number {
    const field = this.field;
    return field.field ? field.field.value : null;
  }

  ngOnInit(): void {
    this._description = this.field.description;
    const key = `HOMEGENIE.programs.${this.field.pid}.${this.field.field.key}`;
    this.translate.get(key).subscribe((res) => {
      if (res !== key) {
        this._description = res;
      }
    });
  }

  onFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.checked });
  }
}
