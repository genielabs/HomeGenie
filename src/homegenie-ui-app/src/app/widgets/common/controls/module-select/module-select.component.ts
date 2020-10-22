import {Component, Input, OnInit} from '@angular/core';
import {Module} from '../../../../services/hgui/module';
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-module-select',
  templateUrl: './module-select.component.html',
  styleUrls: ['./module-select.component.scss']
})
export class ModuleSelectComponent extends ControlFieldBase implements OnInit {
  @Input()
  multiple: boolean = false;

  get value(): string {
    return this.data.field && this.data.field.value ? this.data.field.value.replace(':', '/') : '';
  }
  get modules(): Array<Module> {
    if (this.data.type.options.length >= 3) {
      const fieldNames = this.data.type.options[2];
      return this.hgui.modules.filter((m) => m.fields.find((f) => new RegExp(`,${f.key},`, 'i').test(`,${fieldNames},`)));
    }
    return [];
  }
}
