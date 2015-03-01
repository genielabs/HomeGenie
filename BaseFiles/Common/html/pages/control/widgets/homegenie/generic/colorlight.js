﻿[{
    Name: "Generic Color Light",
    Author: "Generoso Martello",
    Version: "2013-03-31",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/icons/colorbulbs.png',
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
        var deviceaddress = module.Address; //HG.WebApp.Utility.GetModulePropertyByName(module, "Philips.HueLight");
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
            // when options button is clicked control popup is shown
            widget.find('[data-ui-field=options]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            widget.find('[data-ui-field=color]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            //
            // ui events handlers
            //
            widget.find('[data-ui-field=on]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            widget.find('[data-ui-field=off]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            //
            controlpopup.find('[data-ui-field=on]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=off]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            //
            this.ColorWheel = Raphael.colorwheel(controlpopup.find('[data-ui-field=colors]'), 200, 80);
            this.ColorWheel.ondrag(null, function (rgbcolor) {
                var color = Raphael.color(rgbcolor);
                //
                iconp1.clear(); iconp1.ball(22, 22, 22, color);
                iconp2.clear(); iconp2.ball(22, 22, 22, color);
                //
                var hue = color.h;
                var sat = color.s;
                var bri = color.v;
                var hsbcolor = hue + "," + sat + "," + bri;
                //
                HG.Control.Modules.ServiceCall("Control.ColorHsb", module.Domain, module.Address, hsbcolor, function (data) { });
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
                watts = w + 'W';
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
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            //
            level = level.Value.replace(',', '.') * 100;
            if (level >= 99 || level == 0) {
                if (level >= 99) {
                    level = 'ON';
                    this.StatusText = '<img width="16" height="16" src="images/common/led_green.png" style="vertical-align:middle" />';
                }
                else {
                    level = 'OFF';
                    this.StatusText = '<img width="16" height="16" src="images/common/led_black.png" style="vertical-align:middle" />';
                }
            }
            else {
                level = level.toFixed(0) + '%';
                this.StatusText = '<img width="16" height="16" src="images/common/led_green.png" style="vertical-align:middle" />';
            }

        } else level = '';
        //
        this.StatusText = '<span style="vertical-align:middle">' + watts + '&nbsp;&nbsp;&nbsp;' + level + '</span>&nbsp;' + this.StatusText;
        //
        var hsbcolor = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.ColorHsb");
        if (hsbcolor != null && this.ColorWheel != null) {
            hsbcolor = 'hsb(' + hsbcolor.Value + ')';
            var color = Raphael.color(hsbcolor);
            if (level == 'OFF') color.v = 0.05;
            this.ColorWheel.color(hsbcolor);
            this.WidgetImage.clear(); this.WidgetImage.ball(22, 22, 22, color);
            this.ControlImage.clear(); this.ControlImage.ball(22, 22, 22, color);
        }
        //
        this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
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
        controlpopup.find('[data-ui-field=status]').html(level + '<br />' + watts);
    }

}]