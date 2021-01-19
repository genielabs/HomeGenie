import {Component} from '@angular/core';
import {WidgetBase} from "../widget-base";
import {ModuleField} from "../../services/hgui/module";
import {FLD} from "../../services/hgui/hgui.service";

export class WeatherForecastData {
  public location: {
    name: ModuleField,
    country: ModuleField
  }
  public astronomy: {
    sunrise: ModuleField;
    sunset: ModuleField;
  };
  public today: {
    date?: string | Date;
    icon: ModuleField;
    description: ModuleField;
    temperatureC: ModuleField;
    pressureMb: ModuleField,
    wind: {
      speedKph: ModuleField,
      direction: ModuleField
    },
    precipitation: {
      snowMm: ModuleField,
      rainMm: ModuleField
    }
  }
  public forecast: {
    date: string | Date;
    icon: ModuleField;
    description: ModuleField;
    temperature: ModuleField;
    minC: ModuleField;
    maxC: ModuleField;
  }[];
}

@Component({
  selector: 'app-weather-forecast',
  templateUrl: './weather-forecast.component.html',
  styleUrls: ['./weather-forecast.component.scss']
})
export class WeatherForecastComponent extends WidgetBase {

  get todayDate(): Date {
    return new Date();
  }

  get data(): WeatherForecastData {
    if (super.data && super.data.location) {
      return super.data;
    }
    /*
    // else return static demo data
    const forecastDate1 = new Date();
    forecastDate1.setDate(forecastDate1.getDate() + 1);
    const forecastDate2 = new Date();
    forecastDate2.setDate(forecastDate1.getDate() + 2);
    const forecastDate3 = new Date();
    forecastDate3.setDate(forecastDate1.getDate() + 3);
    return {
      location: {
        name: null,
      },
      astronomy: { sunrise: '06:24' , sunset: '17.32' },
      today: {
        date: new Date(),
        icon: 'partly-cloudy-day-drizzle',
        description: 'Partly Cloudy',
        temperatureC: 18.2,
        pressureMb: 3.2,
        wind: {
          speedKph: 13.1,
          direction: 'N/E'
        },
        precipitation: {
          rainMm: 2.1,
          snowMm: 12.2
        }
      },
      forecast: [
        {
          date: forecastDate1,
          description: 'Thunderstorms',
          icon: 'thunderstorms',
          minC: 12.3,
          maxC: 16.1
        },
        {
          date: forecastDate2,
          description: 'Partly Cloudy',
          icon: 'partly-cloudy-day-rain',
          minC: 12.3,
          maxC: 16.1
        },
        {
          date: forecastDate3,
          description: 'Clear day',
          icon: 'clear-day',
          minC: 12.3,
          maxC: 16.1
        }
      ]
    };
    */
  }

  onModuleOptionsClick(e): void {
    this.showOptions.emit(null);
  }

  trackByFn(index, race) {
    return undefined;
  }
}
