import {Component, Input, OnInit} from '@angular/core';
import {Module, ModuleType} from '../../../../services/hgui/module';
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
    let matchTypes = this.hgui.modules.slice(0);
    let matchFields = [];

    if (this.data.type.options.length >= 3) {
      const fieldNames = this.data.type.options[2];
      if (fieldNames.length > 0 &&  fieldNames !== 'any') {
        matchFields = this.hgui.modules.filter((m) => m.fields.find((f) => new RegExp(`,${f.key},`, 'i').test(`,${fieldNames},`)));
      }
    }
    if (this.data.type.options.length >= 2) {
      const typeNames = this.data.type.options[1];
      matchTypes = this.hgui.modules.filter((m) => new RegExp(`,${m.type},`, 'i').test(`,${typeNames},`));
    }

    // TODO: implement other types of filtering from old HG (eg. domain filtering, regexp for fields filter)

    const result = matchTypes.concat(matchFields);
    // remove duplicates from results
    for (let i = 0; i < result.length; ++i) {
      for (let j = i + 1; j < result.length; ++j) {
        if(result[i] === result[j]) {
          result.splice(j--, 1);
        }
      }
    }
    return result;
  }
}
