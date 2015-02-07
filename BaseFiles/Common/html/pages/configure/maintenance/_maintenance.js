﻿// HomeAutomation.HomeGenie / Config / System.Configure / System.ConfigurationBackup

HG.WebApp.Maintenance = HG.WebApp.Maintenance || {};

HG.WebApp.Maintenance.InitializePage = function () {
    $('#page_configure_maintenance').on('pageinit', function (e) {

        $('#systemsettings_httpport_change').bind('click', function () {
            var port = $('#http_service_port').val();
            $.mobile.loading('show');
            HG.System.SetHttpPort(port, function (data) {
                $.mobile.loading('hide');
            });
        });
        //
        $("#configure_system_flip_httpcache").on('slidestop', function (event) {
            $.mobile.loading('show');
            if ($("#configure_system_flip_httpcache").val() == '1') {
                HG.System.WebCacheEnable(function (data) {
                    $.mobile.loading('hide');
                });
            }
            else {
                HG.System.WebCacheDisable(function (data) {
                    $.mobile.loading('hide');
                });
            }
        });
        //
        $("#configure_system_flip_logging").on('slidestop', function (event) {
            $.mobile.loading('show');
            if ($("#configure_system_flip_logging").val() == '1') {
                HG.System.LoggingEnable(function (data) {
                    $.mobile.loading('hide');
                });
            }
            else {
                HG.System.LoggingDisable(function (data) {
                    $.mobile.loading('hide');
                });
            }
        });
        //
        $("#configure_system_flip_eventshistory").on('slidestop', function (event) {
            if ($("#configure_system_flip_eventshistory").val() == '1') {
                dataStore.set('UI.EventsHistory', true);
                $('#btn_eventshistory_led').show();
            }
            else {
                dataStore.set('UI.EventsHistory', false);
                $('#btn_eventshistory_led').hide();
            }
        });
        //
        $('#configure_system_updatemanager_updatebutton').bind('click', function () {
            $('#configure_system_updatemanager_info').html('<strong>Checking for updates...</strong>');
            $.mobile.loading('show');
            HG.System.UpdateManager.UpdateCheck(function (res) {
                $.mobile.loading('hide');
                HG.WebApp.Maintenance.LoadUpdateCheckSettings();
            });
        });
        $('#configure_system_updateinstall_button').bind('click', function () {
            $('#configure_system_updateinstall_button').addClass('ui-disabled');
            $('#configure_system_updatemanager_info').html('<strong>Installing updates...</strong>');
            $('#configure_system_updateinstall_status').html('Installing files...');
            $.mobile.loading('show', {
                text: 'Please wait...',
                textVisible: true,
                html: ""
            });
            HG.System.UpdateManager.InstallUpdate(function (res) {
                var r = eval(res);
                if (typeof r != 'undefined') {
                    // TODO: ....
                    if (r[0].ResponseValue == 'OK') {
                        $('#configure_system_updateinstall_status').html('Update install complete.');
                        setTimeout(function () {
                            document.location.href = '/';
                        }, 3000);
                    }
                    else if (r[0].ResponseValue == 'RESTART') {
                        $('#configure_system_updateinstall_status').html('Installing files... (HomeGenie service stopped)');
                    }
                    else if (r[0].ResponseValue == 'ERROR') {
                        $('#configure_system_updateinstall_status').html('Error during installation progress.');
                        //
                        $.mobile.loading('hide');
                        HG.WebApp.Maintenance.LoadUpdateCheckSettings();
                    }
                }
            });
        });
        //
        //$('systemsettings_updateinstall_popup').on('popupbeforeposition', function (event) {
        //    
        //});
        $('#configure_system_updatemanager_proceedbutton').bind('click', function () {
            $('#configure_system_updateinstall_button').addClass('ui-disabled');
            $('#configure_system_updatemanager_info').html('<strong>Downloading files...</strong>');
            $('#configure_system_updateinstall_log').empty();
            HG.WebApp.Utility.SwitchPopup('#systemsettings_updatewarning_popup', '#systemsettings_updateinstall_popup', true)
            $('#configure_system_updateinstall_status').html('Downloading files...');
            $.mobile.loading('show', {
                text: 'Please wait...',
                textVisible: true,
                html: ""
            });
            HG.System.UpdateManager.DownloadUpdate(function (res) {
                $.mobile.loading('hide');
                HG.WebApp.Maintenance.LoadUpdateCheckSettings();
                //
                var r = eval(res);
                if (typeof r != 'undefined') {
                    // TODO: ....
                    if (r[0].ResponseValue == 'OK') {
                        $('#configure_system_updateinstall_status').html('Update files ready.');
                        $('#configure_system_updateinstall_button').removeClass('ui-disabled');
                    }
                    else if (r[0].ResponseValue == 'RESTART') {
                        $('#configure_system_updateinstall_status').html('Update files ready. HomeGenie will be restarted after updating.');
                        $('#configure_system_updateinstall_button').removeClass('ui-disabled');
                    }
                    else if (r[0].ResponseValue == 'ERROR') {
                        $('#configure_system_updateinstall_status').html('Error while downloading update files.');
                        $('#configure_system_updateinstall_button').addClass('ui-disabled');
                    }
                }
            });
        });
        //
        $('#securitysettings_password_change').bind('click', function () {
            var pass = $('#securitysettings_user_password').val();
            $.mobile.loading('show');
            HG.System.SetPassword(pass, function (data) {
                $.mobile.loading('hide');
                setTimeout(function () {
                    HG.WebApp.Maintenance.LoadSecuritySettings();
                }, 1000);
            });
        });
        $('#securitysettings_password_clear').bind('click', function () {
            $.mobile.loading('show');
            HG.System.ClearPassword(function (data) {
                $.mobile.loading('hide');
                HG.WebApp.Maintenance.LoadSecuritySettings();
            });
        });
        //
        $('#btn_configuresystem_downloadtday').bind('click', function () {
            window.open('/api/HomeAutomation.HomeGenie/Config/System.Configure/SystemLogging.DownloadCsv/0');
        });
        //
        $('#btn_configuresystem_downloadyday').bind('click', function () {
            window.open('/api/HomeAutomation.HomeGenie/Config/System.Configure/SystemLogging.DownloadCsv/1');
        });
        //
        $('#maintenance_configuration_restartproceedbutton').bind('click', function () {
            $('#maintenance_configuration_restartbutton').addClass('ui-disabled');
            $.mobile.loading('show', { text: 'Restarting service, please wait...', textVisible: true, theme: 'a', html: '' });
            // FACTORY RESET....
            HG.Configure.System.ServiceCall("Service.Restart", function (data) { });
        });
        //
        $('#maintenance_configuration_backupbutton').bind('click', function () {
            //$.mobile.loading('show');
            //HG.Configure.System.ServiceCall("System.ConfigurationBackup", function (data) {
            //    $.mobile.loading('hide');
            //});
        });
        $('#maintenance_configuration_restorebutton').bind('click', function () {
            if ($('#restore_configuration_uploadfile').val() == "") {
                alert('Select a file to restore first');
                $('#restore_configuration_uploadfile').parent().stop().animate({ borderColor: "#FF5050" }, 250)
                    .animate({ borderColor: "#FFFFFF" }, 250)
                    .animate({ borderColor: "#FF5050" }, 250)
                    .animate({ borderColor: "#FFFFFF" }, 250);
            }
            else {
                $('#systemsettings_backuprestores1cancelbutton').removeClass('ui-disabled');
                $('#systemsettings_backuprestores1confirmbutton').removeClass('ui-disabled');
                $.mobile.loading('show', { text: 'Restoring backup, please wait...', textVisible: true, theme: 'a', html: '' });
                $('#restore_configuration_form').submit();
            }
        });
        $('#maintenance_configuration_factoryresetbutton').bind('click', function () {
            $.mobile.loading('show', { text: 'Resetting to factory defaults, please wait...', textVisible: true, theme: 'a', html: '' });
            // FACTORY RESET....
            HG.Configure.System.ServiceCall("System.ConfigurationReset", function (data) {
                alert('Factory Reset Completed!');
                $.mobile.loading('hide');
                window.location.replace("/");
            });
        });
        $('#systemsettings_backuprestores1selectallbtn').bind('click', function () {
            if (HG.WebApp.Maintenance.RestoreProgramList != '') {
                HG.WebApp.Maintenance.RestoreProgramList = '';
                $('#systemsettings_backuprestores1plist :checkbox').prop('checked', false).checkboxradio("refresh");
            }
            else {
                HG.WebApp.Maintenance.RestoreProgramList = '';
                $('#systemsettings_backuprestores1plist').find(':checkbox').each(function () {
                    $(this).prop('checked', true).checkboxradio("refresh");
                    HG.WebApp.Maintenance.RestoreProgramList += ',' + $(this).prop('value') + ',';
                });
            }
        });
        $('#restore_configuration_uploadframe').bind('load', function () {
            HG.Configure.System.ServiceCall("System.ConfigurationRestoreS1", function (data) {
                $.mobile.loading('hide');
                $('#systemsettings_backuprestores1plist').empty();
                var programs = data;
                if (programs.length == 0) {
                    $('#systemsettings_backuprestores1plist').append("<p>No user's program found in this backup.</p>");
                    $('#systemsettings_backuprestores1selectallbtn').addClass('ui-disabled');
                }
                else {
                    for (var p = 0; p < programs.length; p++) {
                        var pli = '<input onchange="HG.WebApp.Maintenance.RestoreProgramToggle(this)" type="checkbox" value="' + programs[p].Address + '" name="restoreprogram-' + p + '" id="restoreprogram-' + p + '" data-mini="true" /><label for="restoreprogram-' + p + '">' + programs[p].Address + ' ' + programs[p].Name + '</label>';
                        $('#systemsettings_backuprestores1plist').append(pli);
                    }
                    $('#systemsettings_backuprestores1selectallbtn').removeClass('ui-disabled');
                }
                $('#systemsettings_backuprestores1plist').trigger('create');
                $('#systemsettings_backuprestores1').popup('open');
            });
            $('#systemsettings_backuprestores1confirmbutton').bind('click', function () {
                $('#systemsettings_backuprestores1cancelbutton').addClass('ui-disabled');
                $('#systemsettings_backuprestores1confirmbutton').addClass('ui-disabled');
                $.mobile.loading('show', { text: 'Please be patient, this may take some time...', textVisible: true, theme: 'a', html: '' });
                HG.Configure.System.ServiceCall("System.ConfigurationRestoreS2/" + HG.WebApp.Maintenance.RestoreProgramList, function (data) {
                    $.mobile.loading('hide');
                    window.location.replace("/");
                });
            });
        });
        //
        $('#systemsettings_modulesdelete').on('popupbeforeposition', function (event) {

            HG.WebApp.Maintenance.RefreshModulesList();

        });
        $('#configure_interfaces_modules_list').on('click', 'li', function () {
            $.mobile.loading('show');
            //
            var domain = $(this).attr('data-context-domain');
            var address = $(this).attr('data-context-address');
            HG.Configure.Modules.Delete(domain, address, function (res) {
                var deletedidx = -1;
                for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                    var mod = HG.WebApp.Data.Modules[m];
                    if (mod.Domain == domain && mod.Address == address) {
                        deletedidx = m;
                        break;
                    }
                }
                if (deletedidx != -1) {
                    HG.WebApp.Data.Modules.splice(deletedidx, 1);
                }
                HG.WebApp.Maintenance.RefreshModulesList();
                //
                $.mobile.loading('hide');
            });
        });

        $('#systemsettings_databasemaxsize_change').bind('click', function () {
            var sizemb = $('#systemsettings_databasemaxsizechange_size').val();
            $.mobile.loading('show');
            HG.System.SetStatisticsDatabaseMaximumSize(sizemb, function (data) {
                $.mobile.loading('hide');
                setTimeout(function () {
                    HG.WebApp.Maintenance.LoadStatisticsSettings();
                }, 1000);
            });
        });

    });
};

