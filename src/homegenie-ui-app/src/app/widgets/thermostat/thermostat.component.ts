import {Component, ElementRef, ViewChild} from '@angular/core';
import {WidgetBase} from "../widget-base";
import {ModuleField} from "../../services/hgui/module";
import {CMD, FLD} from "../../services/hgui/hgui.service";
import {ActivityStatusComponent} from "../common/parts/activity-status/activity-status.component";

@Component({
  selector: 'app-thermostat',
  templateUrl: './thermostat.component.html',
  styleUrls: ['./thermostat.component.scss']
})
export class ThermostatComponent extends WidgetBase {
  @ViewChild(ActivityStatusComponent)
  private activityStatus: ActivityStatusComponent;


  // 'Off' | 'Heat' | 'Cool' | 'Auto'
  get currentMode(): string {
    const mode = this.module.field(FLD.Thermostat.Mode)
    if (mode && mode.value) {
      return mode.value.replace('Economy', '');
    }
    return 'N/A';
  }
  set currentMode(m: string) {
    if (m && m.length > 0) {
      const mode = this.module.field(FLD.Thermostat.Mode);
      const currentMode = mode ? mode.value : 'Off';
      this.module.control(CMD.Thermostat.ModeSet, m)
        .subscribe((res) => {
          if (res.response.ResponseValue === 'ERROR') {
            this.activityStatus.setError('Command not implemented');
            // revert slider to previous value
            this.module.field(FLD.Thermostat.Mode, null);
            requestAnimationFrame(() => {
              this.module.field(FLD.Thermostat.Mode, currentMode);
            });
          }
        });
    }
  }

  get economyMode(): boolean {
    const mode = this.module.field(FLD.Thermostat.Mode)
    if (mode && mode.value) {
      return mode.value.indexOf('Economy') > 0;
    }
  }
  set economyMode(eco: boolean) {
    this.currentMode = this.currentMode + (eco ? 'Economy' : '');
  }

  get setPointMax(): number {
    // TODO: detect locale unit (C / F) and set value accordingly
    return 35;
  }
  get setPointMin(): number {
    // TODO: detect locale unit (C / F) and set value accordingly
    return 5;
  }
  get setPointStep(): number {
    return .5;
  }
  get setPoint(): number {
    let setPoint: ModuleField;
    const mode = this.currentMode;
    switch (mode) {
      case 'Heat':
        setPoint = this.module.field(FLD.Thermostat.SetPoint.Heating);
        break;
      case 'Cool':
        setPoint = this.module.field(FLD.Thermostat.SetPoint.Cooling);
        break;
    }
    if (setPoint) {
      return setPoint.value;
    }
  }
  set setPoint(v: number) {
    const mode = this.currentMode;
    if (mode === 'Heat' || mode === 'Cool') {
      this.module.control(CMD.Thermostat.SetPointSet, `${mode}ing/${v}`)
        .subscribe((res) => {
          console.log(res);
        });
    }
  }

  get fanMode(): string {
    const mode = this.module.field(FLD.Thermostat.FanMode);
    return mode ? mode.value : 'Off';
  }
  set fanMode(m: string) {
    if (m && m.length > 0) {
      const mode = this.module.field(FLD.Thermostat.FanMode);
      const currentMode = mode ? mode.value : 'Off';
      this.module.control(CMD.Thermostat.FanModeSet, m)
        .subscribe((res) => {
          if (res.response.ResponseValue === 'ERROR') {
            this.activityStatus.setError('Command not implemented');
            // revert slider to previous value
            this.module.field(FLD.Thermostat.FanMode, null);
            requestAnimationFrame(() => {
              this.module.field(FLD.Thermostat.FanMode, currentMode);
            });
          }
        });
    }
  }


  get operatingState(): string {
    const state = this.module.field(FLD.Thermostat.OperatingState);
    if (state) {
      return state.value;
    }
    return 'N/A';
  }
  get temperature(): number {
    const t = this.module.field(FLD.Sensor.Temperature);
    if (t) {
      return t.value;
    }
    return 0.0;
  }

}
