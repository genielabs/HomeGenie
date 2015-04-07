﻿[{
    Name: "Security Alarm System",
    Author: "Generoso Martello",
    Version: "2013-08-21",

    GroupName: '',
    IconImage : 'pages/control/widgets/homegenie/generic/images/door_closed.png',
    StatusText: '',
    Description: '',
    UpdateTime: '',

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        
        //
        if (!this.Initialized) {
            this.Initialized = true;
            widget.find('[data-ui-field=lockunlock]').slider().slider('refresh');
            widget.find('[data-ui-field=lockunlock]').on('change', function () {
                HG.Control.Modules.ServiceCall('DoorLock.Set', module.Domain, module.Address, ($(this).val() == 'unlocked' ? '0' : '255'), function (data) { });
            });
        }
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        // get module watts prop
        //


        var lockstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.DoorLock"); //HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityArmed");
	if(lockstatus != null){
            var l_updateTime = lockstatus.UpdateTime;
            if (typeof l_updateTime != 'undefined') {
                l_updateTime = l_updateTime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(l_updateTime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
	}
        if (lockstatus != null && lockstatus.Value.indexOf("Locked") == 0) {
            widget.find('[data-ui-field=lockunlock]').val("locked").slider('refresh');
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/door_closed.png';
        }
        else {
            widget.find('[data-ui-field=lockunlock]').val("unlocked").slider('refresh');
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/door_open.png';
        }
        
/*
        var alarmstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityTriggered");
        if (alarmstatus != null && alarmstatus.Value == "1") {
            this.StatusText = "ALARM!";
        }
        else {
            this.StatusText = "OK";
        }

        var armedlevel = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
        var armedstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityArmed");
        if (armedlevel != null && armedlevel.Value == "1") {
            if (armedstatus != null && armedstatus.Value == "1") {
                this.Description = "ARMED";
            }
            else {
                this.Description = "ARMING...";
            }
        }
        else {
            this.Description = "DISARMED";
        }
*/
        
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
        widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle">' + this.StatusText + '</span>');
        widget.find('[data-ui-field=updatetime]').html(this.UpdateTime);
        widget.find('[data-ui-field=description]').html(this.Description);
    }

}]