HG.WebApp.Maintenance.RestoreProgramList = '';
HG.WebApp.Maintenance.RestoreProgramToggle = function (el) {
    if (el.checked) {
        HG.WebApp.Maintenance.RestoreProgramList += ',' + el.value + ',';
    }
    else {
        HG.WebApp.Maintenance.RestoreProgramList = HG.WebApp.Maintenance.RestoreProgramList.replace(',' + el.value + ',', '');
    }
};

HG.WebApp.Maintenance.LoadSettings = function () {
    $.mobile.loading('show');
    //
    HG.Configure.System.ServiceCall("HttpService.GetPort", function (data) {
        $('#http_service_port').val(data);
        $('#systemsettings_httpport_text').html(data);
        $.mobile.loading('hide');
    });
    //
    HG.WebApp.Maintenance.LoadStatisticsSettings();
    HG.WebApp.Maintenance.LoadSecuritySettings();
    HG.WebApp.Maintenance.LoadUpdateCheckSettings();
    //
    $('#configure_system_flip_eventshistory').val(dataStore.get('UI.EventsHistory') ? "1" : "0").slider('refresh');
};

HG.WebApp.Maintenance.LoadUpdateCheckSettings = function () {
    $.mobile.loading('show');
    $('#configure_system_updateinstall_button').addClass('ui-disabled');
    HG.System.UpdateManager.GetUpdateList(function (data) {
        var releasedata = eval(data);
        if (releasedata.length == 0) {
            $('#configure_system_updatemanager_info').html('No updates available.');
            $('#configure_system_updatemanager_detailsscroll').hide();
            $('#configure_system_updatemanager_installbutton').hide();
        }
        else {
            $('#configure_system_updatemanager_info').html('The following updates are available:');
            var s = '<pre>';
            for (var r = 0; r < releasedata.length; r++) {
                var relinfo = releasedata[r];
                s += ' * <strong>' + relinfo.Name + ' ' + relinfo.Version + '</strong>\n'
                s += '   <em>' + relinfo.ReleaseNote + '</em>\n'
            }
            s += '</pre>';
            $('#configure_system_updatemanager_details').html(s);
            $('#configure_system_updatemanager_detailsscroll').show();
            $('#configure_system_updatemanager_installbutton').show();
        }
        $.mobile.loading('hide');
    });
};

