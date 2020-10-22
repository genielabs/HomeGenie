import {Component, OnInit} from '@angular/core';
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.component.html',
  styleUrls: ['./checkbox.component.scss']
})
export class CheckboxComponent extends ControlFieldBase implements OnInit {
  onFieldChange(e): void {
    this.fieldChange.emit({ field: this.data.field, value: e.checked ? 'On' : '' });
  }
}
