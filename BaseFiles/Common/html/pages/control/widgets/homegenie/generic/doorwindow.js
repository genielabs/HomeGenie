[{
    Name: "Door/Window Sensor",
    Author: "Generoso Martello",
    Version: "2013-03-31",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/door_closed.png',
    StatusText: '',
    Description: '',
    UpdateTime: '',
    Initialized: false,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        //
        if (!this.Initialized) {
            this.Initialized = true;
            //
            // ui events handlers
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
        }
        //
        var tamper = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Tamper");
        var dwstate = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.DoorWindow");
        var alarm = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Alarm");
        var generic = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Generic");
        var level = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
        //
        if (tamper != null && (alarm == null || tamper.UpdateTime >= alarm.UpdateTime)) {
            tamper = tamper.Value.replace(',', '.') * 1;
        }
        else if (alarm != null) {
            tamper = alarm.Value.replace(',', '.') * 1;
        }
        else {
            tamper = 0;
        }
        //
        // sensor property fallback
        var doorstate = '';
        if (dwstate != null && (generic == null || dwstate.UpdateTime >= generic.UpdateTime) && (level == null || dwstate.UpdateTime >= level.UpdateTime)) {
            var updatetime = dwstate.UpdateTime;
            if (typeof updatetime != 'undefined') {
                updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(updatetime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            doorstate = dwstate.Value;
        }
        else if (generic != null && (level == null || generic.UpdateTime >= level.UpdateTime)) {
            var updatetime = generic.UpdateTime;
            if (typeof updatetime != 'undefined') {
                updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(updatetime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            doorstate = generic.Value;
        }
        else if (level != null) {
            var updatetime = level.UpdateTime;
            if (typeof updatetime != 'undefined') {
                updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(updatetime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            doorstate = level.Value.replace(',', '.') * 100;
        }
        //
        if (doorstate === '') {
            this.StatusText = '&nbsp;&nbsp;&nbsp;<span style="vertical-align:middle">?</span>';
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/door_closed.png';
        }
        else if (doorstate != '0') {
            this.StatusText = '&nbsp;&nbsp;&nbsp;<span style="vertical-align:middle">OPEN</span> ';
            this.StatusText += '<img width="15" height="15" src="images/common/led_green.png" style="vertical-align:middle" />';
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/door_open.png';
        }
        else {
            this.StatusText = '&nbsp;&nbsp;&nbsp;<span style="vertical-align:middle">CLOSED</span> ';
            this.StatusText += '<img width="15" height="15" src="images/common/led_black.png" style="vertical-align:middle" />';
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/door_closed.png';
        }
        //
        if (tamper > 0) {
            this.StatusText = '&nbsp;&nbsp;&nbsp;<span style="color:red;vertical-align:middle">TAMPER</span>' + this.StatusText;
        }
        //
        var sensorimgdata = '';
        var sensortxtdata = '';
        if (module.Properties != null) {
            for (p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name.indexOf('Sensor.') == 0 || module.Properties[p].Name == 'Meter.Watts' || module.Properties[p].Name == 'Status.Level' || module.Properties[p].Name == 'Status.Battery') {
                    var value = Math.round(module.Properties[p].Value.replace(',', '.') * 100) / 100;
                    if (isNaN(value)) value = module.Properties[p].Value;
                    //
                    var displayname = module.Properties[p].Name.replace('Sensor.', '');
                    displayname = displayname.replace('Meter.', '');
                    displayname = displayname.replace('Status.', '');
                    displayname = '<b>' + displayname + '</b>';
                    //
                    var displayvalue = value;
                    //
                    var updatetime = module.Properties[p].UpdateTime;
                    if (typeof(updatetime) != 'undefined')
                    {
                        updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                        var d = new Date(updatetime);
                        updatetime = HG.WebApp.Utility.GetElapsedTimeText(d);
                    }
                    //
                    var imagesrc = '';
                    //
                    if (module.Properties[p].Name == 'Status.Battery') {
                        var blevel = 0;
                        blevel = parseFloat(module.Properties[p].Value);
                        if (blevel == 255) blevel = 0;
                        else if (blevel > 80 && blevel <= 100) blevel = 100;
                        else if (blevel > 60) blevel = 80;
                        else if (blevel > 40) blevel = 60;
                        else if (blevel > 20) blevel = 40;
                        else if (blevel > 10) blevel = 20;
                        else if (blevel > 0) blevel = 10;
                        //
                        this.StatusText = '<span style="vertical-align:middle">' + value + '%</span> <img style="vertical-align:middle" src="pages/control/widgets/homegenie/generic/images/battery_level_' + blevel + '.png" /> ' + this.StatusText;
                        continue;
                    }
                    else if (module.Properties[p].Name == "Sensor.Temperature") {
                        imagesrc = 'hg-indicator-temperature';
                        var temp = Math.round(module.Properties[p].Value.replace(',', '.') * 10) / 10;
                        displayvalue = temp + '&#8451;';
                    }
                    else if (module.Properties[p].Name == "Sensor.TemperatureF") {
                        imagesrc = 'hg-indicator-temperature';
                        var temp = Math.round(module.Properties[p].Value.replace(',', '.') * 10) / 10;
                        displayvalue = temp + '&#8457;';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Luminance') {
                        imagesrc = 'hg-indicator-luminance';
                        displayvalue = value + '%';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Humidity') {
                        imagesrc = 'hg-indicator-humidity';
                        displayvalue = value + '%';
                    }
                    //
                    if (module.Properties[p].Name == 'Meter.Watts') {
                        imagesrc = 'hg-indicator-energy';
                    }
                    else if (module.Properties[p].Name == 'Sensor.DoorWindow') {
                        imagesrc = 'hg-indicator-door';
                    }
                    //
                    if (imagesrc != '') {
                        displayname = '<span class="' + imagesrc + '" style="padding-left:0;width:20px;">&nbsp;</span>';
                        sensorimgdata += '<div style="margin-left:10px;height:28px;float:left"><div align="right" style="padding-right:4px;width:60px;float:left;text-align:bottom;line-height:28px">' + displayname + '</div><div align="left" style="padding-left:4px;float:left;font-size:18pt">' + displayvalue + '</div></div>';
                    }
                    else {
                        sensortxtdata += '<div style="margin-left:10px;height:28px;float:left"><div align="right" style="padding-right:4px;width:60px;float:left;font-size:11pt;font-weight:bold;text-align:bottom;line-height:28px;overflow:hidden;text-overflow:ellipsis;">' + displayname + '</div><div align="left" style="padding-left:4px;float:left;text-align:bottom;line-height:28px;font-size:18pt">' + displayvalue + '</div></div>';
                    }
                }
            }

        }
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        //
        widget.find('[data-ui-field=description]').html((module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address);
        //
        widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle">' + this.StatusText + '</span>');
        //
        if (sensorimgdata != '') sensorimgdata = sensorimgdata + '<br clear="all" />';
        if (sensortxtdata != '') sensortxtdata = sensortxtdata + '<br clear="all" />';
        widget.find('[data-ui-field=sensorstatus]').html(sensorimgdata + sensortxtdata);
        //
        widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
        widget.find('[data-ui-field=updatetime]').html(this.UpdateTime);
    }
}]