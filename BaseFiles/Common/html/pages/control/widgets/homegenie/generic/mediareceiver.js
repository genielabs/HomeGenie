[{
    Name: "UPnP Media Receiver",
    Author: "Generoso Martello",
    Version: "2013-10-04",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/media_receiver.png',
    StatusText: '',
    Description: '',
    DeviceInfo: '',
    Initialized: false,
    PendingRequests: 0,
    Widget: null,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = this.Widget = container.find('[data-ui-field=widget]');
        var servicecall = HG.Control.Modules.ServiceCall;
        //
        // create and store a local reference to control popup object
        //
        if (!this.Initialized) {
            this.Initialized = true;
            // initialization stuff here
            var _this = this;
            // set current volume on slider change
            widget.find('[data-ui-field=media_volume]').on('slidestop', function () {
                _this.IsMouseDown = false;
                servicecall('AvMedia.SetVolume', module.Domain, module.Address, $(this).val());
            });
            widget.find('[data-ui-field=media_volume]').on('slidestart', function (event) {
                _this.IsMouseDown = true;
            });
            // ui events handlers
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            // media buttons action
            widget.find('[data-ui-field=media_play]').on('click', function () {
                servicecall('AvMedia.Play', module.Domain, module.Address, '');
                widget.find('[data-ui-field=media_play]').hide();
                widget.find('[data-ui-field=media_pause]').show();
            });
            widget.find('[data-ui-field=media_pause]').on('click', function () {
                servicecall('AvMedia.Pause', module.Domain, module.Address, '');
                widget.find('[data-ui-field=media_pause]').hide();
                widget.find('[data-ui-field=media_play]').show();
            });
            widget.find('[data-ui-field=media_pause]').on('click', function () {
                servicecall('AvMedia.Pause', module.Domain, module.Address);
            });
            widget.find('[data-ui-field=media_stop]').on('click', function () {
                servicecall('AvMedia.Stop', module.Domain, module.Address, '');
                widget.find('[data-ui-field=media_pause]').hide();
                widget.find('[data-ui-field=media_play]').show();
            });
            widget.find('[data-ui-field=media_prev]').on('click', function () {
                servicecall('AvMedia.Prev', module.Domain, module.Address, '');
            });
            widget.find('[data-ui-field=media_next]').on('click', function () {
                servicecall('AvMedia.Next', module.Domain, module.Address, '');
            });
            widget.find('[data-ui-field=media_mute]').on('click', function () {
                if (_this.IsMuted) {
                    servicecall('AvMedia.SetMute', module.Domain, module.Address, '0');
                    _this.Widget.find('[data-ui-field=media_mute]').attr('src', 'pages/control/widgets/homegenie/generic/images/media_mute.png');
                }
                else {
                    servicecall('AvMedia.SetMute', module.Domain, module.Address, '1');
                    _this.Widget.find('[data-ui-field=media_mute]').attr('src', 'pages/control/widgets/homegenie/generic/images/media_unmute.png');
                }
            });
        }
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        this.Description = module.Description;
        var modelDescription = HG.WebApp.Utility.GetModulePropertyByName(module, "UPnP.ModelDescription");
        this.DeviceInfo = (modelDescription != null ? modelDescription.Value : '');
        widget.find('[data-ui-field=description]').html(this.DeviceInfo != '' ? this.DeviceInfo : this.Description);
        //
        // update play status and media info
        //
        this.PollStatus(module);
    },

    PollStatus: function (module) {
        var _this = this;
        if (_this.PendingRequests == 0 && !_this.IsMouseDown) {
            var servicecall = HG.Control.Modules.ServiceCall;
            _this.PendingRequests++;
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
                        _this.Widget.find('[data-ui-field=media_position]').html('');
                    }
                    else if (state == 'PLAYING') {
                        playbutton.hide();
                        pausebutton.show();
                        servicecall('AvMedia.GetPositionInfo', module.Domain, module.Address, '', function (res) {
                            var posinfo = eval(res)[0];
                            _this.Widget.find('[data-ui-field=media_position]').html(posinfo.RelTime);
                        });
                    }
                }
                _this.PendingRequests--;
            });
            // get current volume on popup open
            _this.PendingRequests++;
            servicecall('AvMedia.GetVolume', module.Domain, module.Address, '', function (res) {
                if (_this.Widget != null) {
                    _this.Widget.find('[data-ui-field=media_volume]').val(res);
                    _this.Widget.find('.ui-slider').width(250);
                    _this.Widget.find('[data-ui-field=media_volume]').slider('refresh');
                }
                _this.PendingRequests--;
            });
            // get mute status
            _this.PendingRequests++;
            servicecall('AvMedia.GetMute', module.Domain, module.Address, '', function (res) {
                if (res == '0' || res.toLowerCase() == 'false') {
                    _this.Widget.find('[data-ui-field=media_mute]').attr('src', 'pages/control/widgets/homegenie/generic/images/media_mute.png');
                    _this.IsMuted = false;
                }
                else {
                    _this.Widget.find('[data-ui-field=media_mute]').attr('src', 'pages/control/widgets/homegenie/generic/images/media_unmute.png');
                    _this.IsMuted = true;
                }
                _this.PendingRequests--;
            });
        }
        // poll media renderer status while widget is visible
        if (_this.Widget.is(':visible')) {
            setTimeout(function () { _this.PollStatus(module); }, 3000);
        }
    }

}]