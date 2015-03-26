HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || {};

HG.WebApp.SystemSettings.InitializePage = function () {
    $('#page_configure_interfaces').on('pageinit', function (e) {
        if (HOST_SYSTEM.substring(0, 3) == 'Win') {
            $('[data-locale-id=configure_interfaces_lircremote]').hide().next().hide();
            $('[data-locale-id=configure_interfaces_camera]').hide().next().hide();
            $('[data-locale-id=configure_interfaces_weeco4mgpio]').hide().next().hide();
        }
        //    
        $('#systemsettings_zwaveoperation_popup').on('popupbeforeposition', function (event) {
            $('#systemsettings_zwaveoperation_close_button').addClass('ui-disabled');
            $('#systemsettings_zwaveoperation_nodeid').html('<span style="color:green">waiting</span>');
            $('#systemsettings_zwaveoperation_message').html('this operation will timeout in 10 seconds.');
        });
        //
        $('#page_configure_interfaces_zwaveport').change(function (event) {
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.ZWave', 'Options.Set', 'Port', encodeURIComponent($(this).val()));
        });
        $('#systemsettings_zwavehardreset_hardresetbutton').bind('click', function () {
            HG.WebApp.SystemSettings.ZWaveHardReset();
        });
        //
        $('#page_configure_interfaces_insteonport').change(function (event) {
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'Options.Set', 'Port', encodeURIComponent($(this).val()));
        });
        //
        $('#page_configure_interfaces_x10port').change(function (event) {
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'Options.Set', 'Port', encodeURIComponent($(this).val()));
        });
        //
        $('#page_configure_interfaces_w800rf32port').change(function (event) {
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.W800RF', 'Options.Set', 'Port', encodeURIComponent($(this).val()));
        });
        //
        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').change(function (event) {
            var hc = '';
            //
            $('#page_configure_interfaces_x10housecodes input[type=checkbox]').each(function () {
                if ($(this).prop('checked')) {
                    hc += $(this).val() + ',';
                }
            });
            //
            if (hc != '') {
                hc = hc.substr(0, hc.length - 1);
                HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'Options.Set', 'HouseCodes', encodeURIComponent(hc));
                $('#control_groupslist').empty(); // forces control menu rebuild
            }
            else {
                $('#page_configure_interfaces_x10housecodes').qtip({
                    content: {
                        title: HG.WebApp.Locales.GetLocaleString('systemsettings_x10housecodes_title'),
                        text: HG.WebApp.Locales.GetLocaleString('systemsettings_x10housecodes_text'),
                        button: HG.WebApp.Locales.GetLocaleString('systemsettings_x10housecodes_button')
                    },
                    show: { event: false, ready: true, delay: 500 },
                    events: {
                        hide: function () {
                            $(this).qtip('destroy');
                        }
                    },
                    hide: { event: false, inactive: 3000 },
                    style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                    position: { my: 'bottom center', at: 'top center' }
                });
            }
        });
        //
        $('#page_configure_interfaces_insteonx10housecodes input[type=checkbox]').change(function (event) {
            var hc = '';
            //
            $('#page_configure_interfaces_insteonx10housecodes input[type=checkbox]').each(function () {
                if ($(this).prop('checked')) {
                    hc += $(this).val() + ',';
                }
            });
            //
            hc = hc.substr(0, hc.length - 1);
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'Options.Set', 'HouseCodes', encodeURIComponent(hc));
            $('#control_groupslist').empty(); // forces control menu rebuild
        });
        //
        // Interfaces enable / disable switches
        //
        $("#configure_interfaces_flip_zwave").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('zwave');
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.ZWave', 'IsEnabled.Set', $("#configure_interfaces_flip_zwave").val(), '', function (data) {
                $('#control_groupslist').empty(); // forces control menu rebuild
                if ($("#configure_interfaces_flip_zwave").val() == '1' && $('#page_configure_interfaces_zwaveport').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_zwaveport');
                }
                if ($("#configure_interfaces_flip_zwave").val() == '1' && $('#page_configure_interfaces_zwaveport').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_zwaveport');
                }
            });
        });
        //
        $("#configure_interfaces_flip_insteon").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('insteon');
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'IsEnabled.Set', $("#configure_interfaces_flip_insteon").val(), '', function (data) {
                $('#control_groupslist').empty(); // forces control menu rebuild
                if ($("#configure_interfaces_flip_insteon").val() == '1' && $('#page_configure_interfaces_insteonport').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_insteonport');
                }
                if ($("#configure_interfaces_flip_insteon").val() == '1' && $('#page_configure_interfaces_insteonport').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_insteonport');
                }
            });
        });
        //
        $("#configure_interfaces_flip_x10").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('x10');
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'IsEnabled.Set', $("#configure_interfaces_flip_x10").val(), '', function (data) {
                $('#control_groupslist').empty(); // forces control menu rebuild
                if ($("#configure_interfaces_flip_x10").val() == '1' && $('#page_configure_interfaces_x10port').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_x10port');
                }
                if ($("#configure_interfaces_flip_x10").val() == '1' && $('#page_configure_interfaces_x10port').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_x10port');
                }
            });
        });
        //
        $("#configure_interfaces_flip_w800rf32").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('w800rf32');
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.W800RF', 'IsEnabled.Set', $("#configure_interfaces_flip_w800rf32").val(), '', function (data) {
                $('#control_groupslist').empty(); // forces control menu rebuild
                if ($("#configure_interfaces_flip_w800rf32").val() == '1' && $('#page_configure_interfaces_w800rf32port').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_w800rf32port');
                }
                if ($("#configure_interfaces_flip_w800rf32").val() == '1' && $('#page_configure_interfaces_w800rf32port').val() == "") {
                    HG.WebApp.SystemSettings.ShowPortTip('#page_configure_interfaces_w800rf32port');
                }
            });
        });
        //
        $("#configure_interfaces_flip_weeco4mgpio").on('slidestop', function (event) {
            HG.Configure.MIG.InterfaceCommand('EmbeddedSystems.Weeco4mGPIO', 'IsEnabled.Set', $("#configure_interfaces_flip_weeco4mgpio").val(), '', function (data) {
                $('#control_groupslist').empty(); // forces control menu rebuild
            });
        });
        //
        $("#configure_interfaces_flip_upnp").on('slidestop', function (event) {
            HG.Configure.MIG.InterfaceCommand('Protocols.UPnP', 'IsEnabled.Set', $("#configure_interfaces_flip_upnp").val(), '', function (data) {
            });
        });
        //
        $("#configure_interfaces_flip_lircremote").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('lircremote');
            HG.Configure.MIG.InterfaceCommand('Controllers.LircRemote', 'IsEnabled.Set', $("#configure_interfaces_flip_lircremote").val(), '', function (data) {
            });
        });
        //
        $("#configure_interfaces_flip_camera").on('slidestop', function (event) {
            HG.WebApp.SystemSettings.RefreshOptions('camera');
            HG.Configure.MIG.InterfaceCommand('Media.CameraInput', 'IsEnabled.Set', $("#configure_interfaces_flip_camera").val(), '', function (data) {
            });
        });
        //
        $("#configure_interfaces_lircremote_searchinput").keypress(function (e) {
            if (e.which != 13) return;
            $.mobile.loading('show');
            var $ul = $('#configure_interfaces_lircremote_search'),
                $input = $('#configure_interfaces_lircremote_searchinput'),
                value = $input.val(),
                html = "";
            $ul.html("");
            if (value && value.length > 2) {
                $ul.html('<li><div class="ui-loader"><span class="ui-icon ui-icon-loading"></span></div></li>');
                $ul.listview('refresh');
                $.ajax({
                    url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Search/' + value + '/',
                    type: 'GET'
                })
                .then(function (response) {
                    response = eval(response);
                    $.each(response, function (i, val) {
                        html += '<li data-context-value="' + val.Manufacturer + '/' + val.Model + '" data-icon="plus"><a href="#">' + val.Manufacturer + ' ' + val.Model + '</a></li>';
                    });
                    $ul.html(html);
                    $ul.listview("refresh");
                    $ul.children('li').each(function () {
                        var remote = $(this).attr('data-context-value');
                        $(this).on('click', function () {
                            HG.WebApp.SystemSettings.LircRemoteAdd(remote);
                        });
                    });
                    $ul.trigger("create");
                    $.mobile.loading('hide');
                });
            }
        });
        //
        $('#page_configure_interfaces_camerachange').bind('click', function () {
            var device = $('#page_configure_interfaces_cameraport').val();
            var resolution = $('#page_configure_interfaces_cameraresolution').val();
            var width = resolution.split('x')[0];
            var height = resolution.split('x')[1];
            var fps = $('#page_configure_interfaces_camerafps').val();
            HG.Configure.MIG.InterfaceCommand('Media.CameraInput', 'Options.Set', 'Configuration', encodeURIComponent(device + ',' + width + ',' + height + ',' + fps));
        });
    });
};

