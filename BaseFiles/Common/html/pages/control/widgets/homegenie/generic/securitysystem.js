[{
  Name: "Security Alarm System",
  Author: "Generoso Martello",
  Version: "2013-08-21",

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/generic/images/securitysystem.png',
  StatusText: '',
  Description: '',
  UpdateTime: '',

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
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
    if (armedlevel != null && armedlevel.Value == "1") {
      if (armedstatus != null && armedstatus.Value != "Disarmed") {
        this.Description = "Armed " + armedstatus.Value;
      }
      else {
        this.Description = "Arming...";
      }
    }
    else {
      this.Description = "Disarmed";
    }
    //
    // render widget
    //
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
    widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle">' + this.StatusText + '</span>');
    widget.find('[data-ui-field=description]').html(this.Description);
  }

}]