[{
    Name: "Generic Sensor",
    Author: "Generoso Martello",
    Version: "2013-10-04",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/sensor.png',
    StatusText: '',
    Description: '',
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

        widget.find('[data-ui-field=sensoronoff]').on('click', function () {
           var prop = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Alarm");
           if(prop.Value == '255')
           {
                 HG.WebApp.Utility.SetModulePropertyByName(module, "Sensor.Alarm", "0");
              prop = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Alarm");
              prop.NeedsUpdate = 'true';
              HG.WebApp.GroupModules.UpdateModule(module, function () {
                  HG.WebApp.GroupModules.ModuleUpdatedCallback();
              });
                 var sensorstatus = widget.find('[data-ui-field=sensoronoff]').html();
              sensorstatus = sensorstatus.replace('<span class="hg-indicator-alarm" style="padding-left:0;width:20px;">&nbsp;</span>','') ;
              widget.find('[data-ui-field=sensoronoff]').html(sensorstatus);
           }
        });

        widget.find('[data-ui-field=name]').html(module.Name);
        //
        var sensoricon = '';
        var sensorimgdata = '';
        var sensortxtdata = '';
        var infotext = '';
        var lastupdatetime = 0;
        if(module.Properties != null) {
           for (p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name.indexOf('Sensor.') == 0 || module.Properties[p].Name == 'Meter.Watts' || module.Properties[p].Name == 'Status.Level' || module.Properties[p].Name == 'Status.Battery') {
                    var value = Math.round(module.Properties[p].Value.replace(',', '.') * 10) / 10;
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
                        if (lastupdatetime < d) {
                            lastupdatetime = d;
                        }
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
                        infotext += '<span style="vertical-align:middle">' + value + '%</span> <img style="vertical-align:middle" src="pages/control/widgets/homegenie/generic/images/battery_level_' + blevel + '.png" />';
                        continue;
                    }
                    else if (module.Properties[p].Name == 'Sensor.Temperature') {
                        imagesrc = 'hg-indicator-temperature';
                        var temp = module.Properties[p].Value.replace(',', '.');
                        displayvalue = HG.WebApp.Utility.FormatTemperature(temp);
                    }
                    else if (module.Properties[p].Name == 'Sensor.Luminance') {
                        imagesrc = 'hg-indicator-luminance';
                        displayvalue = value + '&nbsp;lx';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Humidity') {
                        imagesrc = 'hg-indicator-humidity';
                        displayvalue = value + '%';
                    }
                    if (module.Properties[p].Name == 'Meter.Watts') {
                        imagesrc = 'hg-indicator-energy';
                    }
                    else if (module.Properties[p].Name == 'Sensor.DoorWindow') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-door';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Alarm') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-alarm';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Smoke') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-smoke';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Flood') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-flood';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Heat') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-heat';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Sensor.Generic') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-generic';
                           displayvalue = '';
                    }
                    else if (module.Properties[p].Name == 'Status.Level') {
                        if( value != '0' )
                           imagesrc = 'hg-indicator-level';
                        else
                              displayvalue = '';
                    }
                    if (imagesrc != '') {
                        displayname = '<span class="' + imagesrc + '" style="padding-left:0;width:20px;">&nbsp;</span>';
                        if( displayvalue !== '')
                        {
                          var template = container.find('[data-ui-field=iconvaluetemplate]').html();
                           template = template.replace('%icon%', displayname);
                          template = template.replace('%value%', displayvalue);
                          sensorimgdata += template;
                        }
                        else
                        {
                          var template = container.find('[data-ui-field=icontemplate]').html();
                          template = template.replace('%icon%', displayname);
                          sensoricon += template;
                        }
                    }
                    else {
                        if( displayvalue !== '')
                        {
                          var template = container.find('[data-ui-field=valuetemplate]').html();
                          template = template.replace('%label%', displayname);
                          template = template.replace('%value%', displayvalue);
                          sensortxtdata += template;
                        }
                    }
                }
            }
        }
        //
        var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
        if (widgeticon != null && widgeticon.Value != '') {
            this.IconImage = widgeticon.Value;
        }
        //
        widget.find('[data-ui-field=description]').html((module.Domain.substring(module.Domain.lastIndexOf('.') + 1))+' '+module.Address);
        //
        if (sensoricon != '') sensoricon = sensoricon + '<br clear="all" />';
        if (sensorimgdata != '') sensorimgdata = sensorimgdata + '<br clear="all" />';
        if (sensortxtdata != '') sensortxtdata = sensortxtdata + '<br clear="all" />';
        if (lastupdatetime > 0) {
            this.UpdateTime = HG.WebApp.Utility.FormatDate(lastupdatetime) + ' ' + HG.WebApp.Utility.FormatDateTime(lastupdatetime);
        widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle;font-size:13px">' + this.UpdateTime + '</span>&nbsp;&nbsp;' + infotext);
        widget.find('[data-ui-field=sensoronoff]').html(sensoricon);
        widget.find('[data-ui-field=sensorstatus]').html(sensorimgdata + sensortxtdata);
        widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
      }
   }

}]