[{
  
  Name: "Generic Dimmer",
  Author: "Generoso Martello",
  Version: "2015-06-18",

  GroupName: '',
  IconImage: 'pages/control/widgets/homegenie/generic/images/shutters_open.png',
  StatusText: '',
  Description: '',
  UpdateTime: '',

  _initialized: false,
  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    var _this = this;
    if (!this._initialized) {
      this._initialized = true;
      //
      // ui events handlers
      //
      // on button action
      widget.find('[data-ui-field=open]').on('click', function () {
        HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
      });
      // off button action
      widget.find('[data-ui-field=close]').on('click', function () {
        HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
      });
      // Calibration button action
      widget.find('[data-ui-field=calibrate]').on('click', function () {
        var parameter = '';
        var shutterCalibration = HG.WebApp.Utility.GetModulePropertyByName(module, "ZWaveNode.Calibration");
        if((shutterCalibration == null) || (shutterCalibration.value == ''))
        {
          var manufaturerSpecific = HG.WebApp.Utility.GetModulePropertyByName(module, "ZWaveNode.ManufacturerSpecific");
          var specificText = manufaturerSpecific.Value ;
          if( specificText == '010F:0301:1001' ) // Fibaro FGRM-222 AC/DC
            parameter = '29';
          if( specificText == '0159:0003:0002' ) // Qubino ZMNHCA? AC
            parameter = '78';
          HG.WebApp.Utility.SetModulePropertyByName(module,"ZWaveNode.Calibration",parameter);
        }
        else
          parameter = shutterCalibration.Value;
        if( parameter != '' )
          HG.Control.Modules.ServiceCall("Config.ParameterSet", module.Domain, module.Address, parameter+"/1", function (data) { });
      });
      // level slider action
      widget.find('[data-ui-field=level]').on('slidestop', function () {
        HG.Control.Modules.ServiceCall("Control.Level", module.Domain, module.Address, $(this).val(), function (data) { });
      });          
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function () {
        HG.WebApp.Control.EditModule(module);
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
      var w = parseFloat(watts.Value.replace(',', '.')).toFixed(1);
      if (w > 0) {
        watts = w + '<span style="opacity:0.65">W</span>';
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
    } else level = 0;
    this.StatusText = '<span style="vertical-align:middle">' + watts + '</span>';
    //
    // hide dimmer level if device type is switch
    //
    if (module.DeviceType == 'Switch' || module.DeviceType == 'Light') {
      widget.find('[data-ui-field=level-div]').hide();
    } else {
      widget.find('[data-ui-field=level-div]').show();
    }
    //
    // set current icon image
    //
    var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
    if (widgeticon != null && widgeticon.Value != '') {
      this.IconImage = widgeticon.Value;
    } else {
      if (level > 0) {
        this.IconImage = 'pages/control/widgets/homegenie/generic/images/shutters_open.png';
      } else {
        this.IconImage = 'pages/control/widgets/homegenie/generic/images/shutters_closed.png';
      }
    }
    this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
    //
    // render widget
    //
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);
    widget.find('[data-ui-field=status]').html(this.StatusText);
    widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
    widget.find('[data-ui-field=updatetime]').html(this.UpdateTime);
    widget.find('[data-ui-field=level]').val(level).slider().slider('refresh');
  }

}]
