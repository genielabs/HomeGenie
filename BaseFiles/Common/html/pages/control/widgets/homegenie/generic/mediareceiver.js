[{
    Name: "UPnP Media Receiver",
    Author: "Generoso Martello",
    Version: "2013-10-04",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/media_receiver.png',
    StatusText: '',
    Description: '',
    DeviceInfo: '',
    NavStack: ['0'],
    Widget: null,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = this.Widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        var servicecall = HG.Control.Modules.ServiceCall;
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
            var _this = this;
            //controlpopup.on('popupafteropen', function(event)
            //{
            //	_this.PollStatus(module);
            //});
            // set current volume on slider change
            widget.find('[data-ui-field=media_volume]').on('slidestop', function () {
                _this.IsMouseDown = false;
                servicecall('AvMedia.SetVolume', module.Domain, module.Address, $(this).val());
            });
            widget.find('[data-ui-field=media_volume]').on('slidestart', function (event) {
                _this.IsMouseDown = true;
            });
            //
            // ui events handlers
            //
            // media buttons action
            widget.find('[data-ui-field=media_play]').on('click', function () {
                servicecall('AvMedia.Play', module.Domain, module.Address);
                widget.find('[data-ui-field=media_play]').hide();
                widget.find('[data-ui-field=media_pause]').show();
            });
            widget.find('[data-ui-field=media_pause]').on('click', function () {
                servicecall('AvMedia.Pause', module.Domain, module.Address);
                widget.find('[data-ui-field=media_pause]').hide();
                widget.find('[data-ui-field=media_play]').show();
            });
            widget.find('[data-ui-field=media_pause]').on('click', function () {
                servicecall('AvMedia.Pause', module.Domain, module.Address);
            });
            widget.find('[data-ui-field=media_stop]').on('click', function () {
                servicecall('AvMedia.Stop', module.Domain, module.Address);
                widget.find('[data-ui-field=media_pause]').hide();
                widget.find('[data-ui-field=media_play]').show();
            });
            widget.find('[data-ui-field=media_prev]').on('click', function () {
                servicecall('AvMedia.Prev', module.Domain, module.Address);
            });
            widget.find('[data-ui-field=media_next]').on('click', function () {
                servicecall('AvMedia.Next', module.Domain, module.Address);
            });
            widget.find('[data-ui-field=media_mute]').on('click', function () {
                servicecall('AvMedia.GetMute', module.Domain, module.Address, '', function (res) {
                    if (res == '0' || res.toLowerCase() == 'false') {
                        servicecall('AvMedia.SetMute', module.Domain, module.Address, '1');
                    }
                    else {
                        servicecall('AvMedia.SetMute', module.Domain, module.Address, '0');
                    }
                });
            });
        }
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        //
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        this.Description = module.Description;
        this.DeviceInfo = HG.WebApp.Utility.GetModulePropertyByName(module, "UPnP.ModelDescription").Value;
        //
        // render control popup
        //
        //controlpopup.find('[data-ui-field=icon]').attr('src', this.IconImage);
        //controlpopup.find('[data-ui-field=group]').html(this.GroupName);
        //controlpopup.find('[data-ui-field=name]').html(module.Name);
        widget.find('[data-ui-field=description]').html(this.DeviceInfo != '' ? this.DeviceInfo : this.Description);
        //
        // update play status and media info
        //
        this.PollStatus(module);
    },

    PollStatus: function (module) {
        var _this = this;
        if (!_this.IsMouseDown) {
            var servicecall = HG.Control.Modules.ServiceCall;
            servicecall('AvMedia.GetTransportInfo', module.Domain, module.Address, '', function (res) {
                var trinfo = eval(res)[0];
                var state = trinfo.CurrentTransportState;
                if (_this.Widget != null) {
                    var playbutton = _this.Widget.find('[data-ui-field=media_play]');
                    var pausebutton = _this.Widget.find('[data-ui-field=media_pause]');
                    if (state == 'PAUSED_PLAYBACK') {
                        playbutton.show();
                        pausebutton.hide();
                    }
                    else if (state == 'STOPPED') {
                        playbutton.show();
                        pausebutton.hide();
                    }
                    else if (state == 'PLAYING') {
                        playbutton.hide();
                        pausebutton.show();
                    }
                }
            });
            // get current volume on popup open
            servicecall('AvMedia.GetVolume', module.Domain, module.Address, '', function (res) {
                if (_this.Widget != null) {
                    _this.Widget.find('[data-ui-field=media_volume]').val(res);
                    _this.Widget.find('[data-ui-field=media_volume]').slider('refresh');
                }
            });
        }
        // poll media renderer status while widget is visible
        if (_this.Widget.is(':visible')) {
            setTimeout(function () { _this.PollStatus(module); }, 2500);
        }

    }

}]