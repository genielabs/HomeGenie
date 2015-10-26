[{
  Name: "Security Alarm System",
  Author: "Generoso Martello",
  Version: "2013-08-21",

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/generic/images/securitysystem.png',
  StatusText: '',
  Description: '',
  UpdateTime: '',
  Widget: null,

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = this.Widget = container.find('[data-ui-field=widget]');
    //
    if (!this.Initialized) {
      this.Initialized = true;
      widget.find('[data-ui-field=btn_armhome]').on('click', function () {
        HG.Control.Modules.ServiceCall('Control.ArmHome', module.Domain, module.Address, '', function (data) { });
      });            
      widget.find('[data-ui-field=btn_armaway]').on('click', function () {
        HG.Control.Modules.ServiceCall('Control.ArmAway', module.Domain, module.Address, '', function (data) { });
      });            
      widget.find('[data-ui-field=btn_disarm]').on('click', function () {
        HG.Control.Modules.ServiceCall('Control.Disarm', module.Domain, module.Address, '', function (data) { });
      });            
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function () {
        HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
        HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
        HG.WebApp.ProgramsList.UpdateOptionsPopup();
      });
    }
    //
    // read some context data
    //
    this.GroupName = container.attr('data-context-group');
    //
    // get module watts prop
    //


    var armedstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level"); //HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityArmed");
    if (armedstatus != null && armedstatus.Value == "1") {
      widget.find('[data-ui-field=arm_group]').hide();
      widget.find('[data-ui-field=disarm_group]').show();
    }
    else {
      widget.find('[data-ui-field=arm_group]').show();
      widget.find('[data-ui-field=disarm_group]').hide();
    }

    var alarmstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityTriggered");
    if (alarmstatus != null && alarmstatus.Value == "1") {
      this.StatusText = "ALARM!";
    }
    else {
      this.StatusText = "OK";
    }

    var armedlevel = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
    var armedstatus = HG.WebApp.Utility.GetModulePropertyByName(module, "HomeGenie.SecurityArmed");
    var statuscolor = 'rgba(100, 255, 100, 0.35)';
    if (armedlevel != null && armedlevel.Value == "1") {
      if (armedstatus != null && armedstatus.Value != "Disarmed") {
        this.Description = "Armed " + armedstatus.Value;
        if (armedstatus.Value == 'Away')
            statuscolor = 'rgba(255, 50, 50, 0.45)';
        else
            statuscolor = 'rgba(50, 50, 255, 0.45)';
      }
      else {
        this.Description = "Arming...";
        statuscolor = 'rgba(255, 255, 50, 0.45)';
      }
    }
    else {
      this.Description = "Disarmed";
      statuscolor = 'rgba(50, 255, 50, 0.45)';
    }
    //
    // render widget
    //
    var title = HG.WebApp.Locales.GetProgramLocaleString(module.Address, 'Title', 'Alarm System');
    widget.find('[data-ui-field=name]').html(title);
    widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
    widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle">' + this.StatusText + '</span>');
    widget.find('[data-ui-field=description]').html(this.Description);
    widget.find('[data-ui-field=status-container]').css('background', statuscolor);

    var _this = this;
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + module.Domain + '/' + module.Address + '/Events.List',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            _this.refreshLog(data);
        }
    });

  },

  refreshLog: function(logData) {
    var log = '';
    if (logData.length > 0) {
        for(var i = logData.length-1; i >= 0; i--) {
            var name = logData[i].Name;
            var param = logData[i].Parameter;
            var val = logData[i].Value;
            var date = moment(logData[i].Timestamp).locale(HG.WebApp.Locales.GetUserLanguage()).fromNow();
            log += '<div class="ui-block-a" style="border-top:1px solid rgba(200,200,200,0.5)"><strong>'+name+'</strong><br/>'+param+'='+val+'</div>';
            log += '<div class="ui-block-b" align="right" style="border-top:1px solid rgba(200,200,200,0.5)"><br/>'+date+'</div>';
        }
    } else {
        log = '<div align="center" style="line-height: 148px">No recent activity</div>';
    }
    this.Widget.find('[data-ui-field=activity-log]').html(log);
  }

}]