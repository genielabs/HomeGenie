[{
    Name: "UPnP Media Server",
    Author: "Generoso Martello",
    Version: "2013-10-04",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/media.png',
    StatusText: '',
    Description: '',
    DeviceInfo: '',
    NavStack: ['0'],
    ControlPopup: null,
    Module: null,
    Receiver: null,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var controlpopup = this.ControlPopup = widget.data('ControlPopUp');
        //
        // create and store a local reference to control popup object
        //
        if (!controlpopup) {
            this.Module = module;
            //
            container.find('[data-ui-field=controlpopup]').trigger('create');
            controlpopup = this.ControlPopup = container.find('[data-ui-field=controlpopup]').popup();
            widget.data('ControlPopUp', controlpopup);
            //
            // initialization stuff here
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            // when browse button is clicked control popup is shown
            widget.find('[data-ui-field=browse]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
                }
            });
            //
            var _this = this;
            controlpopup.on('popupafteropen', function (event) {
                var browselist = controlpopup.find('[data-ui-field=browsecontent]');
                var item = (_this.NavStack.length > 0 ? _this.NavStack[_this.NavStack.length - 1] : '0');
                _this.RenderBrowseList(browselist, item);
            });
        }
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        //
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        this.Description = module.Description;
        this.DeviceInfo = HG.WebApp.Utility.GetModulePropertyByName(module, "UPnP.ModelDescription").Value;
        //
        // render control popup
        //
        controlpopup.find('[data-ui-field=icon]').attr('src', this.IconImage);
        controlpopup.find('[data-ui-field=group]').html(this.GroupName);
        controlpopup.find('[data-ui-field=name]').html(module.Name);
        //
        var sendtolist = controlpopup.find('[data-ui-field=sendto]');
        sendtolist.empty();
        for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
            var upnpdev = HG.WebApp.Data.Modules[m];
            var devtype = HG.WebApp.Utility.GetModulePropertyByName(upnpdev, "UPnP.StandardDeviceType");
            if (upnpdev.DeviceType == 'MediaReceiver' || (devtype != null && devtype.Value == 'MediaRenderer')) {
                var dn = (upnpdev.Name != '' ? upnpdev.Name : upnpdev.Description);
                var mr = $('<option value="' + upnpdev.Address + '">' + dn + '</option>')
                sendtolist.append(mr);
            }
        }
        sendtolist.append('<option value="browser">Open in browser</option>');
        sendtolist.selectmenu('refresh');
        // 
        widget.find('[data-ui-field=description]').html(this.DeviceInfo != '' ? this.DeviceInfo : this.Description);
    },

    RenderBrowseList: function (browselist, folderid) {
        var _this = this;
        $.mobile.loading('show');
        HG.Control.Modules.ServiceCall('AvMedia.Browse', _this.Module.Domain, _this.Module.Address, folderid, function (data) {
            browselist.empty();
            var items = eval(data);
            if (typeof (items) == 'undefined') return false;
            //
            if (folderid != '0') {
                var levelupitem = $('<li><a style="white-space:nowrap;" class="ui-btn ui-icon-back ui-btn-icon-left" href="#">Parent folder</a></li>');
                levelupitem.on('click', function (e) {
                    _this.NavStack.pop();
                    var clicked = _this.NavStack[_this.NavStack.length - 1];
                    _this.RenderBrowseList(browselist, clicked);
                });
                browselist.append(levelupitem);
            }
            //
            for (i = 0; i < items.length; i++) {
                var iconfile = 'browser_folder'; // object.container
                if (items[i].Class.indexOf('object.item.videoItem') == 0) {
                    iconfile = 'browser_video';
                }
                else if (items[i].Class.indexOf('object.item.audioItem') == 0) {
                    iconfile = 'browser_audio';
                }
                else if (items[i].Class.indexOf('object.item.imageItem') == 0) {
                    iconfile = 'browser_image';
                }
                var icon = '<img height="34" src="pages/control/widgets/homegenie/generic/images/' + iconfile + '.png" align="left" style="margin-top:4px;margin-left:6px">';
                var litem = $('<li data-context-id="' + items[i].Id + '" data-mini="true" data-icon="false" class="ui-li-has-thumb"><a style="white-space:nowrap;" href="#">' + icon + ' ' + items[i].Title + '</a></li>');
                if (items[i].Class.indexOf('object.container') == 0) {
                    litem.on('click', function (e) {
                        _this.NavStack.push($(this).attr('data-context-id'));
                        _this.RenderBrowseList(browselist, $(this).attr('data-context-id'));
                    });
                }
                else {
                    litem.on('click', function (e) {
                        $.mobile.loading('show');
                        var mediaid = $(this).attr('data-context-id');
                        var mediareceiver = _this.ControlPopup.find('[data-ui-field=sendto]').val();
                        var servicecall = HG.Control.Modules.ServiceCall;
                        servicecall('AvMedia.GetUri', _this.Module.Domain, _this.Module.Address, mediaid, function (data) {
                            if (mediareceiver == 'browser') {
                                window.open(data);
                                $.mobile.loading('hide');
                            }
                            else {
                                servicecall('AvMedia.SetUri', _this.Module.Domain, mediareceiver, encodeURIComponent(data), function () {
                                    setTimeout(function () {
                                        servicecall('AvMedia.Play', _this.Module.Domain, mediareceiver, '', function () {
                                            $.mobile.loading('hide');
                                        });
                                    }, 1000);
                                });
                            }
                        });
                    });
                }
                browselist.append(litem);
            }
            browselist.listview('refresh');
            _this.ControlPopup.popup("reposition", { positionTo: 'window' });
            $.mobile.loading('hide');
        });
    }

}]