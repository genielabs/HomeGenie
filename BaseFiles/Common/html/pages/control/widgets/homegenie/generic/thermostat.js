[{
    Name: "Thermostat Widget",
    Author: "Mike Tanana",
    Version: "2013-07-03",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/temperature.png',
    StatusText: '',
    Description: '',

    levelKnobBindValue: 'Heating',

    RenderView: function (cuid, module) {
        var displayUnit = 'Celsius';
        if (HG.WebApp.Locales.GetDateEndianType() == 'M') displayUnit = 'Fahrenheit';

        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        var _this = this;

        if (!controlpopup) {

            container.find('[data-ui-field=controlpopup]').trigger('create');
            controlpopup = container.find('[data-ui-field=controlpopup]').popup();
            widget.data('ControlPopUp', controlpopup);

            widget.find('[data-ui-field=options]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });

            controlpopup.find('[data-ui-field=level_knob]').knob({
                'release': function (v) {
                    v = Math.round(v * 10) / 10;
                    var setPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.' + _this.levelKnobBindValue);
                    if (setPoint != null) setPoint.Value = v;
                    HG.Control.Modules.ServiceCall('Thermostat.SetPointSet/' + _this.levelKnobBindValue, module.Domain, module.Address, v, function (data) { });
                },
                'change': function (v) {
                    v = Math.round(v);
                    controlpopup.find('[data-ui-field=status]').html(v + '&deg;');
                }
            });

            if (displayUnit == 'Celsius') {
                controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
                    min: 5,
                    max: 35,
                    step: 0.5
                });
            }
            else {
                controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
                    min: 40,
                    max: 100,
                    step: 1
                });
            }

            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                if (module.Domain == 'HomeAutomation.HomeGenie.Automation') {
                    HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
                    HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
                    HG.WebApp.ProgramsList.UpdateOptionsPopup();
                }
                else {
                    HG.WebApp.Control.EditModule(module);
                }
            });

            // popup values on open
            controlpopup.on('popupbeforeposition', function (evt, ui) {
                // reset buttons' state
                controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_cool]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_auto]').removeClass('ui-btn-active');
                if (_this.levelKnobBindValue == 'Heating')
                {
                    _this.EditHeatSetPoint(controlpopup, module);
                }
                else
                {
                    _this.EditCoolSetPoint(controlpopup, module);
                }
                // set current buttons' state from module properties 
                // thermostat mode
                var thermostatMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.Mode');
                if (thermostatMode != null) {
                    if (thermostatMode.Value == 'Cool') {
                        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                        controlpopup.find('[data-ui-field=mode_cool]').addClass('ui-btn-active');
                    }
                    else if (thermostatMode.Value == 'Heat') {
                        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                        controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
                    }
                    else if (thermostatMode.Value == 'Auto') {
                        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                        controlpopup.find('[data-ui-field=mode_auto]').addClass('ui-btn-active');
                    }
                }
                // fan mode
                var fanMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.FanMode');
                if (fanMode != null) {
                    if (fanMode.Value == 'On' || fanMode.Value == 'OnLow' || fanMode.Value == 'OnHigh') {
                        controlpopup.find('[data-ui-field=fanmode_on]').addClass('ui-btn-active');
                    }
                    else if (fanMode.Value == 'Auto' || fanMode.Value == 'AutoLow' || fanMode.Value == 'AutoHigh') {
                        controlpopup.find('[data-ui-field=fanmode_auto]').addClass('ui-btn-active');
                    }
                    else if (fanMode.Value == 'Circulate') {
                        controlpopup.find('[data-ui-field=fanmode_circulate]').addClass('ui-btn-active');
                    }
                }
            });
            // set point buttons events
            controlpopup.find('[data-ui-field=cool_setpoint]').on('click', function () {
                _this.EditCoolSetPoint(controlpopup, module);
            });
            controlpopup.find('[data-ui-field=heat_setpoint]').on('click', function () {
                _this.EditHeatSetPoint(controlpopup, module);
            });
            // thermostat mode buttons events
            controlpopup.find('[data-ui-field=mode_off]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_cool]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_auto]').removeClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Off", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_cool]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-bHG.WebApp.GroupModules.CurrentModule.Domaintn-active');
                controlpopup.find('[data-ui-field=mode_cool]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_auto]').removeClass('ui-btn-active');
                _this.EditCoolSetPoint(controlpopup, module);
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Cool", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_heat]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_cool]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_auto]').removeClass('ui-btn-active');
                _this.EditHeatSetPoint(controlpopup, module);
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Heat", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_auto]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_cool]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_auto]').addClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Auto", function (data) { });
            });
            // thermostate fan button events
            controlpopup.find('[data-ui-field=fanmode_on]').on('click', function () {
                controlpopup.find('[data-ui-field=fanmode_on]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_auto]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_circulate]').removeClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.FanModeSet", module.Domain, module.Address, "OnLow", function (data) { });
            });
            controlpopup.find('[data-ui-field=fanmode_auto]').on('click', function () {
                controlpopup.find('[data-ui-field=fanmode_on]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_auto]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_circulate]').removeClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.FanModeSet", module.Domain, module.Address, "AutoLow", function (data) { });
            });
            controlpopup.find('[data-ui-field=fanmode_circulate]').on('click', function () {
                controlpopup.find('[data-ui-field=fanmode_on]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_auto]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=fanmode_circulate]').addClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.FanModeSet", module.Domain, module.Address, "Circulate", function (data) { });
            });

        }
        
        this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;

        widget.find('[data-ui-field=name]').html(module.Name);
        widget.find('[data-ui-field=description]').html(this.Description);
        controlpopup.find('[data-ui-field=group]').html(this.GroupName);
        controlpopup.find('[data-ui-field=name]').html(module.Name);

        var imagesrc = 'pages/control/widgets/homegenie/generic/images/temperature.png';

        // display Temperature
        var temperatureField = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Temperature");
        var temperature = 0;
        if (temperatureField != null) {
            temperature = Math.round(temperatureField.Value.replace(',', '.') * 100) / 100;
            if (displayUnit == 'Fahrenheit') temperature = (temperature * 1.8) + 32;
        }
        widget.find('[data-ui-field=temperature_value]').html(temperature.toFixed(1) + '&deg;');

        // display Fan State
        var fanState = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.FanState");
        if (fanState == null || fanState.Value == '') {
            widget.find('[data-ui-field=fan_field]').hide();
        }
        else {
            widget.find('[data-ui-field=fan_field]').show();
            var displayFan = '---';
            switch (fanState.Value) {
                case 1:
                    displayFan = 'On';
                    break;
                default:
                    displayFan = 'Off';
                    break;
            }
            widget.find('[data-ui-field=fan_value]').html(displayFan);
        }


        // display current Heating SetPoint
        var heatTo = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.SetPoint.Heating");
        if (heatTo == null || heatTo.Value == '') {
            widget.find('[data-ui-field=heat_field]').hide();
        }
        else {
            widget.find('[data-ui-field=heat_field]').show();
            var temperature = Math.round(heatTo.Value.replace(',', '.') * 100) / 100;
            widget.find('[data-ui-field=heatset_value]').html(temperature.toFixed(1) + '&deg;');
        }


        // display current Cooling SetPoint
        widget.find('[data-ui-field=cool_field]').hide();
        var coolTo = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.SetPoint.Cooling");
        if (coolTo == null || coolTo.Value == '') {
            widget.find('[data-ui-field=cool_field]').hide();
        }
        else {
            widget.find('[data-ui-field=cool_field]').show();
            var temperature = Math.round(coolTo.Value.replace(',', '.') * 100) / 100;
            widget.find('[data-ui-field=coolset_value]').html(temperature.toFixed(1) + '&deg;');
        }
        // enable/disable Cool Set Point feature (not every thermostat support it)
        if (coolTo == null) {
            controlpopup.find('[data-ui-field=mode_cool]').addClass('ui-disabled');
            controlpopup.find('[data-ui-field=cool_setpoint]').addClass('ui-disabled');
        }
        else {
            controlpopup.find('[data-ui-field=mode_cool]').removeClass('ui-disabled');
            controlpopup.find('[data-ui-field=cool_setpoint]').removeClass('ui-disabled');
        }


        // display status line (operating state + mode)
        var displayState = '---';
        var operatingState = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.OperatingState");
        var operatingFanMode = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.FanMode");
        if (operatingState != null) displayState = operatingState.Value;
        if (operatingFanMode != null) displayState = operatingFanMode.Value;
        widget.find('[data-ui-field=operating_value]').html(displayState);
        //
        var displayMode = '---';
        var operatingMode = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.Mode");
        if (operatingMode != null) displayMode = operatingMode.Value;
        widget.find('[data-ui-field=mode_value]').html(displayMode);

    },

    EditCoolSetPoint: function (controlpopup, module) {
        var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
        controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=cool_setpoint]').addClass('ui-btn-active');
        // show current cool setpoint
        var coolSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Cooling');
        if (coolSetPoint != null) {
            levelKnob.val(coolSetPoint.Value).trigger('change');
            controlpopup.find('[data-ui-field=status]').html(coolSetPoint.Value + '&deg;');
        }
        this.levelKnobBindValue = 'Cooling';
    },

    EditHeatSetPoint: function (controlpopup, module) {
        var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
        controlpopup.find('[data-ui-field=cool_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=heat_setpoint]').addClass('ui-btn-active');
        // show current heat setpoint
        var heatSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Heating');
        if (heatSetPoint != null) {
            levelKnob.val(heatSetPoint.Value).trigger('change');
            controlpopup.find('[data-ui-field=status]').html(heatSetPoint.Value + '&deg;');
        }
        this.levelKnobBindValue = 'Heating';
    }

}]
