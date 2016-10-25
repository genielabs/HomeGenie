//
// namespace : HG.Ui namespace
// info      : -
//
HG.Ui = HG.Ui || new function(){ var $$ = this;

    $$._widgetQueueCount = 0;
    $$._widgetCache = [];

    var ParameterType = {
        Status_ColorHsb:
            'Status.ColorHsb',
        Program_Status:
            'Program.Status',
        Program_UiRefresh:
            'Program.UiRefresh',
        Widget_DisplayIcon:
            'Widget.DisplayIcon',
        Widget_DisplayModule:
            'Widget.DisplayModule',
        Security_Armed:
            'HomeGenie.SecurityArmed',
        Security_Triggered:
            'HomeGenie.SecurityTriggered',
        /* other common params from https://github.com/genielabs/mig-service-dotnet/blob/master/MIG.HomeAutomation/ModuleEvents.cs */
        VirtualMeter_Watts:
            'VirtualMeter.Watts',
        Status_Level:
            'Status.Level',
        Status_DoorLock:
            'Status.DoorLock',
        Status_Battery:
            'Status.Battery',
        Status_Error:
            'Status.Error',
        Meter_KwHour:
            'Meter.KilowattHour',
        Meter_KvaHour:
            'Meter.KilovoltAmpereHour',
        Meter_Watts:
            'Meter.Watts',
        Meter_Pulses:
            'Meter.Pulses',
        Meter_AcVoltage:
            'Meter.AcVoltage',
        Meter_AcCurrent:
            'Meter.AcCurrent',
        Sensor_Power:
            'Sensor.Power',
        Sensor_Generic:
            'Sensor.Generic',
        Sensor_MotionDetect:
            'Sensor.MotionDetect',
        Sensor_Temperature:
            'Sensor.Temperature',
        Sensor_Luminance:
            'Sensor.Luminance',
        Sensor_Humidity:
            'Sensor.Humidity',
        Sensor_DoorWindow:
            'Sensor.DoorWindow',
        Sensor_Key:
            'Sensor.Key',
        Sensor_Alarm:
            'Sensor.Alarm',
        Sensor_CarbonMonoxide:
            'Sensor.CarbonMonoxide',
        Sensor_CarbonDioxide:
            'Sensor.CarbonDioxide',
        Sensor_Smoke:
            'Sensor.Smoke',
        Sensor_Heat:
            'Sensor.Heat',
        Sensor_Flood:
            'Sensor.Flood',
        Sensor_Tamper:
            'Sensor.Tamper',
        Receiver_RawData:
            'Receiver.RawData',
        Receiver_Status:
            'Receiver.Status',
        ZwaveNode_WakeUpSleepingStatus:
            'ZWaveNode.WakeUpSleepingStatus',
        // Thermostat parameters
        Thermostat_Mode:
            'Thermostat.Mode',
        Thermostat_OperatingState:
            'Thermostat.OperatingState',
        Thermostat_SetPoint_Cooling:
            'Thermostat.SetPoint.Cooling',
        Thermostat_SetPoint_Heating:
            'Thermostat.SetPoint.Heating',
        Thermostat_FanState:
            'Thermostat.FanState',
        Thermostat_FanMode:
            'Thermostat.FanMode',
        // UPnP parameters
        UPnP_FriendlyName:
            'UPnP.FriendlyName',
        UPnP_RendererState_MediaItem:
            'Widget.State.MediaItem',
				// New MySensor types
        Sensor_Flow:
						'Sensor.Flow',
				Sensor_Volume:
						'Sensor.Volume',
				Sensor_Distance:
						'Sensor.Distance',
				Sensor_Infrared:
						'Sensor.Infrared',
				Sensor_UV:
						'Sensor.UV',
    };

    var ModuleType = {
        Switch:
            'Switch',
        Light:
            'Light',
        Dimmer:
            'Dimmer',
        Sensor:
            'Sensor',
        Thermostat:
            'Thermostat',
        DoorWindow:
            'DoorWindow',
        DoorLock:
            'DoorLock',
        Shutter:
            'Shutter',
        Siren: 
            'Siren',
        MediaTransmitter:
            'MediaTransmitter',
        MediaReceiver:
            'MediaReceiver',
        Program:
            'Program'
    };

    Module = {
        getDoubleValue: function(v) {
            var value = 0;
            try { value = parseFloat(v.toString().replace(',', '.')); } catch(e) { console.log(e); }
            return value;
        },
        getFormattedNumber: function(v) {
            return (Math.round(this.getDoubleValue(v)*10)/10).toString();
        }
    };

    $$.GenerateWidget = function(fieldType, context, callback) {
        // fieldType: 
        //    widgets/text, widgets/password, widgets/checkbox, widgets/slider, 
        //    widgets/store.text, widgets/store.password, widgets/store.checkbox,
        //    widgets/store.list, store.edit
        //    core/popup.cronwizard
        var widgetWrapper = $('<div/>');
        context.parent.append(widgetWrapper);
        var options = [];
        if (fieldType.indexOf(':') > 0) {
            options = fieldType.split(':');
            fieldType = options[0];
            options.shift();
        }
        // pick it from cache if exists
        var cached = false;
        $.each($$._widgetCache, function(k,v){
            if (v.widget == fieldType) {
                var element = $(v.html);
                var handler = v.json.startsWith('[') ? eval(v.json)[0] : eval('new function(){ var $$ = this; '+v.json+' };');
                element.one('create', function() {
                    handler.element = element;
                    handler.context = context;
                    setTimeout(function(){
                        callback(handler);
                    }, 200);
                    if (handler.init) handler.init(options);
                    if (handler.bind) handler.bind();
                    widgetWrapper.show();
                });
                widgetWrapper.hide();
                widgetWrapper.append(element);
                element.trigger('create');
                cached = true;
                return false;
            }
        });
        if (cached) return widgetWrapper;
        // ... or load it from web
        if ($$._widgetQueueCount++ == 0)
            $.mobile.loading('show');
        $.ajax({
            url: "ui/" + fieldType + ".html",
            dataType: 'text',
            type: 'GET',
            success: function (htmlData) {
                var element = $(htmlData);
                $.ajax({
                    url: "ui/" + fieldType + ".js",
                    dataType: 'text',
                    type: 'GET',
                    success: function (jsonData) {
                        $$._widgetCache.push({ widget: fieldType, html: htmlData, json: jsonData });
                        var handler = null;
                        try { handler = jsonData.startsWith('[') ? eval(jsonData)[0] : eval('new function(){ var $$ = this; '+jsonData+' };'); }
                        catch (e) { console.log(e); callback(null); return; }
                        element.one('create', function() {
                            handler.element = element;
                            handler.context = context;
                            callback(handler);
                            if (handler.init) handler.init(options);
                            if (handler.bind) handler.bind();
                            widgetWrapper.show();
                        });
                        widgetWrapper.hide();
                        widgetWrapper.append(element);
                        element.trigger('create');
                        if (--$$._widgetQueueCount == 0) {
                            $.mobile.loading('hide');
                        }
                    },
                    error: function (data) {
                        console.log(data);
                        if (callback != null) callback(null);
                    }
                });
            },
            error: function (data) {
                if (callback != null) callback(null);
            }
        });
        return widgetWrapper; 
    };

    $$.GetModuleIcon = function(module, callback, elid) {
        var icon = 'pages/control/widgets/homegenie/generic/images/unknown.png';
        if (module != null && typeof module.DeviceType != 'undefined' && module.DeviceType != '') {
            var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, ParameterType.Widget_DisplayIcon);
            var widget = HG.WebApp.Utility.GetModulePropertyByName(module, ParameterType.Widget_DisplayModule);
            if (widget != null && widget.Value != '') {
                widget = widget.Value;
            } else {
                widget = 'homegenie/generic/' + module.DeviceType.toLowerCase();
            }
            if (widgeticon != null && widgeticon.Value != '') {
                icon = widgeticon.Value;
            } else if (typeof module.WidgetInstance != 'undefined') {
                if (typeof module.WidgetInstance.widget != 'undefined' && typeof module.WidgetInstance.widget.icon != 'undefined')
                    icon = module.WidgetInstance.widget.icon;
                else // Compatibility fall-back for old widget format
                    icon = module.WidgetInstance.IconImage;
            } else {
                // get reference to generic type widget 
                HG.WebApp.WidgetsList.GetWidgetIcon(widget, elid, callback);
                return icon;
            }
        }
        if (callback != null) callback(icon, elid);
        return icon;
    };

    $$.GetModuleDisplayName = function(module) {
        var name = module.Domain + ' ' + module.Address;
        try {
            name = module.Name;
            name += ' ('+module.Domain.substring(module.Domain.lastIndexOf('.')+1)+' '+module.Address+')';
            name = name.trim();
        } catch(e) { }
        return name;
    };

    $$.SwitchPopup = function(popup_id1, popup_id2, notransition) {
        var switchfn = function( event, ui ) {
            if (notransition == true)
            {
                setTimeout(function () { $(popup_id2).popup('open'); }, 10);
            }
            else
            {
                setTimeout(function () { $(popup_id2).popup('open', { transition: 'pop' }); }, 100);
            }
        };
        $(popup_id1).one('popupafterclose', switchfn);
        $(popup_id1).popup('close');
    };

    $$.BlinkAnim = function(element, repeatCount) {
      var count = typeof repeatCount != 'undefined' ? repeatCount*2 : 8;
      var animate = function() {
          element.animate({
            opacity: 'toggle'
          }, {
            duration: 250,
            specialEasing: {
              width: "easeInOutExpo",
              height: "easeOutBounce"
            },
            complete: function() {
                if (--count > 0)
                  animate();
            }
          });
      }
      element.clearQueue();
      element.finish();
      animate();
    };

    $$.ScrollTo = function (element, delay, container, callback) {
        if (typeof container == 'undefined')
            container = $('html, body');
        var scroll = container.scrollTop()+($(element).offset().top-container.offset().top);
        container.animate({
            scrollTop: scroll
        }, delay, callback);
    };

    $$.SetTheme = function (theme) {
        HG.WebApp.Store.set('UI.Theme', theme);
        $(document).find('.ui-page')
                .removeClass('ui-page-theme-a ui-page-theme-b ui-page-theme-c ui-page-theme-d ui-page-theme-e ui-page-theme-f ui-page-theme-g ui-page-theme-h')
                .addClass('ui-page-theme-' + theme);
        $(document).find('.ui-mobile-viewport')
                .removeClass('ui-overlay-a ui-overlay-b ui-overlay-c ui-overlay-d ui-overlay-e ui-overlay-f ui-overlay-g ui-overlay-h')
                .addClass('ui-overlay-' + theme);
        $(document).find('.ui-popup')
                .removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g ui-body-h')
                .addClass('ui-body-' + theme);
        $(document).find('.ui-loader')
                .removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g ui-body-h')
                .addClass('ui-body-' + theme);
        return;
    };

    $$.EditModule = function(module) {
        HG.WebApp.Control.EditModule(module);
    }

    $$.ConfigureProgram = function(module) {
        HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
        HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
        HG.WebApp.ProgramsList.UpdateOptionsPopup();    
    }

    $$.GetParameterContext = function(module, parameter, value) {
        var name = '';
        var icon = 0;
        var desc = "";
        var hideable = false;
        var isActive = false;
        var isStatusParam = false;
        var unknown = false;
        // TODO: localize parameter names, eg:
        // name = HG.WebApp.Utility.GetLocalizedString(parameter)
        try {
            switch (parameter) {
                case ParameterType.Status_Level:
                case ParameterType.Sensor_Generic:
                case ParameterType.Status_DoorLock:
                case ParameterType.Sensor_DoorWindow:
                case ParameterType.Sensor_MotionDetect:
                    isStatusParam = true;
                    //hideable = true;
                    if (parameter != ParameterType.Sensor_MotionDetect && 
                        (parameter == ParameterType.Sensor_DoorWindow
                            || parameter == ParameterType.Status_DoorLock
                            || ((module != null && module.DeviceType != '') && (module.DeviceType == ModuleType.DoorWindow || module.DeviceType == ModuleType.DoorLock)))) {
                        // Door / Window sensor or Door Lock
                        if (Module.getDoubleValue(value) == 0) {
                            icon = 'images/indicators/door.png';
                            desc = "Closed";
                        } else {
                            icon = 'images/indicators/door.png';
                            desc = "Open";
                            isActive = true;
                        }
                    } else {
                        // Other generic sensor
                        if (Module.getDoubleValue(value) == 0) {
                            icon = 'images/indicators/level.png';
                            desc = "Off";
                        } else {
                            icon = 'images/indicators/level.png';
                            desc = "On";
                            isActive = true;
                        }
                    }
                    break;
                case ParameterType.Sensor_Tamper:
                case ParameterType.Sensor_Alarm:
                    if (value == '0' || value == '') {
                        icon = 'images/indicators/alarm.png';
                        desc = "Normal";
                        //hideable = true;
                    } else {
                        icon = 'images/indicators/alarm.png';
                        desc = "Tampered";
                    }
                    break;
                case ParameterType.Status_Battery:
                    var level = parseInt(value);
                    desc = level + "%";
                    if (level <= 5)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_0.png';
                    else if (level > 5 && level <= 10)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_10.png';
                    else if (level > 10 && level <= 20)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_20.png';
                    else if (level > 20 && level <= 40)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_40.png';
                    else if (level > 40 && level <= 60)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_60.png';
                    else if (level > 60 && level <= 80)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_80.png';
                    else if (level > 80)
                        icon = 'pages/control/widgets/homegenie/generic/images/battery_level_100.png';
                    hideable = true;
                    break;
                case ParameterType.Meter_Watts:
                case ParameterType.Meter_KwHour:
                case ParameterType.Meter_KvaHour:
                case ParameterType.Meter_Pulses:
                case ParameterType.Meter_AcVoltage:
                case ParameterType.Meter_AcCurrent:
                case ParameterType.Sensor_Power:
                    desc = Module.getFormattedNumber(value);
                    icon = 'images/indicators/energy.png';
                    //if (value == '0' || value == '')
                    //    hideable = true;
                    break;
                case ParameterType.Sensor_Temperature:
                    if (HG.WebApp.Locales.GetTemperatureUnit() == 'Fahrenheit') {
                        var degrees = Module.getDoubleValue(value);
                        degrees = ((9.0 / 5.0) * degrees) + 32.0;
                        desc = Module.getFormattedNumber(degrees);
                    } else {
                        desc = Module.getFormattedNumber(value);
                    }
                    desc += "&deg;";
                    icon = 'images/indicators/temperature.png';
                    break;
                case ParameterType.Sensor_Luminance:
                    desc = Module.getFormattedNumber(value);
                    icon = 'images/indicators/luminance.png';
                    break;
                case ParameterType.Sensor_Humidity:
                    desc = Module.getFormattedNumber(value);
                    icon = 'images/indicators/humidity.png';
                    break;
                case ParameterType.Sensor_Flood:
                    desc = Module.getFormattedNumber(value);
                    icon = 'images/indicators/flood.png';
                    //if (value == '0' || value == '')
                    //    hideable = true;
                    break;
                case ParameterType.Sensor_CarbonMonoxide:
                case ParameterType.Sensor_CarbonDioxide:
                case ParameterType.Sensor_Smoke:
                case ParameterType.Sensor_Heat:
                    desc = Module.getFormattedNumber(value);
                    icon = 'images/indicators/smoke.png';
                    //if (value == '0' || value == '')
                    //    hideable = true;
                    break;
                case ParameterType.Sensor_Key:
                case ParameterType.Receiver_RawData:
                case ParameterType.Receiver_Status:
                    desc = value;
                    icon = 'images/indicators/generic.png';
                    //if (value == '')
                    //    hideable = true;
                    break;
                case ParameterType.Status_Error:
                    hideable = true;
                    desc = ($.isNumeric(value) ? Module.getFormattedNumber(value) : value);
                    icon = 'images/indicators/alarm.png';
                    break;
                	// New MySensor types
								case ParameterType.Sensor_Flow:
            				desc = Module.getFormattedNumber(value);
            				icon = 'images/indicators/flow.png';
            				break;
            		case ParameterType.Sensor_Volume:
            				desc = Module.getFormattedNumber(value);
            				icon = 'images/indicators/volume.png';
            				break;
            		case ParameterType.Sensor_Distance:
            				desc = Module.getFormattedNumber(value);
            				icon = 'images/indicators/distance.png';
            				break;
            		case ParameterType.Sensor_Infrared:
            			desc = BitConverter.ToString(value);
            			icon = 'images/indicators/infrared.png';
            			break;
            		case ParameterType.Sensor_UV:
            			desc = Module.getFormattedNumber(value);
            			icon = 'images/indicators/uv.png';
            			break;
                default:
                    desc = ($.isNumeric(value) ? Module.getFormattedNumber(value) : value);
                    icon = 'images/indicators/generic.png';
                    //if (value == '')
                    //    hideable = true;
                    unknown = true;
                    break;
            }
        } catch (e) {
            console.log(e);
        }
        var ctx = {
            displayName: (parameter.indexOf(".") > 0 && parameter.length > 1 && name == '') ? parameter.substring(parameter.indexOf(".")+1) : name,
            iconImage: icon,
            valueText: desc,
            isStatusParameter: isStatusParam,
            hasActiveStatus: isActive,
            canBeHidden: hideable,
            isUnknown: unknown
        };
        return ctx;
    };

};

HG.Ui.Popup = HG.Ui.Popup || {};

// TODO: is this still used??
eval('hg = {}; hg.ui =  HG.Ui;');

HG.Ui.CreatePage = function(model, cuid) {
    var $$ = model;
    $$._fieldCache = [];
    $$.PageId = $$.pageId = cuid;
    $$.getContainer = function() { 
        if (typeof $$.container == 'undefined')
            $$.container = $('#'+$$.pageId); 
        return $$.container; 
    };
    $$.field = function(field, globalSearch) {
        var f = globalSearch ? '@'+field : field;
        var el = null;
        if (typeof $$._fieldCache[f] == 'undefined') {
            el = globalSearch ? $(field) : $$.container.find('[data-ui-field='+field+']');
            if (el.length) 
                $$._fieldCache[f] = el;
        } else {
            el = $$._fieldCache[f];
        }
        return el; 
    };
    $$.clearCache = function() {
        var obj = $$._fieldCache;
        for (var prop in obj) {
            if (obj.hasOwnProperty(prop)) { delete obj[prop]; } 
        }
        $$._fieldCache = [];
    };
};
