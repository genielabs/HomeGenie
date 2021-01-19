import { Component } from '@angular/core';
import {ControlFieldBase} from "../control-field-base";
import {OptionFieldTypeId} from "../../../../services/hgui/module-options";

@Component({
  selector: 'app-dynamic-control',
  templateUrl: './dynamic-control.component.html',
  styleUrls: ['./dynamic-control.component.scss']
})
export class DynamicControlComponent extends ControlFieldBase {
  OptionFieldTypeId = OptionFieldTypeId;
}
