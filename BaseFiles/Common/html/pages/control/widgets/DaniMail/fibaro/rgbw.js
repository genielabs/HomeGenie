﻿[{
    Name: "Fibaro RGBW",
    Author: "DaniMail",
    Version: "01  2014-02-05",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/light_off.png',
    StatusText: '',
    Description: '',
    UpdateTime: '',
    HueLightNumber: '',
    ColorWheel: null,
    WidgetImage: null,
    ControlImage: null,

    RenderView: function (cuid, module) {
        if (cuid == null) return;
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        //
        var deviceaddress = module.Address;
        //
        // create and store a local reference to control popup object
        //
        if (!controlpopup) {
            container.find('[data-ui-field=controlpopup]').trigger('create');
            controlpopup = container.find('[data-ui-field=controlpopup]').popup();
            widget.data('ControlPopUp', controlpopup);
            //
            var iconp1 = this.WidgetImage = Raphael(widget.find('[data-ui-field=color]').get(0));
            var iconp2 = this.ControlImage = Raphael(controlpopup.find('[data-ui-field=color]').get(0));
            //
            // initialization stuff here
            //
            // when widget is clicked control popup is shown
            widget.on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            //
            // ui events handlers
            //
            controlpopup.find('[data-ui-field=on]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", "HomeAutomation.FibaroRGBW", module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=off]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", "HomeAutomation.FibaroRGBW", module.Address, null, function (data) { });
            });
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            //
            controlpopup.find('[data-ui-field=prg1]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "1", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg2]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "2", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg3]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "3", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg4]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "4", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg5]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "5", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg6]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "6", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg7]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "7", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg8]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "8", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg9]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "9", function (data) { });
            });
            controlpopup.find('[data-ui-field=prg10]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.ProgramRGB", "HomeAutomation.FibaroRGBW", module.Address, "10", function (data) { });
            });
            //
            this.ColorWheel = Raphael.colorwheel(controlpopup.find('[data-ui-field=colors]'), 200, 80);
            this.ColorWheel.ondrag(null, function (rgbcolor) {
                var color = Raphael.color(rgbcolor);
                //
                iconp1.clear(); iconp1.ball(20, 20, 20, color);
                iconp2.clear(); iconp2.ball(20, 20, 20, color);
                //
                var red = color.r;
                var green = color.g;
                var blue = color.b;
                var srgbcolor = red + "," + green + "," + blue;
                //
                HG.Control.Modules.ServiceCall("Control.ColorHsb", "HomeAutomation.FibaroRGBW", module.Address, srgbcolor, function (data) { });
            });
        }
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        // get module watts prop
        //
        var watts = HG.WebApp.Utility.GetModulePropertyByName(module, "Meter.Watts");
        if (watts != null) {
            var w = Math.round(watts.Value.replace(',', '.'));
            if (w > 0) {
                watts = '&nbsp;&nbsp;&nbsp;' + w + 'W';
            } else watts = '';
        } else watts = '';
        //
        // get module level prop for status text
        //
        var level = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
        if (level != null) {
            var updatetime = level.UpdateTime;
            if (typeof updatetime != 'undefined') {
                updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(updatetime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + '<br>' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            //
            level = level.Value.replace(',', '.') * 100;
            if (level >= 99 || level == 0) {
                if (level >= 99) {
                    level = 'ON';
                }
                else {
                    level = 'OFF';
                }
            }
            else {
                level = level.toFixed(0) + '%';
            }

        } else level = '';
        //
        this.StatusText = level + watts;
        //
        var srgbcolor = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.ColorHsb");
        if (srgbcolor != null && this.ColorWheel != null) {
            srgbcolor = 'rgb(' + srgbcolor.Value + ')';
            var color = Raphael.color(srgbcolor);
            if (level == 'OFF') color.v = 0.05;
            this.ColorWheel.color(srgbcolor);
            this.WidgetImage.clear(); this.WidgetImage.ball(20, 20, 20, color);
            this.ControlImage.clear(); this.ControlImage.ball(20, 20, 20, color);
        }
        //
        this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' <strong>' + module.Address + '</strong>';
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        widget.find('[data-ui-field=description]').html(this.Description);
        widget.find('[data-ui-field=status]').html(this.StatusText);
        widget.find('[data-ui-field=updatetime]').html(this.UpdateTime);
        //
        // render control popup
        //
        controlpopup.find('[data-ui-field=group]').html(this.GroupName);
        controlpopup.find('[data-ui-field=name]').html(module.Name);
        controlpopup.find('[data-ui-field=status]').html(this.StatusText);
    }

}]