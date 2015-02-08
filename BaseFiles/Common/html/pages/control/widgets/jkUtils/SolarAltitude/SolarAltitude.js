[{
    Name: "jkUtils - Solar Altitude (formerly known as: SolarAltitude Twilight Widget)",
    Author: "Jan Koch",
    Version: "v2.0 2014-05-07",

    GroupName: '',
    IconImage: 'pages/control/widgets/jkUtils/SolarAltitude/images/Widget.png',
    Path: 'pages/control/widgets/jkUtils/SolarAltitude/',
    StatusText: '',
    Description: '',
    Initialized: false,
    WidgetPrefix: 'jkUtils.SolarAltitude.',
    DataUiFields: [],
    DataUiFieldsDisplayed: [],
    DataTimers: [],
    DisplayPreferences: ['Label', 'Timeformat', 'Color', 'Zoom', 'Latitude', 'Longitude'], //displayed in the order of this array
    DisplayStatusIcons: [
						 ['Morning.Night.End', 'till'],
						 ['Morning.Astronomical.Start', 'since'],
						 ['Morning.Nautical.Start', 'since'],
						 ['Morning.Civil.Start', 'since'],
						 ['Morning.Sunrise.Start', 'since'],
						 ['Morning.Sunrise.End', 'at'],
						 ['Morning.GoldenHour.Start', 'since'],
						 ['Morning.GoldenHour.End', 'since'],
						 ['Day.Noon', 'at'],
						 ['Evening.GoldenHour.Start', 'till'],
						 ['Evening.GoldenHour.End', 'till'],
						 ['Evening.Sunset.Start', 'at'],
						 ['Evening.Sunset.End', 'till'],
						 ['Evening.Civil.End', 'till'],
						 ['Evening.Nautical.End', 'till'],
						 ['Evening.Astronomical.End', 'till'],
						 ['Evening.Night.Start', 'since'],
						 ['Night.Nadir', 'at']
    ],
    Widget: {},
    Preferences: {},
    Module: {},
    Container: '',
    DimensionXMain: 1000,
    DimensionYMain: 0,

    InitView: function () {

        var _this = this;
        this.Widget = this.Container.find('[data-ui-field=widget]');
        this.DataUiFields = this.GetDataFields(this.Widget, "data-ui-field", this.WidgetPrefix, false, "^");
        //DEBUG: console.log("Valued in DataUiFields", this.DataUiFields);

        //var displayedFields = this.DisplayDataFields(this.Module, this.Widget, this.DataUiFields);
        this.DataUiFieldsDisplayed = this.DisplayDataFields(this.Module, this.Widget, this.DataUiFields);
        //DEBUG: console.log("DisplayFields", this.DataUiFieldsDisplayed);

        var iconFields = this.GetDataFields(this.Widget.find('.settings .icons'), 'data-locale-id', this.WidgetPrefix, false, '^');
        //DEBUG: console.log("IconFields",iconFields);

        //-- Convert Time-Stamps to AM/PM if Required
        if (HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.' + this.WidgetPrefix + 'Timeformat').Value.toLowerCase() == 'true')
            this.ConvertTime();

        //-- Setup Timers for Status Icon
        this.SetupTimers();



        $(window).resize(function () {
            _this.Resize();
        });



        //-- Setup Refresh Button // currently not supported by Javascript HG Script
        /*
		this.Container.find( '[data-ui-field=refresh]' ).on( 'click', function(){

			HG.Control.Modules.ServiceCall( 'Control.Refresh', _this.Module.Domain, _this.Module.Address, null, function (data) { } );
		});
		*/

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

        //-- Prepare List Of Property Elements
        for (var i in this.DisplayPreferences)
            this.DisplayPreferences[i] = this.WidgetPrefix + this.DisplayPreferences[i];

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
                PreferencesAdditionalHTML = (PreferencesProperties[i].Label == this.WidgetPrefix + 'Color') ? this.Preferences.find('[data-ui-field=hue-selector]').html() : '';
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

        //-- Apply Defined Tooltips To Elements and
        //   Trigger Some Other Routines That Depend on Completed Localization
        //   (needs some delay for localization to take place)
        setTimeout(function () {

            var tooltipFields = _this.GetDataFields(_this.Widget.find('.settings .tooltips'), "data-locale-id", _this.WidgetPrefix, false, "^");

            for (var i = 0; i < tooltipFields.length; i++)
                _this.Widget.find('[data-ui-field="' + tooltipFields[i].substr(0, tooltipFields[i].lastIndexOf('.')) + '"]').attr('title', _this.Widget.find('[data-locale-id="' + tooltipFields[i] + '"]').html());

            //-- Set Label To Waxing/Waning For Moon Phase
            (HG.WebApp.Utility.GetModulePropertyByName(_this.Module, _this.WidgetPrefix + 'Moon.Waxing').Value) ?
			_this.Widget.find('[data-ui-field="' + _this.WidgetPrefix + 'Moon.Waxing"]').html(_this.Widget.find('[data-locale-id="' + _this.WidgetPrefix + 'Moon.Waning"]').html()) :
			_this.Widget.find('[data-ui-field="' + _this.WidgetPrefix + 'Moon.Waxing"]').html(_this.Widget.find('[data-locale-id="' + _this.WidgetPrefix + 'Moon.Waxing"]').html());

            //-- Trigger Rezise
            _this.Resize();

            //-- Trigger Update
            _this.UpdateStatusIcon();

        }, 2000);

        //-- Call Update Status-Icon Function and Register Auto-Refresh
        this.interval = setInterval(function () { _this.UpdateStatusIcon() }, 30000);

        this.Initialized = true;

    },

    RenderView: function (cuid, module) {

        if (module) this.Module = module;
        if (cuid) this.Container = $(cuid);
        //DEBUG: console.log( "MODULE", this.Module);
        //DEBUG: console.log( "DATA MODULE", HG.WebApp.Data);

        if (!this.Initialized) this.InitView();

        //-- Apply Custom Settings
        this.ApplySettings();

        //-- Display Module-Label If Set By User Config
        if (HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.' + this.WidgetPrefix + 'Label').Value != '') {
            var labelPlaceholder = this.Widget.find('[data-ui-label="' + this.WidgetPrefix + 'Label"]');
            labelPlaceholder.html(HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.' + this.WidgetPrefix + 'Label').Value);
            labelPlaceholder.removeAttr('data-locale-id');
        }
        this.UpdateStatusIcon();

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

    DisplayDataFields: function (module, widget, dataFields) {
        var displayedFields = new Array();

        for (var i = 0; i < dataFields.length; i++) {
            if (HG.WebApp.Utility.GetModulePropertyByName(module, dataFields[i])) {
                displayedFields[dataFields[i]] = HG.WebApp.Utility.GetModulePropertyByName(module, dataFields[i]).Value;
                this.Widget.find('[data-ui-field="' + dataFields[i] + '"]').html(displayedFields[dataFields[i]]);
            }
        }

        return displayedFields;
    },

    SetupTimers: function () {

        var timer = '',
			label, icon;

        for (var i = 0; i < this.DisplayStatusIcons.length; i++) {

            if (this.DisplayStatusIcons[i][0].substr(2, 1) == ':')
                timer = this.DisplayStatusIcons[i][0];
            else
                timer = this.Widget.find('[data-ui-field="' + this.WidgetPrefix + this.DisplayStatusIcons[i][0] + '"]').html();


            if (this.DisplayStatusIcons[i][2])
                label = this.WidgetPrefix + this.DisplayStatusIcons[i][2];
            else
                label = this.WidgetPrefix + this.DisplayStatusIcons[i][0];


            if (this.DisplayStatusIcons[i][3])
                icon = this.DisplayStatusIcons[i][3];
            else
                icon = this.DisplayStatusIcons[i][0] + '.png';


            this.DataTimers.push({
                label: label,
                time: this.ParseTime(timer),
                timeStr: timer,
                timePrefix: this.DisplayStatusIcons[i][1],
                icon: icon
            });

        }

        this.DataTimers.sort(function (date1, date2) {
            return (date1.time < date2.time) ? -1 : 1;
        });

        //DEBUG: console.log("TIMERS", this.DataTimers);

    },

    ParseTime: function (timestring) {
        var d = new Date();
        var time = timestring.match(/(\d+)(?::(\d\d))?\s*(p?)/);
        d.setHours(parseInt(time[1]) + (time[3] ? 12 : 0));
        d.setMinutes(parseInt(time[2]) || 0);

        return d;

    },

    ConvertTime: function () {

        //DEBUG: console.log( "DISPLAYED",this.DataUiFieldsDisplayed );

        for (var i in this.DataUiFieldsDisplayed)
            if (this.DataUiFieldsDisplayed[i].substr(2, 1) == ':') {
                var timeString = this.DataUiFieldsDisplayed[i];
                var H = +timeString.substr(0, 2);
                var h = H % 12 || 12;
                var ampm = H < 12 ? " am" : " pm";
                timeString = h + timeString.substr(2, 3) + ampm;
                this.Widget.find('[data-ui-field="' + i + '"]').html(timeString);
            }

    },

    UpdateStatusIcon: function () {

        var now = new Date(),
			currentTime = 0,
		    icon, description, offset, closestOffset, phase,
			timePrefix = [];
        var widgetIconLabel = this.Widget.find('.widgetIconLabel');

        timePrefix['till'] = this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Evening.String.Till"]').html();
        timePrefix['at'] = this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Evening.String.At"]').html();
        timePrefix['since'] = this.Widget.find('[data-locale-id="' + this.WidgetPrefix + 'Evening.String.Since"]').html();

        //-- Find Closest Timestamp To Current Time
        closestOffset = Infinity;
        for (var i = 0; i < this.DataTimers.length; ++i) {
            var loopTimer = this.DataTimers[i].time;
            offset = Math.abs(+loopTimer - +now);
            if (offset < closestOffset) {
                closestOffset = offset;
                currentTime = i;
            }
        }

        //-- Jump To Next Timestamp When Time Is Past 'till' Or 'at'
        if (now > this.DataTimers[currentTime].time &&
		    (this.DataTimers[currentTime].timePrefix == 'till' || this.DataTimers[currentTime].timePrefix == 'at')) currentTime++;

        //-- Jump Back To Previous Timestamp When Time Before 'since'
        if (now < this.DataTimers[currentTime].time &&
		    this.DataTimers[currentTime].timePrefix == 'since') currentTime--;

        //-- Setup Display Text
        switch (this.DataTimers[currentTime].timePrefix) {

            case "till":
                description = '<span>' + timePrefix['till'] + '</span><br/>';
                break;

            case "at":
                description = '<span>' + timePrefix['at'] + '</span><br/>';
                break;

            case "since":
                description = '<span>' + timePrefix['since'] + '</span><br/>';
                break;

            default:
                description = '';
                break;

        }

        description += '<span>' + this.DataTimers[currentTime].timeStr + '</span><br/>';

        if (this.DataTimers[currentTime + 1])
            if (this.DataTimers[currentTime + 1].timePrefix == 'at') {
                description += '<br/><span class="i">' + this.Widget.find('[data-locale-id="' + this.DataTimers[currentTime + 1].label + '"]').html() + '</span><br/>';
                description += '<span class="i">' + timePrefix['at'] + '</span><br/>';
                description += '<span class="i">' + this.DataTimers[currentTime + 1].timeStr + '</span><br/>';
            }

        phase = this.Widget.find('[data-locale-id="' + this.DataTimers[currentTime].label + '"]').html();

        //-- Display Status Text
        widgetIconLabel.find('.description').html(description);
        widgetIconLabel.find('.phase').html(phase).attr('title', phase);

        //DEBUG: console.log("CURRENT TIME OBJECT", this.DataTimers[currentTime]);

        //-- Set Status Icon
        icon = this.DataTimers[currentTime].label.replace(new RegExp(this.WidgetPrefix, 'g'), '');
        icon = this.Path + 'images/status/' + icon + '.png';
        this.Widget.find('.widgetIcon > img').attr('src', icon).attr('title', phase);;


    },

    CalcFullSize: function () {

        var overallWidth = 0,
			rowHeight = 0;

        this.Widget.find('.main > div').not('.dmin').each(function (index, elem) {
            overallWidth += $(elem).width();
        });

        this.DimensionXMain = overallWidth;

        this.Widget.find('.main div:first-child > div.y1').not('.dmin').each(function (index, elem) {
            rowHeight += $(elem).height();
        });

        this.DimensionYMain = rowHeight;
    },

    Resize: function () {

        this.CalcFullSize();
        //DEBUG: console.log("Container:", this.Widget.find('#centerContainer').width(), " Min Width:", this.WidgetFullSize);
        if (this.Widget.find('.center').width() < this.DimensionXMain) {
            this.Widget.find('.dmin').show();
            this.Widget.find('.dfull').hide();
        } else {
            this.Widget.find('.dmin').hide();
            this.Widget.find('.dfull').show();
        }

        if (this.DimensionYMain)
            this.Widget.find('.main div.flexY').height(this.DimensionYMain);


    },

    UpdateProperty: function (property, value) {

        //-- Save Settings to Module-Preferences
        HG.WebApp.GroupModules.ModulePropertyAdd(this.Module, property, value);
        var prop = HG.WebApp.Utility.GetModulePropertyByName(this.Module, property);
        prop.NeedsUpdate = 'true';
        HG.WebApp.GroupModules.UpdateModule(this.Module);

        //-- Apply Custom Settings
        this.ApplySettings();

        //-- Trigger Resize
        this.Resize();

        //-- Refresh Module Display
        HG.Control.Modules.ServiceCall( 'Control.Refresh', this.Module.Domain, this.Module.Address, null, function (data) { } );

    },

    ApplySettings: function () {

        //-- Apply Color Code as CSS
        this.Widget.find('.y1').css('background-color', 'hsl(' + HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.' + this.WidgetPrefix + 'Color').Value + ', 99%, 69%)');

        //-- Apply Custom Zoom Factor
        this.Widget.css('font-size', HG.WebApp.Utility.GetModulePropertyByName(this.Module, 'ConfigureOptions.' + this.WidgetPrefix + 'Zoom').Value);

    },

    GetModuleConfigurationProperties: function () {

        var configurationProperties = [],
			counter = 0;

        for (var i = 0; i < this.Module.Properties.length; i++) {
            if (this.Module.Properties[i].Name.indexOf('ConfigureOptions.' + this.WidgetPrefix) > -1) {
                configurationProperties[counter] = [];
                configurationProperties[counter]['Name'] = this.Module.Properties[i].Name;
                configurationProperties[counter]['Label'] = this.Module.Properties[i].Name.substring(17);
                configurationProperties[counter]['Value'] = this.Module.Properties[i].Value;
                configurationProperties[counter]['Description'] = this.Module.Properties[i].Description;
                counter++;
            }
        }
        //DEBUG: console.log("Properties",configurationProperties);
        return configurationProperties;

    }


}]