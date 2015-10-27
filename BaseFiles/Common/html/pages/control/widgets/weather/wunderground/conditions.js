[{
  Name: 'Weather Underground Widget',
  Author: 'Generoso Martello',
  Version: '2013-03-31',

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/generic/images/wu_logo.png',
  StatusText: '',
  Description: '',

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    //
    if (!this.Initialized) {
      this.Initialized = true;
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function () {
        HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
        HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
        HG.WebApp.ProgramsList.UpdateOptionsPopup();
      });
    }
    //
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
        show: { event: false, ready: true, delay: 1000 },
        events: {
          hide: function () {
            $(this).qtip('destroy');
          }
        },
        hide: { event: false, inactive: 3000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { my: 'top center', at: 'bottom center' }
      });
    }
    else if (display_location == '') {
      widget.find('[data-ui-field=name]').html('Waiting for data...');
      widget.find('[data-ui-field=last_updated_value]').html('Not updated!');
    }
    else {
      widget.find('[data-ui-field=name]').html(display_location);
      //
      var windspeed = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.WindKph').Value;
      widget.find('[data-ui-field=windspeed_value]').html(windspeed+' km/h');
      var rainmm = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.PrecipitationHourMetric').Value;
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
      //
      var display_celsius = (HG.WebApp.Locales.GetTemperatureUnit() == 'Celsius');
      if (display_celsius) {
        var temperaturec = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.TemperatureC').Value;
        widget.find('[data-ui-field=temperature_value]').html(temperaturec + '&#8451;');
      } else {
        var temperaturef = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.TemperatureF').Value;
        widget.find('[data-ui-field=temperature_value]').html(temperaturef + '&#8457;');
      }
      //
      // Forecast data
      for (var f = 1; f <= 3; f++)
      {
        var fIconUrl = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.IconUrl').Value;
        widget.find('[data-ui-field=forecast_' + f + '_icon]').attr('src', this.GetIconUrl(fIconUrl));
        var fDescription = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Description').Value;
        widget.find('[data-ui-field=forecast_' + f + '_desc]').html(fDescription);
        if (display_celsius) {
          var temperatureMinC = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.TemperatureC.Low').Value;
          var temperatureMaxC = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.TemperatureC.High').Value;
          widget.find('[data-ui-field=forecast_' + f + '_tmin]').html(temperatureMinC + '&#8451;');
          widget.find('[data-ui-field=forecast_' + f + '_tmax]').html(temperatureMaxC + '&#8451;');
        } else {
          var temperatureMinF = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.TemperatureF.Low').Value;
          var temperatureMaxF = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.TemperatureF.High').Value;
          widget.find('[data-ui-field=forecast_' + f + '_tmin]').html(temperatureMinF + '&#8457;');
          widget.find('[data-ui-field=forecast_' + f + '_tmax]').html(temperatureMaxF + '&#8457;');
        }
        var displayDate = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Weekday').Value.substr(0, 3) + ', ';
        displayDate += HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Day').Value + ' ';
        displayDate += HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Month').Value;
        widget.find('[data-ui-field=forecast_' + f + '_date]').html(displayDate);
      }
    }

  },

  GetIconUrl: function(url) {
    var localurl = 'wu_clear';
    var fileName = url.substring(url.lastIndexOf('/') + 1, url.length);
    if (fileName.lastIndexOf('.') > 0) {
      fileName = fileName.substring(0, fileName.lastIndexOf('.')).replace('nt_', '');
      switch (fileName) {
        case 'chanceflurries':
          localurl = 'wu_chanceflurries.png';
          break;
        case 'chancerain':
          localurl = 'wu_chancerain.png';
          break;
        case 'chancesleet':
          localurl = 'wu_chancesleet.png';
          break;
        case 'chancesnow':
          localurl = 'wu_chancesnow.png';
          break;
        case 'chancetstorms':
          localurl = 'wu_chancestorm.png';
          break;
        case 'clear':
          localurl = 'wu_clear.png';
          break;
        case 'cloudy':
          localurl = 'wu_cloudy.png';
          break;
        case 'flurries':
          localurl = 'wu_flurries.png';
          break;
        case 'fog':
          localurl = 'wu_fog.png';
          break;
        case 'hazy':
          localurl = 'wu_hazy.png';
          break;
        case 'mostlycloudy':
          localurl = 'wu_partlysunny.png';
          break;
        case 'mostlysunny':
          localurl = 'wu_mostlysunny.png';
          break;
        case 'partlycloudy':
          localurl = 'wu_mostlysunny.png';
          break;
        case 'partlysunny':
          localurl = 'wu_partlysunny.png';
          break;
        case 'sleet':
          localurl = 'wu_sleet.png';
          break;
        case 'rain':
          localurl = 'wu_rain01.png';
          break;
        case 'snow':
          localurl = 'wu_snow.png';
          break;
        case 'sunny':
          localurl = 'wu_clear.png';
          break;
        case 'tstorms':
          localurl = 'wu_thunderstorms01.png';
          break;
        default:
          localurl = 'wu_unknown.png';
          break;
      }
    }
    return 'pages/control/widgets/weather/wunderground/images/'+localurl;
  }

}]