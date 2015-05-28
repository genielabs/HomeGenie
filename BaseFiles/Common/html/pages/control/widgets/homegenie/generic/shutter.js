[{
    Name: "Generic Shutter",
    Author: "Gene",
    Version: "2013-04-03",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/shutters_open.png',
    StatusText: '',
    Description: '',
    UpdateTime: '',

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        var invertcontrols = false;
        //
        // create and store a local reference to control popup object
        //
        if (!controlpopup) {
            container.find('[data-ui-field=controlpopup]').trigger('create');
            controlpopup = container.find('[data-ui-field=controlpopup]').popup();
            widget.data('ControlPopUp', controlpopup);
            //
            // initialization stuff here
            //
            // when options button is clicked control popup is shown
            widget.find('[data-ui-field=options]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            widget.find('[data-ui-field=icon]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            //
            // level knob
            //
            controlpopup.find('[data-ui-field=level_knob]').knob({
                'release': function (v) {
                    v = Math.round(v);
                   HG.Control.Modules.ServiceCall("Control.Level", module.Domain, module.Address, v, function (data) { });
                },
                'change': function (v) {
                    v = Math.round(v);
                    controlpopup.find('[data-ui-field=status]').html(v + '%');
                }
            });
            //
            // ui events handlers
            //
            // on button action
            widget.find('[data-ui-field=open]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=open_button]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            //
            // off button action
            widget.find('[data-ui-field=close]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=close_button]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            //
            // Calibrationp button action
            controlpopup.find('[data-ui-field=calibration_button]').on('click', function () {
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
        }
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        // read module status prop
        //
        var status = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
        var level = 0;
        var leveltext = '';
        if (status != null) {
            level = status.Value.replace(',', '.') * 100;
            var updatetime = status.UpdateTime;
            if (typeof updatetime != 'undefined') {
                updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
                var d = new Date(updatetime);
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            //
            if ((invertcontrols && level != 1) || level > 0) {
            	if( level == 100 )
	                leveltext = 'Ouvert';
	            else
	                leveltext = level+"%";
                this.StatusText = '<span style="vertical-align:middle">' + leveltext + '</span> ';
                this.StatusText += '<img width="15" height="15" src="images/common/led_green.png" style="vertical-align:middle" />';
                this.IconImage = 'pages/control/widgets/homegenie/generic/images/shutters_open.png';
            }
            else {
                leveltext = 'Fermé';
                this.StatusText = '<span style="vertical-align:middle">' + leveltext + '</span> ';
                this.StatusText += '<img width="15" height="15" src="images/common/led_black.png" style="vertical-align:middle" />';
                this.IconImage = 'pages/control/widgets/homegenie/generic/images/shutters_closed.png';
            }
        }
        else {
            this.StatusText = '<span style="vertical-align:middle">?</span>';
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/shutters_closed.png';
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
        //
        // render control popup
        //
        controlpopup.find('[data-ui-field=icon]').attr('src', this.IconImage);
        controlpopup.find('[data-ui-field=group]').html(this.GroupName);
        controlpopup.find('[data-ui-field=name]').html(module.Name);
        controlpopup.find('[data-ui-field=status]').html(leveltext);
        controlpopup.find('[data-ui-field=level_knob]').val(Math.round(level)).trigger('change');
    }

}]