import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Module, ModuleField} from '../../services/hgui/module';
import {WidgetOptions} from "../widget-options";
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

  private refreshTimeout: any = null;
  private currentIndex = 0;

  ngOnInit(): void {
    this.refresh();
  }
  ngOnDestroy(): void {
    this.stopTimeout();
  }

  onModuleOptionsClick(e): void {
    this.showOptions.emit(null);
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
