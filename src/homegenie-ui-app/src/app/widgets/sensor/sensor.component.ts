import {Component, OnDestroy, OnInit} from '@angular/core';
import {ModuleField} from '../../services/hgui/module';
import {WidgetBase} from "../widget-base";

export class SensorFieldData {
  field: ModuleField;
  unit: any;
}

export class SensorData {
  public sensors: SensorFieldData[];
}

@Component({
  selector: 'app-sensor',
  templateUrl: './sensor.component.html',
  styleUrls: ['./sensor.component.scss']
})
export class SensorComponent extends WidgetBase implements OnInit, OnDestroy {
  sensor: SensorFieldData;
  statusText = '// TODO: status text and battery level';

  private refreshTimeout: any = null;
  private currentIndex = 0;

  ngOnInit(): void {
    this.refresh();
  }
  ngOnDestroy(): void {
    this.stopTimeout();
  }

  private startTimeout(): void {
    this.refreshTimeout = setTimeout(this.refresh.bind(this), 5000);
  }
  private stopTimeout(): void {
    clearTimeout(this.refreshTimeout);
  }
  private refresh(): void {
    if (!this.options && !this.options.data) return;
    this.sensor = this.options.data.sensors[this.currentIndex];
    if (this.currentIndex < this.options.data.sensors.length - 1) {
      this.currentIndex++;
    } else {
      this.currentIndex = 0;
    }
    this.startTimeout();
  }
}
