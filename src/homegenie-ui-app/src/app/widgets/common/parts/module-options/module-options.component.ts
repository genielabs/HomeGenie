import { Component } from '@angular/core';
import {DynamicOptionsBase} from "../dynamic-options-base.component";
import {CMD} from "../../../../services/hgui/hgui.service";

@Component({
  selector: 'app-module-options',
  templateUrl: './module-options.component.html',
  styleUrls: ['./module-options.component.scss']
})
export class ModuleOptionsComponent extends DynamicOptionsBase {

  applyChanges() {
    console.log('ModuleOptionsComponent::applyChanges', this.changes);
    if (this.changes.length > 0) {
      const changes: any = {};
      this.changes.forEach((c) => {
        changes[c.field.key] = c.value;
      });
      this.module.control(CMD.Options.Set, changes).subscribe((res) => {
        console.log('ModuleOptionsComponent::applyChanges DONE');
      });
    }
  }

}