HG.WebApp.SystemSettings.GetInterface = function (domain) {
    var iface = null;
    var interfaces = HG.WebApp.Data.Interfaces;
    if (interfaces && interfaces != 'undefined') {
        for (i = 0; i < interfaces.length; i++) {
            if (interfaces[i].Domain == domain) {
                iface = interfaces[i];
                break;
            }
        }
    }
    return iface;
};

HG.WebApp.SystemSettings.ShowPortTip = function (el) {
    $(el).qtip({
        content: {
            title: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_title'),
            text: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_text'),
            button: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_button')
        },
        show: { event: false, ready: true, delay: 1000 },
        events: {
            hide: function () {
                $(this).qtip('destroy');
            }
        },
        hide: { event: false, inactive: 3000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { my: 'left center', at: 'right center' }
    });
};

HG.WebApp.SystemSettings.LircRemoteList = function () {
    $.mobile.loading('show');
    $('#lirc_remotes').empty();
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.List/',
        type: 'GET'
    })
    .then(function (response) {
        var remotes = eval(response);
        for (r = 0; r < remotes.length; r++) {
            var remdata = (remotes[r].Manufacturer + '/' + remotes[r].Model);
            $('#lirc_remotes').append($('<li/>', { 'data-icon': 'minus' })
                                .append($('<a/>', {
                                    'href': "javascript:HG.WebApp.SystemSettings.LircRemoteRemove('" + remdata + "')",
                                    'text': remdata.replace('/', ' ')
                                })));
        }
        $('#lirc_remotes').listview('refresh');
        $.mobile.loading('hide');
    });
};