HG.WebApp.Maintenance.LoadSecuritySettings = function () {
    $.mobile.loading('show');
    HG.System.HasPassword(function (data) {
        var sfx = (data == '1' ? 'on' : 'off');
        $('#securitysettings_password_image').attr('src', 'images/protection-' + sfx + '.png');
        //
        HG.System.LoggingIsEnabled(function (data) {
            $('#configure_system_flip_logging').val(data).slider('refresh');
            $.mobile.loading('hide');
        });
        //
        HG.System.WebCacheIsEnabled(function (data) {
            $('#configure_system_flip_httpcache').val(data).slider('refresh');
            $.mobile.loading('hide');
        });
    });
};

HG.WebApp.Maintenance.LoadStatisticsSettings = function () {
    $.mobile.loading('show');
    HG.Configure.System.ServiceCall("Statistics.GetStatisticsDatabaseMaximumSize", function (data) {
        $('#systemsettings_databasemaxsizechange_size').val(data);
        $('#systemsettings_databasemaxsize_text').html(data);
        $.mobile.loading('hide');
    });
};

HG.WebApp.Maintenance.RefreshModulesList = function () {
    $('#configure_interfaces_modules_list').empty();
    var cdomain = '';
    for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
        var mod = HG.WebApp.Data.Modules[m];
        //
        if (mod.Domain == 'HomeAutomation.HomeGenie.Automation') continue;
        //
        if (cdomain != mod.Domain) {
            $('#configure_interfaces_modules_list').append($('<li/>', { 'data-role': 'list-divider' }).append(mod.Domain));
            cdomain = mod.Domain;
        }
        $('#configure_interfaces_modules_list').append($('<li/>', { 'data-icon': 'minus', 'data-context-domain': mod.Domain, 'data-context-address': mod.Address })
			.append($('<a/>',
				{
				    'text': mod.Address + ' ' + mod.Name
				})));
    }
    cdomain = '';
    for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
        var mod = HG.WebApp.Data.Modules[m];
        //
        if (mod.Domain != 'HomeAutomation.HomeGenie.Automation') continue;
        //
        if (cdomain != mod.Domain) {
            $('#configure_interfaces_modules_list').append($('<li/>', { 'data-role': 'list-divider' }).append(mod.Domain));
            cdomain = mod.Domain;
        }
        $('#configure_interfaces_modules_list').append($('<li/>', { 'data-icon': 'minus', 'data-context-domain': mod.Domain, 'data-context-address': mod.Address })
			.append($('<a/>',
				{
				    'text': mod.Address + ' ' + mod.Name
				})));
    }
    $('#configure_interfaces_modules_list').listview('refresh');
};

HG.WebApp.Maintenance.SetTheme = function (theme) {
    setTheme(theme);
};
