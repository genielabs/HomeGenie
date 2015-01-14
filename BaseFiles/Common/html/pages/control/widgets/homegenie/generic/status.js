[{
    Name: "Status Widget",
    Author: "Generoso Martello",
    Version: "2013-10-13",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/status.png',
    StatusText: '',
    Description: '',

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        //
        var statusmarkup = '';
        if (module.Properties != null) {
            for (p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name.indexOf('StatusWidget.') == 0) {
                    var value = Math.round(module.Properties[p].Value.replace(',', '.') * 100) / 100;
                    if (isNaN(value)) value = module.Properties[p].Value;
                    //
                    var displayname = module.Properties[p].Name.replace('StatusWidget.', '');
                    displayname = '<b>' + displayname + '</b>';
                    //
                    var displayvalue = value;
                    //
                    var updatetime = module.Properties[p].UpdateTime;
                    updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                    var d = new Date(updatetime);
                    updatetime = HG.WebApp.Utility.GetElapsedTimeText(d);
                    //
                    statusmarkup += '<div style="margin-left:10px;height:28px;float:left"><div align="right" style="padding-right:4px;width:60px;float:left;font-size:11pt;font-weight:bold;text-align:bottom;line-height:28px;overflow:hidden;text-overflow:ellipsis;">' + displayname + '</div><div align="left" style="padding-left:4px;float:left;text-align:bottom;line-height:28px;font-size:18pt">' + displayvalue + '</div></div>';
                }
            }
        }
        //
        var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
        if (widgeticon != null && widgeticon.Value != '') {
            this.IconImage = widgeticon.Value;
        }
        //
        widget.find('[data-ui-field=description]').html((module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address);
        //
        if (statusmarkup != '') statusmarkup = statusmarkup + '<br clear="all" />';
        widget.find('[data-ui-field=sensorstatus]').html(statusmarkup);
        widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
    }

}]