HG.WebApp.SystemSettings.LircRemoteAdd = function (remote) {
    $.mobile.loading('show');
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Add/' + remote + '/',
        type: 'GET'
    })
    .then(function (response) {
        $.mobile.loading('hide');
        HG.WebApp.SystemSettings.LircRemoteList();
    });
};

HG.WebApp.SystemSettings.LircRemoteRemove = function (remote) {
    $.mobile.loading('show');
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/Controllers.LircRemote/0/Remotes.Remove/' + remote + '/',
        type: 'GET'
    })
    .then(function (response) {
        $.mobile.loading('hide');
        HG.WebApp.SystemSettings.LircRemoteList();
    });
};

HG.WebApp.SystemSettings.RefreshOptions = function (dname) {
    if ($("#configure_interfaces_flip_" + dname).val() != "0") {
        $('[id=configure_interfaces_' + dname + 'options]').each(function () { $(this).show() });
    }
    else {
        $('[id=configure_interfaces_' + dname + 'options]').each(function () { $(this).hide() });
    }
}

HG.WebApp.SystemSettings.LoadSettings = function () {
    $.mobile.loading('show');
    HG.Configure.Interfaces.ServiceCall("Hardware.SerialPorts", function (data) {
        var ports = eval(decodeURIComponent(data));
        $('#page_configure_interfaces_zwaveport').empty();
        $('#page_configure_interfaces_zwaveport').append('<option value="">' + HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_placeholder') + '</option>');
        if (ports.length == 0) {
            $('#page_configure_interfaces_zwaveport').append('<option value="">NO SERIAL PORTS FOUND</option>');
        }
        else {
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_zwaveport').append('<option value="' + ports[p] + '">' + ports[p] + '</option>');
            }
        }
        $('#page_configure_interfaces_zwaveport').selectmenu('refresh', true);
        $.mobile.loading('hide');
        //
        HG.Configure.MIG.InterfaceCommand('HomeAutomation.ZWave', 'Options.Get', 'Port', '', function (data) {
            $('#page_configure_interfaces_zwaveport').val(data.ResponseValue);
            $('#page_configure_interfaces_zwaveport').selectmenu('refresh', true);
            //
            $('#page_configure_interfaces_insteonport').empty().append('<option value="">' + HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_placeholder') + '</option>');
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_insteonport').append('<option value="' + ports[p] + '">' + ports[p] + '</option>');
            }
            $('#page_configure_interfaces_insteonport').selectmenu('refresh', true);
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'Options.Get', 'Port', '', function (data) {
                $('#page_configure_interfaces_insteonport').val(data.ResponseValue);
                $('#page_configure_interfaces_insteonport').selectmenu('refresh', true);
            });
            //
            $('#page_configure_interfaces_x10port').empty().append('<option value="">' + HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_placeholder') + '</option>');
            $('#page_configure_interfaces_x10port').append('<option value="USB">CM15 Pro - USB</option>');
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_x10port').append('<option value="' + ports[p] + '">CM11 - ' + ports[p] + '</option>');
            }
            $('#page_configure_interfaces_x10port').selectmenu('refresh', true);
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'Options.Get', 'Port', '', function (data) {
                $('#page_configure_interfaces_x10port').val(data.ResponseValue);
                $('#page_configure_interfaces_x10port').selectmenu('refresh', true);
            });
            $('#page_configure_interfaces_w800rf32port').empty();
            $('#page_configure_interfaces_w800rf32port').append('<option value="">' + HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_placeholder') + '</option>');
            for (var p = 0; p < ports.length; p++) {
                $('#page_configure_interfaces_w800rf32port').append('<option value="' + ports[p] + '">' + ports[p] + '</option>');
            }
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.W800RF', 'Options.Get', 'Port', '', function (data) {
                $('#page_configure_interfaces_w800rf32port').val(data.ResponseValue);
                $('#page_configure_interfaces_w800rf32port').selectmenu('refresh', true);
            });
            HG.Configure.MIG.InterfaceCommand('Media.CameraInput', 'Options.Get', 'Configuration', '', function (data) {
                data = data.ResponseValue.split(',');
                if (data.length > 3)
                {
                    $('#page_configure_interfaces_cameraport').val(data[0]);
                    $('#page_configure_interfaces_cameraport').selectmenu('refresh', true);
                    $('#page_configure_interfaces_cameraresolution').val(data[1] + 'x' + data[2]);
                    $('#page_configure_interfaces_cameraresolution').selectmenu('refresh', true);
                    $('#page_configure_interfaces_camerafps').val(data[3]);
                    $('#page_configure_interfaces_camerafps').selectmenu('refresh', true);
                }
            });
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.ZWave', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_zwave').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('zwave');
            });
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_insteon').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('insteon');
            });
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_x10').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('x10');
            });
            HG.Configure.MIG.InterfaceCommand('HomeAutomation.W800RF', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_w800rf32').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('w800rf32');
            });
            HG.Configure.MIG.InterfaceCommand('Controllers.LircRemote', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_lircremote').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('lircremote');
                HG.WebApp.SystemSettings.LircRemoteList();
            });
            HG.Configure.MIG.InterfaceCommand('Media.CameraInput', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_camera').val(data.ResponseValue).slider('refresh');
                HG.WebApp.SystemSettings.RefreshOptions('camera');
            });
            HG.Configure.MIG.InterfaceCommand('EmbeddedSystems.Weeco4mGPIO', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_weeco4mgpio').val(data.ResponseValue).slider('refresh');
            });
            HG.Configure.MIG.InterfaceCommand('Protocols.UPnP', 'IsEnabled.Get', '', '', function (data) {
                $('#configure_interfaces_flip_upnp').val(data.ResponseValue).slider('refresh');
            });
        });
    });
    HG.Configure.MIG.InterfaceCommand('HomeAutomation.X10', 'Options.Get', 'HouseCodes', '', function (data) {
        data = ',' + data.ResponseValue + ',';
        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').each(function () {
            if (data.indexOf(',' + $(this).val() + ',') >= 0) {
                $(this).prop('checked', true);
            }
            else {
                $(this).prop('checked', false);
            }
        });
        $('#page_configure_interfaces_x10housecodes input[type=checkbox]').checkboxradio('refresh');
    });
    HG.Configure.MIG.InterfaceCommand('HomeAutomation.Insteon', 'Options.Get', 'HouseCodes', '', function (data) {
        data = ',' + data.ResponseValue + ',';
        $('#page_configure_interfaces_insteonx10housecodes input[type=checkbox]').each(function () {
            if (data.indexOf(',' + $(this).val() + ',') >= 0) {
                $(this).prop('checked', true);
            }
            else {
                $(this).prop('checked', false);
            }
        });
        $('#page_configure_interfaces_insteonx10housecodes input[type=checkbox]').checkboxradio('refresh');
    });
};



