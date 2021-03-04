import {Component, OnInit} from '@angular/core';
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-slider',
  templateUrl: './slider.component.html',
  styleUrls: ['./slider.component.scss']
})
export class SliderComponent extends ControlFieldBase implements OnInit {
  isInitialized = false;

  get default(): any {
    return this.data.type.options[3] || this.data.type.options[0];
  }
  get isBinary(): boolean {
    return this.data && this.data.type.options[1] - this.data.type.options[0] === 1;
  }

  ngOnInit(): void {
    super.ngOnInit();
    this.isInitialized = true;
  }

  onFieldChange(e): void {
    this.fieldChange.emit({ field: this.data.field, value: this.isBinary ? (e.checked ? 1 : 0) : e.value.toString() });
  }
}
