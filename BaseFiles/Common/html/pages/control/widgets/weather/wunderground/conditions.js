[{
    Name: 'Weather Underground Widget',
    Author: 'Generoso Martello',
    Version: '2013-03-31',

    GroupName: '',
    IconImage: 'http://icons-ak.wxug.com/graphics/wu2/logo_130x80.png',
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
            widget.find('[data-ui-field=name]').html('Not configured.');
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
            var sunrise = HG.WebApp.Utility.GetModulePropertyByName(module, 'Astronomy.Sunrise').Value;
            widget.find('[data-ui-field=sunrise_value]').html(sunrise);
            //
            var sunset = HG.WebApp.Utility.GetModulePropertyByName(module, 'Astronomy.Sunset').Value;
            widget.find('[data-ui-field=sunset_value]').html(sunset);
            //
            var iconurl = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.IconUrl').Value;
            widget.find('[data-ui-field=icon]').attr('src', iconurl);
            //
            var icontext = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Description').Value;
            widget.find('[data-ui-field=description]').html(icontext);
            //
            var last_updated = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.LastUpdated').Value;
            widget.find('[data-ui-field=last_updated_value]').html(last_updated);
            //
            var display_celsius = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.DisplayCelsius').Value;
            if (display_celsius == 'TRUE') {
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
                widget.find('[data-ui-field=forecast_' + f + '_icon]').attr('src', fIconUrl);
                var fDescription = HG.WebApp.Utility.GetModulePropertyByName(module, 'Conditions.Forecast.' + f + '.Description').Value;
                widget.find('[data-ui-field=forecast_' + f + '_desc]').html(fDescription);
                if (display_celsius == 'TRUE') {
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

    }

}]