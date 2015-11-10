[{
    Name: "Z-Wave.Me Floor Thermostat Widget",
    Author: "Alexander Sidorenko (based on Mike Tanana's widget)",
    Version: "2014-12-07",

    GroupName: "",
    IconImage: "pages/control/widgets/homegenie/generic/images/temperature.png",
    StatusText: "",
    Description: "",

    levelKnobBindValue: "Heating",

    RenderView: function (cuid, module) {

        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        if (!controlpopup) {
            var _this = this;

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
                    v = Math.round(v);
                    var setPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.' + _this.levelKnobBindValue);
                    if (setPoint != null) setPoint.Value = v;
                    HG.Control.Modules.ServiceCall('Thermostat.SetPointSet/' + _this.levelKnobBindValue, module.Domain, module.Address, v, function (data) { });
                },
                'change': function (v) {
                    v = Math.round(v);
                    controlpopup.find('[data-ui-field=status]').html(v + '&deg;');
                }
            });

            if (HG.WebApp.Locales.GetTemperatureUnit() == 'Celsius') {
                controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
                    min: 5,
                    max: 40
                });
            }
            else {
                controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
                    min: 40,
                    max: 100
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
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heateconomy]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_furnace]').removeClass('ui-btn-active');
                // set current buttons' state from module properties 
                var thermostatMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.Mode');
                if (thermostatMode != null) {
                    if (thermostatMode.Value == 'Off') {
                        controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
                        _this.EditHeatSetPoint(controlpopup, module);
                    }
                    if (thermostatMode.Value == 'HeatEconomy') {
                        controlpopup.find('[data-ui-field=mode_heateconomy]').addClass('ui-btn-active');
                        _this.EditHeatEconomySetPoint(controlpopup, module);
                    }
                    else if (thermostatMode.Value == 'Heat') {
                        controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
                        _this.EditHeatSetPoint(controlpopup, module);
                    }
                    else if (thermostatMode.Value == 'Furnace') {
                        controlpopup.find('[data-ui-field=mode_furnace]').addClass('ui-btn-active');
                        _this.EditFurnaceSetPoint(controlpopup, module);
                    }
                }
                else {
                    controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
                }
                //
            });

            // set point buttons events
            controlpopup.find('[data-ui-field=heateconomy_setpoint]').on('click', function () {
                _this.EditHeatEconomySetPoint(controlpopup, module);
            });
            controlpopup.find('[data-ui-field=heat_setpoint]').on('click', function () {
                _this.EditHeatSetPoint(controlpopup, module);
            });
            controlpopup.find('[data-ui-field=furnace_setpoint]').on('click', function () {
                _this.EditFurnaceSetPoint(controlpopup, module);
            });

            // thermostat mode buttons events
            controlpopup.find('[data-ui-field=mode_off]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heateconomy]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_furnace]').removeClass('ui-btn-active');
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Off", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_heateconomy]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heateconomy]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_furnace]').removeClass('ui-btn-active');
                _this.EditHeatEconomySetPoint(controlpopup, module);
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "HeatEconomy", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_heat]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heateconomy]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_furnace]').removeClass('ui-btn-active');
                _this.EditHeatSetPoint(controlpopup, module);
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Heat", function (data) { });
            });
            controlpopup.find('[data-ui-field=mode_furnace]').on('click', function () {
                controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heateconomy]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
                controlpopup.find('[data-ui-field=mode_furnace]').addClass('ui-btn-active');
                _this.EditFurnaceSetPoint(controlpopup, module);
                HG.Control.Modules.ServiceCall("Thermostat.ModeSet", module.Domain, module.Address, "Furnace", function (data) { });
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
            temperature = temperatureField.Value.replace(',', '.');
            widget.find('[data-ui-field=temperature_value]').html(HG.WebApp.Utility.FormatTemperature(temperature));
        }


        // display current Heating SetPoint
        var heatTo = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.SetPoint.Heating");
        if (heatTo == null || heatTo.Value == '') {
            widget.find('[data-ui-field=heat_field]').hide();
        }
        else {
            widget.find('[data-ui-field=heat_field]').show();
            var temperature = heatTo.Value.toString().replace(',', '.');
            widget.find('[data-ui-field=heat_field] > [data-ui-field=set_value]').html(HG.WebApp.Utility.FormatTemperature(temperature));
        }


        // display current HeatingEconomy SetPoint
        widget.find('[data-ui-field=heateconomy_field]').hide();
        var heatEconomyTo = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.SetPoint.HeatingEconomy");
        if (heatEconomyTo == null || heatEconomyTo.Value == '') {
            widget.find('[data-ui-field=heateconomy_field]').hide();
        }
        else {
            widget.find('[data-ui-field=heateconomy_field]').show();
            var temperature = heatEconomyTo.Value.toString().replace(',', '.');
            widget.find('[data-ui-field=heateconomy_field] > [data-ui-field=set_value]').html(HG.WebApp.Utility.FormatTemperature(temperature));
        }

        // display current Furnace SetPoint
        widget.find('[data-ui-field=furnace_field]').hide();
        var furnaceTo = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.SetPoint.Furnace");
        if (furnaceTo == null || furnaceTo.Value == '') {
            widget.find('[data-ui-field=furnace_field]').hide();
        }
        else {
            widget.find('[data-ui-field=furnace_field]').show();
            var temperature = furnaceTo.Value.toString().replace(',', '.');
            widget.find('[data-ui-field=furnace_field] > [data-ui-field=set_value]').html(HG.WebApp.Utility.FormatTemperature(temperature));
        }


        // display status line (operating state + mode)
        var displayState = '---';
        var operatingState = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.OperatingState");
        if (operatingState != null) displayState = operatingState.Value;
        widget.find('[data-ui-field=operating_value]').html(displayState);
        //
        var displayMode = '---';
        var operatingMode = HG.WebApp.Utility.GetModulePropertyByName(module, "Thermostat.Mode");
        if (operatingMode != null) {
            displayMode = operatingMode.Value;
            var localizedDisplayMode = HG.WebApp.Locales.GetWidgetLocaleString(widget, displayMode);
            if (localizedDisplayMode != undefined)
                widget.find('[data-ui-field=mode_value]').html(localizedDisplayMode);
            else
                widget.find('[data-ui-field=mode_value]').html(displayMode);
            switch (displayMode) {
                case "Off":
                    widget.find('[data-ui-field=heat_field]').hide();
                    widget.find('[data-ui-field=heateconomy_field]').hide();
                    widget.find('[data-ui-field=furnace_field]').hide();
                    break;
                case "HeatEconomy":
                    widget.find('[data-ui-field=heat_field]').hide();
                    widget.find('[data-ui-field=heateconomy_field]').show();
                    widget.find('[data-ui-field=furnace_field]').hide();
                    break;
                case "Heat":
                    widget.find('[data-ui-field=heat_field]').show();
                    widget.find('[data-ui-field=heateconomy_field]').hide();
                    widget.find('[data-ui-field=furnace_field]').hide();
                    break;
                case "Furnace":
                    widget.find('[data-ui-field=heat_field]').hide();
                    widget.find('[data-ui-field=heateconomy_field]').hide();
                    widget.find('[data-ui-field=furnace_field]').show();
                    break;
            }
        }
    },

    EditHeatEconomySetPoint: function (controlpopup, module) {
        var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
        controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=furnace_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=heateconomy_setpoint]').addClass('ui-btn-active');
        // show current heatEconomy setpoint
        var heatEconomySetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.HeatingEconomy');
        if (heatEconomySetPoint != null) {
            levelKnob.val(heatEconomySetPoint.Value).trigger('change');
            controlpopup.find('[data-ui-field=status]').html(heatEconomySetPoint.Value + '&deg;');
        }
        this.levelKnobBindValue = 'HeatingEconomy';
    },

    EditHeatSetPoint: function (controlpopup, module) {
        var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
        controlpopup.find('[data-ui-field=heateconomy_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=furnace_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=heat_setpoint]').addClass('ui-btn-active');
        // show current heat setpoint
        var heatSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Heating');
        if (heatSetPoint != null) {
            levelKnob.val(heatSetPoint.Value).trigger('change');
            controlpopup.find('[data-ui-field=status]').html(heatSetPoint.Value + '&deg;');
        }
        this.levelKnobBindValue = 'Heating';
    },

    EditFurnaceSetPoint: function (controlpopup, module) {
        var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
        controlpopup.find('[data-ui-field=heateconomy_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
        controlpopup.find('[data-ui-field=furnace_setpoint]').addClass('ui-btn-active');
        // show current furnace setpoint
        var furnaceSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Furnace');
        if (furnaceSetPoint != null) {
            levelKnob.val(furnaceSetPoint.Value).trigger('change');
            controlpopup.find('[data-ui-field=status]').html(furnaceSetPoint.Value + '&deg;');
        }
        this.levelKnobBindValue = 'Furnace';
    }

}]