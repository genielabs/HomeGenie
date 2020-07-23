import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Module, ModuleField} from '../../services/hgui/module';
import {Adapter} from '../../adapters/adapter';

@Component({
  selector: 'app-sensor',
  templateUrl: './sensor.component.html',
  styleUrls: ['./sensor.component.scss']
})
export class SensorComponent implements OnInit, OnDestroy {
  @Input()
  module: Module;
  @Output()
  showOptions: EventEmitter<any> = new EventEmitter();

  sensor: { field: ModuleField, unit: string };

  // TODO: create SensorValueFormatter pipe

  private refreshTimeout: any = null;
  private currentIndex = 0;
  constructor() {}

  get sensors(): Array<any> {
    const fields = this.module.fields.filter((f) => f.key.startsWith('Sensor.'));
    return fields.map((f) => ({
      field: f,
      unit: ''
    }));
  }

  get temperature(): number {
    return 22.4;
  }

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
    this.sensor = this.sensors[this.currentIndex];
    if (this.currentIndex < this.sensors.length - 1) {
      this.currentIndex++;
    } else {
      this.currentIndex = 0;
    }
    this.startTimeout();
  }
}
