//
// namespace : HG.WebApp.Maintenance namespace
// info      : HomeAutomation.HomeGenie / Config / System.Configure / System.ConfigurationBackup
//
HG.WebApp.Maintenance = HG.WebApp.Maintenance || new function () { var $$ = this;

    $$.PageId = 'page_configure_maintenance';
    $$.RestoreProgramList = '';

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        var locationPopup = page.find('[id=maintenance_configuration_locationpopup]');
        var locationName = page.find('[data-ui-field=location-name]');
        $$.locationLat = page.find('[data-ui-field="location-latitude"]').on('change', function(){ $$.Location.name = ''; $$.SetLocation(); });
        $$.locationLon = page.find('[data-ui-field="location-longitude"]').on('change', function(){ $$.Location.name = ''; $$.SetLocation(); });
        page.on('pagebeforeshow', function (e) {
            $('#restore_configuration_uploadfile').val('')
        });
        page.on('pageshow', function (e) {
            var messageText = '';
            if ($('#interfaces_status').data('update_available')) {
                messageText = HG.WebApp.Locales.GetLocaleString('configure_system_installupdate_description',
                    'Click here to install the latest update.');
            } else {
                messageText = HG.WebApp.Locales.GetLocaleString('configure_system_installpackage_description',
                    'Click here to install additional features.');
            }
            page.find('[id=configure_system_updatemanager_installbutton]').qtip({
                content: {
                    text: messageText,
                },
                show: {event: false, ready: true, delay: 1000},
                events: {
                    hide: function () {
                        $(this).qtip('destroy');
                    }
                },
                hide: {event: false, inactive: 3000},
                style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                position: {my: 'right center', at: 'left center'}
            });
        });
        page.on('pageinit', function (e) {
            locationPopup.popup().on('popupbeforeposition',function(){
                if (locationPopup.initialized) {
                    page.find('[data-ui-field="location-picker"]').locationpicker('location', { latitude: $$.Location.latitude, longitude: $$.Location.longitude });
                } else {
                    $.cachedScript('http://maps.google.com/maps/api/js?k'+'ey='+'AIzaSyCSS'+'Msdcyihg'+'UsHWYCwGcGXBS'+'Nu1kWgCGQ'+'&sensor=false&libraries=places').done(function(){
                        $.getScript('js/locationpicker.jquery.js').done(function(){
                            locationPopup.initialized = true;
                            page.find('[data-ui-field="location-picker"]').locationpicker({
                                location: { latitude: $$.Location.latitude, longitude: $$.Location.longitude },   
                                radius: 0,
                                inputBinding: {
                                    locationNameInput: locationName       
                                },
                                enableAutocomplete: true,
                                onchanged: function(currentLocation, radius, isMarkerDropped) {
                                    $$.Location.name = locationName.val();
                                    $$.locationLat.val(currentLocation.latitude);
                                    $$.locationLon.val(currentLocation.longitude);
                                    $$.SetLocation();
                                }           
                            });
                        });
                    });
                }
            });
            $('#systemsettings_httpport_change').bind('click', function () {
                var port = $('#http_service_port').val();
                $.mobile.loading('show');
                HG.System.SetHttpPort(port, function (data) {
                    $('#systemsettings_httpport_text').html(port);
                    $.mobile.loading('hide');
                });
            });

            $('#systemsettings_httphostheader_change').bind('click', function () {
                var host = $('#http_host_header').val();
                $.mobile.loading('show');
                HG.System.SetHostHeader(host, function (data) {
                    $('#systemsettings_hostheader_text').html(host);
                    $.mobile.loading('hide');
                });
            });

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
                    HG.WebApp.Store.set('UI.EventsHistory', true);
                    $('#btn_eventshistory_led').show();
                }
                else {
                    HG.WebApp.Store.set('UI.EventsHistory', false);
                    $('#btn_eventshistory_led').hide();
                }
            });
            //
            $("input[name='tempunit-choice']").on('change', function () {
                HG.WebApp.Store.set('UI.TemperatureUnit', $(this).val());
            });
            $("input[name='dateformat-choice']").on('change', function () {
                HG.WebApp.Store.set('UI.DateFormat', $(this).val());
            });
            //
            $('#configure_system_updatemanager_updatebutton').bind('click', function () {
                $('#configure_system_updatemanager_info').html('<strong>Checking for updates...</strong>');
                $('#configure_system_updatemanager_detailsscroll').slideUp(300);
                $('#configure_system_updatemanager_installbutton').hide();
                $.mobile.loading('show');
                HG.System.UpdateManager.UpdateCheck(function (res) {
                    $.mobile.loading('hide');
                    if (res.ResponseValue == 'OK')
                        $$.LoadUpdateCheckSettings();
                    else
                        $('#configure_system_updatemanager_info').html(HG.WebApp.Locales.GetLocaleString('configure_system_updatemanager_connection_error', 'Connection error!'));
                });
            });
            $('#configure_system_updatemanager_manualbutton').on('click', function () {
                //$('#configure_system_updatemanager_info').html('<strong>Updating from file...</strong>');
                if ($('#updatemanager_updatefile_uploadfile').val() == "") {
                    alert('Select a .tgz release file to install first');
                    $('#updatemanager_updatefile_uploadfile').parent().stop().animate({borderColor: "#FF5050"}, 250)
                        .animate({borderColor: "#FFFFFF"}, 250)
                        .animate({borderColor: "#FF5050"}, 250)
                        .animate({borderColor: "#FFFFFF"}, 250);
                } else {
                    $.mobile.loading('show');
                    $('#configure_system_updatemanager_manualbutton').addClass('ui-disabled');
                    $.mobile.loading('show', {
                        text: 'Processing release file, please wait...',
                        textVisible: true,
                        theme: 'a',
                        html: ''
                    });
                    $('#configure_system_updateinstall_log').html();
                    $('#systemsettings_updateinstall_popup').popup('open');
                    $('#updatemanager_updatefile_form').submit();
                }
            });
            $('#updatemanager_updatefile_uploadframe').bind('load', function (evt) {
                if ($('#updatemanager_updatefile_uploadfile').val() == "")
                    return;
                $('#configure_system_updatemanager_manualbutton').removeClass('ui-disabled');
                $.mobile.loading('hide');
                $('#systemsettings_updateinstall_popup').popup('close');
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
                    // TODO: ....
                    if (res.ResponseValue == 'OK') {
                        $('#configure_system_updateinstall_status').html('Update install complete.');
                        setTimeout(function () {
                            window.location.replace("/");
                        }, 3000);
                    } else if (res.ResponseValue == 'RESTART') {
                        $('#configure_system_updateinstall_status').html('Installing files... (HomeGenie service stopped)');
                    } else if (res.ResponseValue == 'ERROR') {
                        $('#configure_system_updateinstall_status').html('Error during installation progress.');
                        //
                        $.mobile.loading('hide');
                        $$.LoadUpdateCheckSettings();
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
                HG.Ui.SwitchPopup('#systemsettings_updatewarning_popup', '#systemsettings_updateinstall_popup', true)
                $('#configure_system_updateinstall_status').html('Downloading files...');
                $.mobile.loading('show', {
                    text: 'Please wait...',
                    textVisible: true,
                    html: ""
                });
                HG.System.UpdateManager.DownloadUpdate(function (res) {
                    $.mobile.loading('hide');
                    $$.LoadUpdateCheckSettings();
                    // TODO: ....
                    if (res.ResponseValue == 'OK') {
                        $('#configure_system_updateinstall_status').html('Update files ready.');
                        $('#configure_system_updateinstall_button').removeClass('ui-disabled');
                    } else if (res.ResponseValue == 'RESTART') {
                        $('#configure_system_updateinstall_status').html('Update files ready. HomeGenie will be restarted after updating.');
                        $('#configure_system_updateinstall_button').removeClass('ui-disabled');
                    } else if (res.ResponseValue == 'ERROR') {
                        $('#configure_system_updateinstall_status').html('Error while downloading update files.');
                        $('#configure_system_updateinstall_button').addClass('ui-disabled');
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
                        $$.LoadHttpSettings();
                    }, 1000);
                });
            });
            $('#securitysettings_password_clear').bind('click', function () {
                $.mobile.loading('show');
                HG.System.ClearPassword(function (data) {
                    $.mobile.loading('hide');
                    $$.LoadHttpSettings();
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
                $.mobile.loading('show', {
                    text: 'Restarting service, please wait...',
                    textVisible: true,
                    theme: 'a',
                    html: ''
                });
                // SYSTEM RESTART....
                HG.Configure.System.ServiceCall("Service.Restart", function (data) {
                });
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
                    $('#restore_configuration_uploadfile').parent().stop().animate({borderColor: "#FF5050"}, 250)
                        .animate({borderColor: "#FFFFFF"}, 250)
                        .animate({borderColor: "#FF5050"}, 250)
                        .animate({borderColor: "#FFFFFF"}, 250);
                } else {
                    $('#systemsettings_backuprestores1cancelbutton').removeClass('ui-disabled');
                    $('#systemsettings_backuprestores1confirmbutton').removeClass('ui-disabled');
                    $.mobile.loading('show', {
                        text: 'Restoring backup, please wait...',
                        textVisible: true,
                        theme: 'a',
                        html: ''
                    });
                    $('#restore_configuration_form').submit();
                }
            });
            $('#maintenance_configuration_factoryresetbutton').bind('click', function () {
                $.mobile.loading('show', {
                    text: 'Resetting to factory defaults, please wait...',
                    textVisible: true,
                    theme: 'a',
                    html: ''
                });
                // FACTORY RESET....
                HG.Configure.System.ServiceCall("System.ConfigurationReset", function (data) {
                    setTimeout(function () {
                        $.mobile.loading('hide');
                        window.location.replace("/");
                    }, 3000);
                });
            });
            $('#systemsettings_backuprestores1selectallbtn').bind('click', function () {
                if ($$.RestoreProgramList != '') {
                    $$.RestoreProgramList = '';
                    $('#systemsettings_backuprestores1plist div').attr('data-checked', 'false')
                        .find('i').addClass('fa-square-o')
                        .removeClass('fa-check-square');
                } else {
                    $$.RestoreProgramList = '';
                    $('#systemsettings_backuprestores1plist div').each(function () {
                        $(this).attr('data-checked', 'true')
                            .find('i').removeClass('fa-square-o')
                            .addClass('fa-check-square');
                        $$.RestoreProgramList += ',' + $(this).attr('data-program') + ',';
                    });
                }
            });
            $('#maintenance_configuration_backupbutton').bind('click', function () {
                window.open(location.protocol + '../../api/HomeAutomation.HomeGenie/Config/System.Configure/System.ConfigurationBackup');
            });
            $('#restore_configuration_uploadframe').bind('load', function (evt) {
                if ($('#restore_configuration_uploadfile').val() == "")
                    return;
                HG.Configure.System.ServiceCall("System.ConfigurationRestoreS1", function (data) {
                    $.mobile.loading('hide');
                    $('#systemsettings_backuprestores1plist').empty();
                    var programs = data;
                    if (programs.length == 0) {
                        $('#systemsettings_backuprestores1plist').append("<p>No user's program found in this backup.</p>");
                        $('#systemsettings_backuprestores1selectallbtn').addClass('ui-disabled');
                    } else {
                        for (var p = 0; p < programs.length; p++) {
                            var pli = $('<div data-checked="false" data-program="' + programs[p].Address + '" style="padding:4px;cursor:pointer"><i class="fa fa-square-o fa-lg" style="width:20px"></i> <span style="font-size:12pt">' + programs[p].Address + ' ' + programs[p].Name + '</label></span>');
                            pli.on('click', function () {
                                if ($(this).attr('data-checked') == 'true') {
                                    $(this).find('i').addClass('fa-square-o');
                                    $(this).find('i').removeClass('fa-check-square');
                                    $(this).attr('data-checked', 'false');
                                    $$.RestoreProgramList = $$.RestoreProgramList.replace(',' + $(this).attr('data-program') + ',', '');
                                } else {
                                    $(this).find('i').removeClass('fa-square-o');
                                    $(this).find('i').addClass('fa-check-square');
                                    $(this).attr('data-checked', 'true');
                                    $$.RestoreProgramList += ',' + $(this).attr('data-program') + ',';
                                }
                            });
                            $('#systemsettings_backuprestores1plist').append(pli);
                        }
                        $('#systemsettings_backuprestores1selectallbtn').removeClass('ui-disabled');
                    }
                    $('#systemsettings_backuprestores1plist').scrollTop(0);
                    $('#systemsettings_backuprestores1').find('[data-ui-field=restore_log]').empty();
                    $('#systemsettings_backuprestores1').find('[data-ui-field=restore_log]').hide();
                    $('#systemsettings_backuprestores1').find('[data-ui-field=user-programs]').show();
                    $('#systemsettings_backuprestores1').popup('open');
                });
                $('#systemsettings_backuprestores1confirmbutton').bind('click', function () {
                    $('#systemsettings_backuprestores1cancelbutton').addClass('ui-disabled');
                    $('#systemsettings_backuprestores1confirmbutton').addClass('ui-disabled');
                    $('#systemsettings_backuprestores1').find('[data-ui-field=restore_log]').show();
                    $('#systemsettings_backuprestores1').find('[data-ui-field=user-programs]').hide();
                    $.mobile.loading('show', {
                        text: 'Please be patient, this may take some time...',
                        textVisible: true,
                        theme: 'a',
                        html: ''
                    });
                    HG.Configure.System.ServiceCall("System.ConfigurationRestoreS2/" + $$.RestoreProgramList, function (data) {
                        $.mobile.loading('hide');
                        window.location.replace("/");
                    });
                });
            });
            //
            $('#systemsettings_modulesdelete').on('popupbeforeposition', function (event) {
                $$.RefreshModulesList();
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
                    $$.RefreshModulesList();
                    //
                    $.mobile.loading('hide');
                });
            });

            $('#systemsettings_databasemaxsize_change').bind('click', function () {
                var sizemb = $('#systemsettings_databasemaxsizechange_size').val();
                $.mobile.loading('show');
                HG.Statistics.SetStatisticsDatabaseMaximumSize(sizemb, function (data) {
                    $.mobile.loading('hide');
                    setTimeout(function () {
                        $$.LoadStatisticsSettings();
                    }, 1000);
                });
            });

            $('#maintenance_configuration_routingresetbutton').on('click', function () {
                var message = HG.WebApp.Locales.GetLocaleString('systemsettings_resetroutingdata_warning', 'This operation cannot be undone.');
                HG.WebApp.Utility.ConfirmPopup(HG.WebApp.Locales.GetLocaleString('systemsettings_resetroutingdata_title', 'Reset routing data?'), message, function (confirmed) {
                    if (confirmed) HG.Configure.Modules.RoutingReset();
                });
            });

            $('#maintenance_configuration_databaseresetbutton').on('click', function () {
                var message = HG.WebApp.Locales.GetLocaleString('systemsettings_resetdatabase_warning', 'This operation cannot be undone.');
                HG.WebApp.Utility.ConfirmPopup(HG.WebApp.Locales.GetLocaleString('systemsettings_resetdatabase_title', 'Reset stats database?'), message, function (confirmed) {
                    if (confirmed) HG.Statistics.DatabaseReset();
                });
            });

            // Add-ons repository browser
            page.find('[id=systemsettings_browserepo]').on('popupbeforeposition', function (event) {
                var availHeight = $(window).height() - 320;
                var availWidth = $(window).width();
                availWidth = availWidth - (availWidth / 5);
                var contentFiles = page.find('[data-ui-field=browser_files]');
                if (availWidth > 500) {
                    contentFiles.parent().parent().css('width', availWidth);
                    contentFiles.parent().parent().css('max-width', availWidth);
                }
                contentFiles.css('height', availHeight / 2);
                contentFiles.css('max-height', availHeight / 2);
                var contentText = page.find('[data-ui-field=browser_text]');
                contentText.css('height', availHeight / 2);
                contentText.css('max-height', availHeight / 2);
            }).on('popupafteropen', function (event) {
                $$.BrowseRoot();
            });
            page.find('[data-ui-field=parent_folder]').on('click', function () {
                var browse = $$.BrowseRepository;
                var history = $$._BrowserHistory;
                if (history.length > 1) {
                    history.pop();
                    browse(history.pop());
                }
            });
            page.find('[data-ui-field=install_package]').on('click', function () {
                page.find('[data-ui-field=install_text]').empty();
                page.find('[data-ui-field=install_package]').addClass('ui-disabled');
                $.mobile.loading('show');
                $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Package.Install/' + encodeURIComponent($$._BrowserPackage), function (res) {
                    page.find('[data-ui-field=install_package]').removeClass('ui-disabled');
                    $.mobile.loading('hide');
                });
            });
            page.find('[data-ui-field=repository_browse]').on('click', function () {
                $$.BrowseRoot();
            });
        });
    };

    $$._BrowserHistory = [];
    $$._BrowserPackage = '';
    $$.BrowseRoot = function () {
        // reset history and browse to new repo
        $$._BrowserHistory = [];
        var page = $('#' + $$.PageId);
        $$.BrowseRepository({url: page.find('[data-ui-field=repository_url]').val() + '/packages', name: ''});
    };
    $$.BrowseRepository = function (path) {
        var page = $('#' + $$.PageId);
        var list = page.find('[data-ui-field=repository_files]');
        var text = page.find('[data-ui-field=description_text]');
        var info = page.find('[data-ui-field=package_info]');
        var popup = page.find('[id=systemsettings_browserepo]');
        var browse = $$.BrowseRepository;
        var history = $$._BrowserHistory;
        history.push(path);
        // show/hide ui elements as needed
        if (history.length > 1)
            page.find('[data-ui-field=parent_folder]').show();
        else
            page.find('[data-ui-field=parent_folder]').hide();
        page.find('[data-ui-field=install_package]').hide();
        page.find('[data-ui-field=install_text]').empty();
        text.html('');
        info.hide();
        // show current path
        var cpath = '';
        $.each(history, function (idx, p) {
            cpath += '<strong>' + p.name + '</strong> / ';
        });
        page.find('[data-ui-field=browser_path]').html(cpath);
        // get current folder files
        list.empty();
        $.mobile.loading('show');
        $.get(path.url, function (data) {
            $.each(data, function (idx, file) {
                if (file.type == 'dir') {
                    var item = $('<li><a href="#" class="ui-btn ui-btn-icon-left ui-icon-fa-folder">' + file.name + '</a></li>');
                    list.append(item);
                    item.on('click', function () {
                        browse({url: file.url, name: file.name});
                    });
                } else if (file.name.toLowerCase() == 'package.json') {
                    $$._BrowserPackage = file.download_url.substring(0, file.download_url.lastIndexOf('/'));
                    $.get(file.download_url, function (package) {
                        package = $.parseJSON(package);
                        var pkginfo = '<strong>' + package.title + '</strong>';
                        pkginfo += '<br/><strong>Version</strong>: ' + package.version;
                        pkginfo += '<br/><strong>Author</strong>: ' + package.author;
                        pkginfo += '<br/><strong>Files</strong>: '
                        pkginfo += package.programs.length + ' automation programs, ';
                        pkginfo += package.widgets.length + ' ui widgets, ';
                        pkginfo += package.interfaces.length + ' mig interfaces';
                        pkginfo += '<br/><strong>Published</strong>: ' + package.published;
                        if (typeof package.sourcecode != 'undefined' && package.sourcecode != "")
                            pkginfo += '<br/><i class="fa fa-file-code-o fa-md"></i> <a href="' + package.sourcecode + '" target="_blank">Source Code</a>';
                        if (typeof package.homepage != 'undefined' && package.homepage != "")
                            pkginfo += '<br/><i class="fa fa-comments fa-md"></i> <a href="' + package.homepage + '" target="_blank">Forum Thread</a>';
                        info.html(pkginfo + '<br/>');
                        info.show();
                        page.find('[data-ui-field=browser_text]').scrollTop(0);
                        // check if already installed
                        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Package.Get/' + encodeURIComponent($$._BrowserPackage), function (package) {
                            if (typeof package.install_date != 'undefined') {
                                info.append('<br/><strong>Installed</strong>: ' + package.install_date);
                            }
                        });
                        // Automation Programs
                        $.each(package.programs, function (idx, f) {
                            var item = $('<li><a href="#" class="ui-btn ui-btn-icon-left ui-icon-fa-file-code-o">' + f.description + ' (' + f.file + ')</a></li>');
                            list.append(item);
                        });
                        // Widgets
                        $.each(package.widgets, function (idx, f) {
                            var item = $('<li><a href="#" class="ui-btn ui-btn-icon-left ui-icon-fa-cube">' + f.description + ' (' + f.file + ')</a></li>');
                            list.append(item);
                        });
                        // MIG Interfaces
                        $.each(package.interfaces, function (idx, f) {
                            var item = $('<li><a href="#" class="ui-btn ui-btn-icon-left ui-icon-fa-wrench">' + f.description + ' (' + f.file + ')</a></li>');
                            list.append(item);
                        });
                    });
                    page.find('[data-ui-field=install_package]').show();
                } else if (file.name.toLowerCase().endsWith('.md')) {
                    $.get(file.download_url, function (readme) {
                        text.html(marked(readme));
                        page.find('[data-ui-field=browser_text]').scrollTop(0);
                    });
                } else if (file.type == 'file') {
                    //var item = $('<li><a href="#" class="ui-btn ui-btn-icon-left ui-icon-fa-file-code-o">'+file.name+'</a></li>');
                    //list.append(item);
                    //item.on('click', function() {
                    //  //console.log(file);
                    //});
                }
            });
            list.listview().listview('refresh');
            setTimeout(function () {
                popup.popup().popup('reposition', {positionTo: 'window'});
                $.mobile.loading('hide');
            }, 300);
        });
    };

    $$.SetLocation = function(){
        var lat = parseFloat($$.locationLat.val());
        var lon = parseFloat($$.locationLon.val());
        if (lat != null) {
            $$.Location.latitude = lat;
            $$.locationLat.val(lat);
        }
        if (lon != null) {
            $$.Location.longitude = lon;
            $$.locationLon.val(lon);
        }
        HG.System.LocationSet($$.Location);
    };

    $$.LoadSettings = function () {
        $.mobile.loading('show');
        //
        $$.LoadUpdateCheckSettings();
        HG.System.LocationGet(function(data) {
            if (typeof data.latitude != 'undefined')
                $$.Location = data;
            else
                $$.Location = { name: 'Rome, RM, Italia', latitude: 41.90278349999999, longitude: 12.496365500000024 };
            $$.locationLat.val($$.Location.latitude);
            $$.locationLon.val($$.Location.longitude);
        });
        HG.System.LoggingIsEnabled(function (data) {
            $('#configure_system_flip_logging').val(data).slider('refresh');
            $.mobile.loading('hide');
        });
        $$.LoadHttpSettings();
        $$.LoadStatisticsSettings();
        //
        $('#configure_system_flip_eventshistory').val(HG.WebApp.Store.get('UI.EventsHistory') ? "1" : "0").slider('refresh');
        //
        var temperatureUnit = HG.WebApp.Store.get('UI.TemperatureUnit');
        $('#tempunit-celsius').prop("checked", false);
        $('#tempunit-fahrenheit').prop('checked', false);
        if (temperatureUnit != 'F')
            $('#tempunit-celsius').prop('checked', true);
        else
            $('#tempunit-fahrenheit').prop('checked', true);
        $("input[name='tempunit-choice']").checkboxradio('refresh');
        //
        var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
        $('#dateformat-dmy').prop("checked", false);
        $('#dateformat-mdy').prop('checked', false);
        if (dateFormat != 'MDY12')
            $('#dateformat-dmy').prop('checked', true);
        else
            $('#dateformat-mdy').prop('checked', true);
        $("input[name='dateformat-choice']").checkboxradio('refresh');
    };

    $$.LoadUpdateCheckSettings = function () {
        $.mobile.loading('show');
        $('#configure_system_updateinstall_button').addClass('ui-disabled');
        $('#configure_system_updatemanager_detailsscroll').slideUp(300);
        $('#configure_system_updatemanager_installbutton').hide();
        HG.System.UpdateManager.GetUpdateList(function (releasedata) {
            if (releasedata.ResponseValue == 'ERROR') {
                $('#configure_system_updatemanager_info').html(HG.WebApp.Locales.GetLocaleString('configure_system_updatemanager_connection_error', 'Connection error!'));
            } else if (releasedata.length == 0) {
                $('#configure_system_updatemanager_info').html(HG.WebApp.Locales.GetLocaleString('configure_system_updatemanager_no_updates'));
            } else {
                $('#configure_system_updatemanager_info').html(HG.WebApp.Locales.GetLocaleString('configure_system_updatemanager_updates_available'));
                var s = '<pre>';
                for (var r = 0; r < releasedata.length; r++) {
                    var relinfo = releasedata[r];
                    s += '<strong>' + relinfo.Name + ' ' + relinfo.Version + ' ' + relinfo.ReleaseDate + '</strong>\n'
                    s += '<em>' + relinfo.ReleaseNote + '</em>\n'
                }
                s += '</pre>';
                $('#configure_system_updatemanager_details').html(s);
                $('#configure_system_updatemanager_detailsscroll').slideDown(300);
                $('#configure_system_updatemanager_installbutton').show();
            }
            $.mobile.loading('hide');
        });
    };

    $$.LoadHttpSettings = function () {
        $.mobile.loading('show');
        HG.System.HasPassword(function (data) {
            var sfx = (data.ResponseValue == '1' ? 'on' : 'off');
            $('#securitysettings_password_image').attr('src', 'images/protection-' + sfx + '.png');
            //
            HG.System.WebCacheIsEnabled(function (data) {
                $('#configure_system_flip_httpcache').val(data == 'true' ? '1' : '0').slider('refresh');
                $.mobile.loading('hide');
            });
            //
            HG.Configure.System.ServiceCall("HttpService.GetPort", function (data) {
                $('#http_service_port').val(data);
                $('#systemsettings_httpport_text').html(data);
                $.mobile.loading('hide');
            });
            HG.Configure.System.ServiceCall("HttpService.GetHostHeader", function (data) {
                $('#http_host_header').val(data);
                $('#systemsettings_hostheader_text').html(data);
                $.mobile.loading('hide');
            });
        });
    };

    $$.LoadStatisticsSettings = function () {
        $.mobile.loading('show');
        HG.Configure.System.ServiceCall("Statistics.GetStatisticsDatabaseMaximumSize", function (data) {
            $('#systemsettings_databasemaxsizechange_size').val(data);
            $('#systemsettings_databasemaxsize_text').html(data);
            $.mobile.loading('hide');
        });
    };

    $$.RefreshModulesList = function () {
        $('#configure_interfaces_modules_list').empty();
        var cdomain = '';
        for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
            var mod = HG.WebApp.Data.Modules[m];
            //
            if (mod.Domain == 'HomeAutomation.HomeGenie.Automation') continue;
            //
            if (cdomain != mod.Domain) {
                $('#configure_interfaces_modules_list').append($('<li/>', {'data-role': 'list-divider'}).append(mod.Domain));
                cdomain = mod.Domain;
            }
            $('#configure_interfaces_modules_list').append($('<li/>', {
                'data-icon': 'minus',
                'data-context-domain': mod.Domain,
                'data-context-address': mod.Address
            })
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
                $('#configure_interfaces_modules_list').append($('<li/>', {'data-role': 'list-divider'}).append(mod.Domain));
                cdomain = mod.Domain;
            }
            $('#configure_interfaces_modules_list').append($('<li/>', {
                'data-icon': 'minus',
                'data-context-domain': mod.Domain,
                'data-context-address': mod.Address
            })
                .append($('<a/>',
                    {
                        'text': mod.Address + ' ' + mod.Name
                    })));
        }
        $('#configure_interfaces_modules_list').listview('refresh');
    };

};