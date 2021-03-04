import {Component, Input, OnDestroy, OnInit} from '@angular/core';
import {Group} from '../services/hgui/group';
import {HguiService} from '../services/hgui/hgui.service';
import {Module} from '../services/hgui/module';

@Component({
  selector: 'app-group-list-item',
  templateUrl: './group-list-item.component.html',
  styleUrls: ['./group-list-item.component.scss']
})
export class GroupListItemComponent implements OnInit, OnDestroy {
  @Input()
  group: Group;

  private updateTimeout = null;

  constructor(public hgui: HguiService) { }

  ngOnInit(): void {
    this.update();
    this.updateTimeout = setInterval(() => {
      this.update();
    }, 1000);
  }
  ngOnDestroy(): void {
    clearInterval(this.updateTimeout);
  }

  private update(): void {
    const s = this.group.stats = this.group.stats || {};
    const modules = this.hgui.getGroupModules(this.group);
    s.luminance = this.getAverageValue(modules, 'Sensor.Luminance');
    s.temperature = this.getAverageValue(modules, 'Sensor.Temperature');
    s.humidity = this.getAverageValue(modules, 'Sensor.Humidity');
    s.watts = this.getTotalValue(modules, 'Meter.Watts');
  }

  private getAverageValue(modules: Module[], fieldName: string): number | void {
    let averageValue = 0;
    let count = 0;
    modules.forEach((m) => {
      const field = m.field(fieldName);
      if (field && field.value) {
        averageValue += +(field.value.toString().replace(',', '.'));
        count++;
      }
    });
    if (count > 0) {
      return (averageValue / count);
    }
  }

  private getTotalValue(modules: Module[], fieldName: string): number | void {
    let totalValue = null;
    modules.forEach((m) => {
      const field = m.field(fieldName);
      if (field && field.value) {
        totalValue += +(field.value.toString().replace(',', '.'));
      }
    });
    return totalValue;
  }

  private getMostRecentValue(modules: Module[], fieldName: string): number | void {
    // TODO: ...
  }

  private getDeviceTypeCount(modules: Module[], deviceType: string): number | void {
    let count = null;
    // TODO: ...
    return count;
  }

}
