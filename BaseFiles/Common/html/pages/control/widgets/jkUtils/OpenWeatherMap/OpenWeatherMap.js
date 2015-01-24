﻿[{
    Name: "jkUtils - OpenWeatherMap Widget",
    Author: "Jan Koch",
    Version: "2014-05-08 - V2.0",

    GroupName: '',
    IconImage: 'pages/control/widgets/jkUtils/OpenWeatherMap/images/Widget.png',
    Path: 'pages/control/widgets/jkUtils/OpenWeatherMap/',
    StatusText: '',
    Description: '',
    Initialized: false,
    WidgetPrefix: 'jkUtils.OpenWeatherMap.',
    DataUiFields: [],
    DataLocaleFields: [],
    RoundDataUiFields: ['Main.Temp', 'Main.TempMax', 'Main.TempMin', 'Main.Temp.Previous', 'Wind.Deg'],
    TimeDataUiFields: ['Dt', 'LastUpdated'],
    ToggleDataUiFields: ['Snow.H1', 'Snow.H3', 'Snow.H24', 'Snow.Today', 'Rain.H1', 'Rain.H3', 'Rain.H24', 'Rain.Today', 'Main.Pressure', 'Main.PressureSea', 'Main.PressureGround', 'Main.Humidity', 'Wind.Gust', 'Wind.Speed', 'Wind.Deg', 'Clouds.All'],
    DisplayedDataUiFields: [],
    DisplayPreferences: ['Location', 'Custom Color', 'Custom Zoom'], //displayed in the order of this array
    Widget: {},
    Preferences: {},
    Module: {},
    Container: '',

    InitView: function () {

        var _this = this;
        this.Widget = this.Container.find('[data-ui-field=widget]');
        this.DataUiFields = this.GetDataFields(this.Widget, "data-ui-field", "jkUtils.OpenWeatherMap.", false, "^");
        //DEBUG: console.log("Valued in DataUiFields", this.DataUiFields);

        //-- Setup Variables
        for (var i = 0; i < this.RoundDataUiFields.length; i++)
            this.RoundDataUiFields[i] = this.WidgetPrefix + this.RoundDataUiFields[i];

        for (var i = 0; i < this.TimeDataUiFields.length; i++)
            this.TimeDataUiFields[i] = this.WidgetPrefix + this.TimeDataUiFields[i];

        //-- Apply Defined Tooltips To Elements (needs some delay for localization to take place)
        setTimeout(function () {
            this.DataLocaleFields = _this.GetDataFields(_this.Widget.find('.settings .tooltips'), "data-locale-id", _this.WidgetPrefix, false, "^");

            //DEBUG: console.log(tooltipFields);
            for (var i = 0; i < this.DataLocaleFields.length; i++) {
                _this.Widget.find('[data-ui-label="' + this.DataLocaleFields[i].substr(0, this.DataLocaleFields[i].lastIndexOf('.')) + '"]').attr('title', _this.Widget.find('[data-locale-id="' + this.DataLocaleFields[i] + '"]').html());
            }
        }, 3000);

        //-- Setup Refresh Button
        this.Container.find('[data-ui-field=refresh]').on('click', function () {
            HG.Control.Modules.ServiceCall('Control.Refresh', _this.Module.Domain, _this.Module.Address, null, function (data) { });
        });

        //-- Setup Popup Dialog
        this.Preferences = this.Container.find('[data-ui-field=controlpopup]');
        this.Preferences.trigger('create');
        this.Widget.data('ControlPopUp', this.Preferences.popup());
        this.Preferences.find('[data-ui-field=widgeticon]').attr('src', this.Path + 'images/Widget.Preferences.png');

        //-- Make Click on Widget Show Popup
        this.Widget.on('click', function () {
            if (_this.Container.find('[data-ui-field=widget]').data('ControlPopUp'))
                _this.Container.find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
        });


        //-- Setup Popup Settings and Controlers
        var PreferencesList = this.Preferences.find('[data-role=controlgroup]'),
		    PreferencesProperties = this.GetModuleConfigurationProperties(),
		    PreferencesAdditionalHTML = '',
		    PreferencesAppend = [],
			PreferencesElement = -1;

        //-- Create Property Elements
        for (var i = 0; i < PreferencesProperties.length; i++) {

            PreferencesElement = $.inArray(PreferencesProperties[i].Label, this.DisplayPreferences);

            if (PreferencesElement > -1) {
                PreferencesAdditionalHTML = (PreferencesProperties[i].Label == 'Custom Color') ? this.Preferences.find('[data-ui-field=hue-selector]').html() : '';
                PreferencesAppend[PreferencesElement] = '<div class="property"> \
															<p>' + PreferencesProperties[i].Description + '</p> \
															<div class="ui-input-text ui-shadow-inset ui-corner-all ui-btn-shadow ui-body-a" data-ui-field="property"> \
															<input type="text" value="' + PreferencesProperties[i].Value + '" data-parameter-name="' + PreferencesProperties[i].Name + '" onchange=" \
															\
															var openweatherModule = HG.WebApp.Utility.GetModuleByDomainAddress(\'' + this.Module.Domain + '\', \'' + this.Module.Address + '\');\
															openweatherModule.WidgetInstance.UpdateProperty(\'' + PreferencesProperties[i].Name + '\', this.value);\
															\
															" class="ui-input-text ui-body-a"> \
															'+ PreferencesAdditionalHTML + '\
															</div> \
														</div>';
            }
        }

        //-- Append Property Elements in the Order of this.DisplayPreferences-Array
        for (var i in PreferencesAppend)
            PreferencesList.append(PreferencesAppend[i]);

        this.Initialized = true;
    },

    RenderView: function (cuid, module) {

        if (module) this.Module = module;
        if (cuid) this.Container = $(cuid);
        //DEBUG: console.log( "MODULE", this.Module);
        //DEBUG: console.log( "DATA MODULE", HG.WebApp.Data);

        if (!this.Initialized) this.InitView();

        var _this = this;

        //-- Get Weather Icon
        var statusDescription = HG.WebApp.Utility.GetModulePropertyByName(this.Module, this.WidgetPrefix + 'Weather.Description').Value;
        if (statusDescription) this.Widget.find('.widgetIcon > img').attr('title', statusDescription);

        //-- Get Weather Description for Icon
        var statusIcon = HG.WebApp.Utility.GetModulePropertyByName(this.Module, this.WidgetPrefix + 'Weather.Icon').Value;
        if (statusIcon) this.Widget.find('.widgetIcon > img').attr('src', this.Path + 'images/icons/' + statusIcon + '.png');

        //-- Display Data from HGX
        this.DisplayDataFields();

        //-- Rotate Wind Direction Icon According to Value
        if (this.DisplayedDataUiFields[this.WidgetPrefix + 'Wind.Deg']) this.CSSRotate('.windDeg .icon> img', this.DisplayedDataUiFields[this.WidgetPrefix + 'Wind.Deg']);

        //-- Toggle Display of Available Fields
        this.ToggleDataFields();

        //-- Apply Custom Settings
        this.ApplySettings();

        //-- Modify Display According To Data (needs some delay for localization to take place)
        setTimeout(function () {
            _this.ImageByValue(_this.WidgetPrefix + 'Wind', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Wind.Speed').Value, [0, 5, 8, 15, 40]);
            _this.ImageByValue(_this.WidgetPrefix + 'Main.Humidity', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Humidity').Value, [0, 30, 50, 70, 101]);
            _this.ImageByValue(_this.WidgetPrefix + 'Clouds.All', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Clouds.All').Value, [0, 20, 40, 60, 101]);

            _this.ImageByTendency(_this.WidgetPrefix + 'Main.Pressure', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Pressure').Value, HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Pressure.Previous').Value, 0);
            _this.ImageByTendency(_this.WidgetPrefix + 'Main.Pressure.Tendency', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Pressure.Previous').Value, HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Pressure').Value, 1);
            _this.ImageByTendency(_this.WidgetPrefix + 'Main.Temp', HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Temp').Value, HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Main.Temp.Previous').Value, 1);

            if (_this.DisplayedDataUiFields[_this.WidgetPrefix + 'Wind.Deg']) _this.Widget.find('[data-ui-field="' + _this.WidgetPrefix + 'Wind.Deg"]').next().html("&deg; " + _this.DegToText(_this.DisplayedDataUiFields[_this.WidgetPrefix + 'Wind.Deg']));

        }, 1000);

    },

    GetDataFields: function (input, attrib, selector, sorted, matcher) {

        sorted = sorted || true;
        matcher = matcher || "";
        if (sorted)
            return input.find('[' + attrib + matcher + '="' + selector + '"]').sort(function (a, b) {
                return ($(a).attr(attrib) < $(b).attr(attrib)) ? -1 : 1;
            }).map(function () {
                return $(this).attr(attrib);
            })
							.get();
        else
            return input.find('[' + attr + matcher + '="' + selector + '"]').map(function () {
                return $(this).attr(attrib);
            })
							.get();
    },

    DisplayDataFields: function () {

        var d = new Date(0),
			timeString;

        for (var i = 0; i < this.DataUiFields.length; i++) {
            if (HG.WebApp.Utility.GetModulePropertyByName(this.Module, this.DataUiFields[i])) {
                this.DisplayedDataUiFields[this.DataUiFields[i]] = HG.WebApp.Utility.GetModulePropertyByName(this.Module, this.DataUiFields[i]).Value;

                if ($.inArray(this.DataUiFields[i], this.RoundDataUiFields) > -1)
                    this.DisplayedDataUiFields[this.DataUiFields[i]] = Math.round(this.DisplayedDataUiFields[this.DataUiFields[i]] * 10) / 10;

                if ($.inArray(this.DataUiFields[i], this.TimeDataUiFields) > -1) {
                    d = new Date(parseInt(this.DisplayedDataUiFields[this.DataUiFields[i]]) * 1000);
                    this.DisplayedDataUiFields[this.DataUiFields[i]] = d.toLocaleTimeString();
                }

                this.Widget.find('[data-ui-field="' + this.DataUiFields[i] + '"]').html(this.DisplayedDataUiFields[this.DataUiFields[i]]);
            }
        }
    },

    UpdateProperty: function (property, value) {

        //-- Save Settings to Module-Preferences
        HG.WebApp.GroupModules.ModulePropertyAdd(this.Module, property, value);
        var prop = HG.WebApp.Utility.GetModulePropertyByName(this.Module, property);
        prop.NeedsUpdate = 'true';
        HG.WebApp.GroupModules.UpdateModule(this.Module);

        //-- Apply Custom Settings
        //this.ApplySettings();

        //-- Refresh Module Display
        HG.Control.Modules.ServiceCall('Control.Refresh', this.Module.Domain, this.Module.Address, null, function (data) { });

    },

    ApplySettings: function () {

        //-- Apply Color Code as CSS
        this.Widget.find('.y1').css('background-color', 'hsl(' + HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.Custom Color').Value + ', 99%, 69%)');

        //-- Apply Custom Zoom Factor
        this.Widget.css('font-size', HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.Custom Zoom').Value);

    },

    ToggleDataFields: function () {

        var toggleCheck = [],
			cssName;

        //-- Setup Variables
        for (var i = 0; i < this.ToggleDataUiFields.length; i++) {
            toggleCheck[i] = new Array();
            toggleCheck[i]['variable'] = this.WidgetPrefix + this.ToggleDataUiFields[i];
            cssName = this.ToggleDataUiFields[i].replace(/\./g, '');
            toggleCheck[i]['cssName'] = cssName.substring(0, 1).toLowerCase() + cssName.substring(1);
            toggleCheck[i]['value'] = this.DisplayedDataUiFields[toggleCheck[i]['variable']];

            (toggleCheck[i]['value'] != "") ? this.Widget.find('.' + toggleCheck[i]['cssName']).show() : this.Widget.find('.' + toggleCheck[i]['cssName']).hide();
        }
    },

    CSSRotate: function (element, degrees) {

        $(element).css({
            '-webkit-transform': 'rotate(' + degrees + 'deg)',
            '-moz-transform': 'rotate(' + degrees + 'deg)',
            '-ms-transform': 'rotate(' + degrees + 'deg)',
            '-o-transform': 'rotate(' + degrees + 'deg)',
            'transform': 'rotate(' + degrees + 'deg)',
            'zoom': 1
        });
    },

    ImageByValue: function (element, value, valueset) {

        var index = 0,
			src = this.Widget.find('[data-ui-label="' + element + '"] img').first().attr('src').match(/(.*)\.\d\.png$/);
        if (src.length > 1)
            if (valueset.length > 1) {
                for (var i = 1; i < valueset.length; i++) {
                    if (valueset[i - 1] < value && value <= valueset[i])
                        index = i;
                }
                this.Widget.find('[data-ui-label="' + element + '"] img').first().attr('src', src[1] + '.' + index + '.png');
            }
    },

    ImageByTendency: function (element, value1, value2, addLabel) {

        var index = 0,
			src = this.Widget.find('[data-ui-label="' + element + '"] img').first().attr('src').match(/(.*)\.\d\.png$/);

        if (value1 > value2) index = 1;
        if (value2 > value1) index = 2;

        if (src.length > 1) {
            var icon = this.Widget.find('[data-ui-label="' + element + '"] img').first();
            icon.attr('src', src[1] + '.' + index + '.png');

            if (addLabel)

                switch (index) {
                    case 1:
                        icon.attr('title', this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Tendency.Falling"]').html());
                        break;

                    case 2:
                        icon.attr('title', this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Tendency.Rising"]').html());
                        break;

                    default:
                        icon.attr('title', this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Tendency.Unchanged"]').html());
                        break;
                }

        }

        return index;
    },

    DegToText: function (deg) {

        var text = '';

        if (deg > 0) text = "N";
        if (deg > 22.5) text = "NE";
        if (deg > 67.5) text = "E";
        if (deg > 112.5) text = "SE";
        if (deg > 157.5) text = "S";
        if (deg > 202.5) text = "SW";
        if (deg > 247.5) text = "W";
        if (deg > 292.5) text = "NW";
        if (deg > 337.5) text = "N";


        return this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Wind.' + text + '"]').html();
    },

    GetModuleConfigurationProperties: function () {

        var configurationProperties = [],
			counter = 0;

        for (var i = 0; i < this.Module.Properties.length; i++) {
            if (this.Module.Properties[i].Name.indexOf('ConfigureOptions.') > -1) {
                configurationProperties[counter] = [];
                configurationProperties[counter]['Name'] = this.Module.Properties[i].Name;
                configurationProperties[counter]['Label'] = this.Module.Properties[i].Name.substring(17);
                configurationProperties[counter]['Value'] = this.Module.Properties[i].Value;
                configurationProperties[counter]['Description'] = this.Module.Properties[i].Description;
                counter++;
            }
        }

        return configurationProperties;

    }

}]