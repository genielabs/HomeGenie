import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {HguiService} from '../../../../services/hgui/hgui.service';
import {Module} from '../../../../services/hgui/module';

@Component({
  selector: 'app-module-select',
  templateUrl: './module-select.component.html',
  styleUrls: ['./module-select.component.scss']
})
export class ModuleSelectComponent implements OnInit {
  @Input()
  field: any;
  @Output()
  fieldChange: EventEmitter<any> = new EventEmitter();

  constructor(public hgui: HguiService) { }

  get modules(): Array<Module> {
    if (this.field.type.options.length >= 3) {
      const fieldNames = this.field.type.options[2];
      return this.hgui.modules.filter((m) => m.fields.find((f) => new RegExp(`,${f.key},`, 'i').test(`,${fieldNames},`)));
    }
    return [];
  }

  ngOnInit(): void {
    console.log(this.field.type, this.field);
  }

  onFieldChange(e, f): void {
    this.fieldChange.emit({ field: f.field, value: e.value });
  }
}