HG.WebApp.SystemSettings.ZWaveDiscovery = function (port) {
    $('#configure_system_zwavediscovery_log').empty();
    $('#systemsettings_zwavediscovery_popup').popup('open');
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/1/Controller.Discovery/' + (new Date().getTime()), function (data) { });
};

HG.WebApp.SystemSettings.ZWaveHardReset = function (port) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.ZWave/1/Controller.HardReset/' + (new Date().getTime()), function (data) { });
};

HG.WebApp.SystemSettings.ZWaveNodeAdd = function (port) {
    $('#systemsettings_zwaveoperation_title').html('Add Node');
    $('#systemsettings_zwaveoperation_popup').popup('open');
    zwave_NodeAdd(function (res) {
        if (res != 0) {
            HG.WebApp.Control.UpdateModules();
            //
            $('#systemsettings_zwaveoperation_nodeid').html(res);
            $('#systemsettings_zwaveoperation_message').html('node added.');
        }
        else {
            $('#systemsettings_zwaveoperation_nodeid').html('<span style="color:red">timed out</span>');
            $('#systemsettings_zwaveoperation_message').html('operation falied.');
        }
        $('#systemsettings_zwaveoperation_close_button').removeClass('ui-disabled');
    });
};

HG.WebApp.SystemSettings.ZWaveNodeRemove = function (port) {
    $('#systemsettings_zwaveoperation_title').html('Remove Node');
    $('#systemsettings_zwaveoperation_popup').popup('open');
    zwave_NodeRemove(function (res) {
        if (res != 0) {
            HG.WebApp.Control.UpdateModules();
            //
            $('#systemsettings_zwaveoperation_nodeid').html(res);
            $('#systemsettings_zwaveoperation_message').html('node removed.');
        }
        else {
            $('#systemsettings_zwaveoperation_nodeid').html('<span style="color:red">timed out</span>');
            $('#systemsettings_zwaveoperation_message').html('operation falied.');
        }
        $('#systemsettings_zwaveoperation_close_button').removeClass('ui-disabled');
    });
};


HG.WebApp.SystemSettings.CheckConfigureStatus = function () {

    var ifaceurl = '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.List/' + (new Date().getTime());
    $.ajax({
        url: ifaceurl,
        type: 'GET',
        success: function (data) {
            var interfaces = eval(data);
            if (!interfaces || interfaces == 'undefined' || interfaces.length == 0 || interfaces[0].Domain == 'HomeGenie.UpdateChecker') {
                setTimeout(function () {
                    $('#homemenu_option_control').addClass('ui-disabled');
                    $('#popup_system_not_configured').popup().popup('open', { transition: 'slidedown' });
                }, 2000);
            }
            else {
                $('#homemenu_option_control').removeClass('ui-disabled');
            }
        }
    });

};