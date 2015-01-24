[{
    Name: "Generic Siren",
    Author: "Generoso Martello",
    Version: "2013-03-31",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/siren.png',
    StatusText: '',
    Description: '',
    UpdateTime: '',

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
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
            //widget.find('[data-ui-field=options]').on('click', function() {
            //	if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp'))
            //	{
            //		$(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
            //	}
            //});
            //
            // ui events handlers
            //
            //
            // ui events handlers
            //
            // on button action
            widget.find('[data-ui-field=on]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=on]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.On", module.Domain, module.Address, null, function (data) { });
            });
            //
            // off button action
            widget.find('[data-ui-field=off]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            controlpopup.find('[data-ui-field=off]').on('click', function () {
                HG.Control.Modules.ServiceCall("Control.Off", module.Domain, module.Address, null, function (data) { });
            });
            //
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
                this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d); //HG.WebApp.Utility.GetElapsedTimeText(d);
            }
            //
            level = level.Value.replace(',', '.') * 100;
            if (level >= 99 || level == 0) {
                this.StatusText = '<span style="vertical-align:middle">' + (level >= 99 ? 'ON' : 'OFF') + '</span> ';
                this.StatusText += '<img width="15" height="15" src="images/common/led_' + (level >= 99 ? 'green' : 'black') + '.png" style="vertical-align:middle" />';
            }
            else {
                this.StatusText = '<span style="vertical-align:middle">' + level.toFixed(0) + '%</span> ';
                this.StatusText += '<img width="15" height="15" src="images/common/led_green.png" style="vertical-align:middle" />';
            }
        } else this.StatusText = '';
        this.StatusText = '<span style="vertical-align:middle">' + watts + '</span> ' + this.StatusText;
        //
        // get light level prop for icon image ... 
        // TODO: merge with the code above
        //
        level = 0;
        var levelprop = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
        if (levelprop != null) {
            level = levelprop.Value.replace(',', '.') * 100;
            if (level > 0) {
                this.IconImage = 'pages/control/widgets/homegenie/generic/images/siren.png';
            }
            else {
                this.IconImage = 'pages/control/widgets/homegenie/generic/images/siren.png';
            }
        }
        else {
            level = "?";
            this.IconImage = 'pages/control/widgets/homegenie/generic/images/siren.png';
        }
        this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' <strong>' + module.Address + '</strong>';
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
        controlpopup.find('[data-ui-field=status]').html(this.StatusText);
    }

}]