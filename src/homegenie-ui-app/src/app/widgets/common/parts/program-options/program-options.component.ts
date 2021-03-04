import { Component } from '@angular/core';
import {DynamicOptionsBase} from "../dynamic-options-base.component";
import {CMD} from "../../../../services/hgui/hgui.service";

@Component({
  selector: 'app-program-options',
  templateUrl: './program-options.component.html',
  styleUrls: ['./program-options.component.scss']
})
export class ProgramOptionsComponent extends DynamicOptionsBase {

  applyChanges() {
    console.log('ProgramOptionsComponent::applyChanges', this.changes);
    if (this.changes.length > 0) {
      const changes: any = {};
      this.changes.forEach((c) => {
        changes[c.field.key] = c.value;
      });
      this.module.control(CMD.Options.Set, changes).subscribe((res) => {
        console.log('ProgramOptionsComponent::applyChanges DONE');
      });
    }
  }

}
