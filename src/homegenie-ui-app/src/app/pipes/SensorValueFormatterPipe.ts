import {Pipe, PipeTransform} from '@angular/core';
import {ModuleField} from '../services/hgui/module';

@Pipe({ name: 'sensorValueFormatter' })
export class SensorValueFormatterPipe implements PipeTransform {
  transform(fieldValue: any, decimalDigits?: number): string {
    if (typeof fieldValue === 'string') {
      fieldValue = +fieldValue.replace(',', '.');
    }
    if (!decimalDigits) decimalDigits = 1;
    return (Math.round(fieldValue * Math.pow(10, decimalDigits)) / Math.pow(10, decimalDigits)).toLocaleString();
  }
}
