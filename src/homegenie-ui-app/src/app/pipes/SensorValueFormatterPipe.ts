import {Pipe, PipeTransform} from '@angular/core';
import {ModuleField} from '../services/hgui/module';

@Pipe({ name: 'sensorValueFormatter' })
export class SensorValueFormatterPipe implements PipeTransform {
  transform(fieldValue: any): string {
    return (Math.round(fieldValue * 100) / 100).toLocaleString();
  }
}
