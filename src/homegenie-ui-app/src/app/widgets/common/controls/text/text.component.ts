import {Component, OnInit} from '@angular/core';
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-text',
  templateUrl: './text.component.html',
  styleUrls: ['./text.component.scss']
})
export class TextComponent extends ControlFieldBase implements OnInit {

  get default(): string {
    return this.data.type.options[3] || this.data.type.options[0];
  }

  onTextFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.target.value });
  }
}
