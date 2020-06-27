[{
  Name: 'Weather Widget',
  Author: 'Generoso Martello',
  Version: '2013-03-31',

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/environment/weather/images/partlycloudy.png',
  StatusText: '',
  Description: '',

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');

    if (!this.Initialized) {
      this.Initialized = true;
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function () {
        HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
        HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
        HG.WebApp.ProgramsList.UpdateOptionsPopup();
      });
    }

    var display_location = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.DisplayLocation').Value;
    var serviceapi = HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.ApiKey').Value;
    if (serviceapi == '' || serviceapi == '?') {
      widget.find('[data-ui-field=name]').html('Not configured');
      widget.find('[data-ui-field=sunrise_value]').html(sunrise);
      //
      widget.find('[data-ui-field=settings]').qtip({
        content: {
          title: HG.WebApp.Locales.GetLocaleString('control_widget_notconfigured_title'),
          text: HG.WebApp.Locales.GetLocaleString('control_widget_notconfigured_text'),
          button: HG.WebApp.Locales.GetLocaleString('control_widget_notconfigured_button')
        },
        show: { event: false, ready: true, delay: 3000 },
        events: {
          hide: function () {
            $(this).qtip('destroy');
          }
        },
        hide: { event: false, inactive: 3000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { my: 'top center', at: 'bottom center' }
      });
    } else if (display_location == '') {
      widget.find('[data-ui-field=name]').html('Waiting for data...');
      widget.find('[data-ui-field=last_updated_value]').html('Not updated!');
    } else {
      widget.find('[data-ui-field=name]').html(display_location);
      //
      var windspeed = HG.WebApp.Utility.GetModulePropertyByName(module, 'Sensor.Wind.Speed').Value;
      widget.find('[data-ui-field=windspeed_value]').html(windspeed+' km/h');
      var rainmm = HG.WebApp.Utility.GetModulePropertyByName(module, 'Sensor.Precipitation.Rain').Value;
      widget.find('[data-ui-field=rainmm_value]').html(rainmm+' mm');
      var sunrise = HG.WebApp.Utility.GetModulePropertyByName(module, 'Astronomy.Sunrise').Value;
      widget.find('[data-ui-field=sunrise_value]').html(sunrise);
      var sunset = HG.WebApp.Utility.GetModulePropertyByName(module, 'Astronomy.Sunset').Value;
      widget.find('[data-ui-field=sunset_value]').html(sunset);
      //
      var iconurl = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.IconUrl').Value;
      widget.find('[data-ui-field=icon]').attr('src', this.GetIconUrl(iconurl));
      //
      var icontext = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Description').Value;
      widget.find('[data-ui-field=description]').html(icontext);
      //
      var last_updated = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.LastUpdated').Value;
      widget.find('[data-ui-field=last_updated_value]').html(last_updated);

      // Internally temperature is always expressed in Celsius, then converted to user locale unit
      var temperature = HG.WebApp.Utility.GetModulePropertyByName(module, 'Sensor.Temperature').Value;
      temperature = parseFloat(HG.WebApp.Utility.GetLocaleTemperature(parseFloat(temperature.replace(',','.'))));
      widget.find('[data-ui-field=temperature_value]').html(temperature + '&#8451;');

      // Forecast data
      for (var f = 1; f <= 3; f++)
      {
        var fIconUrl = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.IconUrl').Value;
        widget.find('[data-ui-field=forecast_' + f + '_icon]').attr('src', this.GetIconUrl(fIconUrl));
        var fDescription = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Description').Value;
        widget.find('[data-ui-field=forecast_' + f + '_desc]').html(fDescription);

        var temperatureMin = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Temperature.Min').Value;
        temperatureMin = parseFloat(HG.WebApp.Utility.GetLocaleTemperature(parseFloat(temperatureMin.replace(',','.'))));
        var temperatureMax = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Temperature.Max').Value;
        temperatureMax = parseFloat(HG.WebApp.Utility.GetLocaleTemperature(parseFloat(temperatureMax.replace(',','.'))));
        widget.find('[data-ui-field=forecast_' + f + '_tmin]').html(temperatureMin + '&#8451;');
        widget.find('[data-ui-field=forecast_' + f + '_tmax]').html(temperatureMax + '&#8451;');

        var displayDate = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Weekday').Value.substr(0, 3) + ', ';
        displayDate += HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Day').Value + ' ';
        displayDate += HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Month').Value;
        widget.find('[data-ui-field=forecast_' + f + '_date]').html(displayDate);
      }
    }
  },

  GetIconUrl: function(icon) {
    return 'pages/control/widgets/homegenie/environment/weather/images/'+icon+".png";
  }
}]
