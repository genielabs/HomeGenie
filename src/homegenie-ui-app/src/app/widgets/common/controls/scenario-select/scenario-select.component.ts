import {Component, Input, OnInit} from '@angular/core';
import {CMD} from "../../../../services/hgui/hgui.service";
import {Scenario} from "../../../../services/hgui/automation";
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-scenario-select',
  templateUrl: './scenario-select.component.html',
  styleUrls: ['./scenario-select.component.scss']
})
export class ScenarioSelectComponent extends ControlFieldBase implements OnInit {
  @Input()
  multiple: boolean = false;

  private _scenarios: Array<Scenario> = [];
  get scenarios(): Array<Scenario> {
    return this._scenarios;
  }

  ngOnInit(): void {
    super.ngOnInit();
    if (this.module) {
      this.module.getAdapter().system(CMD.Automation.Scenarios.List).subscribe((res: Scenario[]) => {
        this._scenarios = res;
      });
    }
  }
}
