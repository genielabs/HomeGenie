HG.WebApp.Events = HG.WebApp.Events || {};
HG.WebApp.Events.PageId = 'page_events';
HG.WebApp.Events._eventQueueCapacity = 200;
HG.WebApp.Events._ledOffTimeout = null;
HG.WebApp.Events._popupHideTimeout = null;

HG.WebApp.Events.InitializePage = function () {
    var page = $('#'+HG.WebApp.Events.PageId);
    page.on('pagebeforeshow', function (e) {
        HG.WebApp.Events.Refresh();
    });
    page.find('[data-ui-field=property-txt]').change(function () {
        HG.WebApp.Events.Refresh();
    });
    page.find('[data-ui-field=source-txt]').change(function () {
        HG.WebApp.Events.Refresh();
    });
    page.find('[data-ui-field=domain-txt]').change(function () {
        HG.WebApp.Events.Refresh();
    });
    setTimeout(function () {
        HG.WebApp.Events.SetupListener();
    }, 2000);
};

HG.WebApp.Events.SetupListener = function () {
    var es = new EventSource('/api/HomeAutomation.HomeGenie/Logging/RealTime.EventStream/');
    es.onmessage = function (e) {
        var event = eval('[' + e.data + ']')[0];
        //
        if ((event.Domain == 'HomeGenie.System' && event.Property == 'Console.Output') == false) {
            // update event source (the module that is raising this event)
            var module = HG.WebApp.Utility.GetModuleByDomainAddress(event.Domain, event.Source);
            if (module != null) {
                var curprop = HG.WebApp.Utility.GetModulePropertyByName(module, event.Property);
                HG.WebApp.Utility.SetModulePropertyByName(module, event.Property, event.Value, event.Timestamp);
                HG.WebApp.Control.RefreshGroupIndicators();
            }
            // send message to UI for updating UI elements related to this event (widgets, popup and such)
            HG.WebApp.Events.SendEventToUi(module, event);
        }
        //
        if (dataStore.get('UI.EventsHistory')) {
            // add message to local events queue
            HG.WebApp.Data.Events.push(event);
            if (HG.WebApp.Data.Events.length > HG.WebApp.Events._eventQueueCapacity) {
                HG.WebApp.Data.Events.shift();
            }
            // blink hg activity led
            if (HG.WebApp.Events._ledOffTimeout != null) {
                clearTimeout(HG.WebApp.Events._ledOffTimeout);
            }
            $('#event_status_off').hide();
            $('#event_status_on').show();
            HG.WebApp.Events._ledOffTimeout = setTimeout(function () {
                HG.WebApp.Events._ledOffTimeout = null;
                $('#event_status_on').hide();
                $('#event_status_off').show();
            }, 500);
            //
            // refresh events list page if currently open
            if ($.mobile.activePage.attr("id") == HG.WebApp.Events.PageId) {
                HG.WebApp.Events.Refresh();
            }
        }
    }
}

HG.WebApp.Events.Refresh = function () {
    var page = $('#'+HG.WebApp.Events.PageId);
    var rows = '';
    for (var e = HG.WebApp.Data.Events.length - 1; e >= 0; e--) {
        var event = HG.WebApp.Data.Events[e];
        //
        var filterProperty = page.find('[data-ui-field=property-txt]').val();
        var filterSource = page.find('[data-ui-field=source-txt]').val();
        var filterDomain = page.find('[data-ui-field=domain-txt]').val();
        if (filterDomain != '' && event.Domain.indexOf(filterDomain) < 0) continue;
        if (filterSource != '' && event.Source.indexOf(filterSource) < 0) continue;
        if (filterProperty != '' && event.Property.indexOf(filterProperty) < 0) continue;
        //
        var d = new Date(event.UnixTimestamp);
        var longDate = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d, false);
        rows += '<tr>';
        rows += '<td><abbr title="' + longDate + '">' + HG.WebApp.Utility.FormatDateTime(d, true) + '</abbr></td>';
        rows += '<td>' + event.Property + '</td>';
        rows += '<td>' + event.Value + '</td>';
        rows += '<td>' + event.Source + '</td>';
        rows += '<td>' + event.Domain + '</td>';
        rows += '</tr>';
    }
    var eventTable = page.find('[data-ui-field=events-tbl]');
    eventTable.find('tbody').html(rows);
    eventTable.table().table("refresh");
    //{"Timestamp":"2014-04-10T12:55:46.1195672Z","Domain":"HomeAutomation.PhilipsHue","Source":"2","Description":"","Property":"Meter.Watts","Value":"0","UnixTimestamp":1397134546119.5674} ;

};

HG.WebApp.Events.SendEventToUi = function (module, eventLog) {

    // refresh widget associated to the module that raised the event
    if (module != null) {
        HG.WebApp.Control.UpdateModuleWidget(eventLog.Domain, eventLog.Source);
        if ($.mobile.activePage.attr("id") == HG.WebApp.WidgetEditor.PageId) {
            HG.WebApp.WidgetEditor.RenderView(eventLog);
        }
        // when event is an automation program event, we update the whole module
        if (module.Domain == 'HomeAutomation.HomeGenie.Automation' && eventLog.Property != 'Program.Status') {
            HG.Configure.Modules.Get(module.Domain, module.Address, function (data) {
                try {
                    var mod = eval('[' + data + ']')[0];
                    var idx = HG.WebApp.Utility.GetModuleIndexByDomainAddress(mod.Domain, mod.Address);
                    HG.WebApp.Data.Modules[idx] = mod;
                    HG.WebApp.Control.UpdateModuleWidget(mod.Domain, mod.Address);
                } catch (e) { }
            });
        }
    }
    //
    // show event popup as needed
    var logdate = new Date(eventLog.UnixTimestamp);
    var date = HG.WebApp.Utility.FormatDateTime(logdate);
    var popupdata = null;
    switch (eventLog.Domain) {

        case 'HomeGenie.System':
            if (eventLog.Value == 'STARTED') {
                $('#configure_system_updateinstall_status').html('Update install complete. HomeGenie started.');
                setTimeout(function () {
                    document.location.href = '/';
                }, 3000);
            }
            // continue to default 'HomeGenie.*' event processing

        case 'HomeGenie.UpdateChecker':
            if (eventLog.Property == 'InstallProgress.Message') {
                $('#configure_system_updateinstall_log').prepend('*&nbsp;' + eventLog.Value + '<br/>');
            }
            else {
                $('#configure_system_updateinstall_log').prepend('*&nbsp;<strong>' + eventLog.Property + '</strong><br/>&nbsp;&nbsp;' + eventLog.Value + '<br/>');
                var iconImage = HG.Ui.GetModuleIcon(module, null);
                popupdata = {
                    icon: iconImage,
                    title: eventLog.Property + '<br/>' + eventLog.Value,
                    text: '',
                    timestamp: date
                };
            }
            break;

        case 'HomeAutomation.HomeGenie.Automation':
            var iconImage = HG.Ui.GetModuleIcon(module, null);
            if (eventLog.Property == 'Runtime.Error') {
                if (module != null && eventLog.Value != '') {
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">Program ' + module.Address + '</span><br><b>' + eventLog.Value + '</b>',
                        text: 'Runtime<br />Error',
                        timestamp: date
                    };
                    if ($.mobile.activePage.attr("id") == 'page_automation_editprogram') {
                        HG.WebApp.ProgramEdit.RefreshProgramOptions();
                    }
                }
                else if (module != null && HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address) {
                    //var cp = HG.WebApp.Utility.GetProgramByAddress(module.Address);
                    //if (cp != null) cp.ScriptErrors = '';
                    HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = '';
                    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                }
            }
            else if (eventLog.Property == 'Program.Status') {
                if (module != null && HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address) {
                    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                }
            }
            else if (eventLog.Property == 'Program.Notification') {
                var notification = JSON.parse(eventLog.Value);
                popupdata = {
                    icon: iconImage,
                    title: '<span style="color:yellow;font-size:9pt;">' + notification.Title + '</span><br>' + notification.Message,
                    text: '',
                    timestamp: date
                };
            }
            else {
                //var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
                //if (module.Name != '') name = module.Name;
                popupdata = {
                    icon: iconImage,
                    title: '<span style="color:yellow;font-size:9pt;">' + eventLog.Property + '</span><br>' + eventLog.Value,
                    text: '',
                    timestamp: date
                };
            }
            break;

        case 'Protocols.UPnP':
        case 'HomeAutomation.ZWave':
            // events from Z-Wave and UPnP controllers (node 1)
            if (eventLog.Source == '1') {
                var domain = eventLog.Domain.substr(eventLog.Domain.lastIndexOf('.') + 1);
                popupdata = {
                    icon: 'images/genie.png',
                    title: '<span style="color:yellow;font-size:9pt;">' + domain + ' ' + eventLog.Property + '</span><br/>' + eventLog.Value,
                    text: '',
                    timestamp: date
                };
                HG.WebApp.SystemSettings.Interfaces['HomeAutomation.ZWave'].DiscoveryLog.prepend('*&nbsp;' + eventLog.Value + '<br/>');
                break;
            }
            // continue to default processing

        default:
            if (module != null && eventLog.Property != 'Meter.Watts') {

                var iconImage = HG.Ui.GetModuleIcon(module, null);
                if ((module.Address == 'RF' || module.Address == 'IR') && eventLog.Value != '') {
                    iconImage = 'images/remote.png';
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">' + module.Domain.substring(module.Domain.indexOf('.') + 1) + '</span><br/>' + eventLog.Value,
                        text: module.Address,
                        timestamp: date
                    };
                }
                else if (eventLog.Property.substring(0, 7) == 'Sensor.') {
                    var group = HG.WebApp.GroupsList.GetModuleGroup(module);
                    if (group != null) group = group.Name;
                    var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
                    var propname = eventLog.Property.substring(eventLog.Property.indexOf('.') + 1);
                    //
                    switch (propname) {
                        case 'Temperature':
                            iconImage = 'pages/control/widgets/homegenie/generic/images/temperature.png';
                            if (HG.WebApp.Locales.GetDateEndianType() == 'M') {
                                // convert to Fahrenheit
                                var temperature = Math.round(eventLog.Value.replace(',', '.') * 100) / 100;
                                temperature = (temperature * 1.8) + 32;
                                eventLog.Value = temperature.toString();
                            }
                            break;
                        case 'Luminance':
                            iconImage = 'pages/control/widgets/homegenie/generic/images/luminance.png';
                            break;
                        default:
                            iconImage = 'pages/control/widgets/homegenie/generic/images/sensor.png';
                            break;
                    }
                    //
                    if (module.Name != '') name = module.Name;
                    if (group == null) group = '';
                    //
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">' + group + '</span><br><b>' + name + '</b><br>' + propname,
                        text: parseFloat(eventLog.Value.replace(',', '.')).toFixed(2),
                        timestamp: date
                    };
                }
                else if (eventLog.Property.substring(0, 7) == 'Status.') {
                    var group = HG.WebApp.GroupsList.GetModuleGroup(module);
                    if (group != null) group = group.Name;
                    var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
                    var propname = eventLog.Property.substring(eventLog.Property.indexOf('.') + 1);
                    var value = eventLog.Value;
                    if (!isNaN(eventLog.Value.replace(',', '.')))
                        value = (parseFloat(eventLog.Value.replace(',', '.')).toFixed(2));
                    //
                    if (module.Name != '') name = module.Name;
                    if (group == null) group = '';
                    //
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">' + group + '</span><br><b>' + name + '</b>',
                        text: propname + '<br />' + value,
                        timestamp: date
                    };
                }
                //
                // send this to wizard script event capture
                if (HG.WebApp.ProgramEdit._IsCapturingConditions && eventLog.Value != '') {
                    var conditionobj = {
                        'Domain': module.Domain,
                        'Target': module.Address,
                        'Property': eventLog.Property,
                        'ComparisonOperator': 'Equals',
                        'ComparisonValue': eventLog.Value
                    };
                    HG.WebApp.ProgramEdit._CurrentProgram.Conditions.push(conditionobj);
                    automationpage_ConditionsRefresh();
                }
                else if (HG.WebApp.ProgramEdit._IsCapturingCommands && eventLog.Value != '') {
                    var command = HG.WebApp.Utility.GetCommandFromEvent(module, eventLog);
                    if (command != null) {
                        HG.WebApp.ProgramEdit._CurrentProgram.Commands.push(command);
                        automationpage_CommandsRefresh();
                    }
                }
            }
            /*
            // TODO: DEPREACATE THIS?
            else {
                if (eventLog.Domain == 'Protocols.AirPlay' && eventLog.Property == 'PlayControl.DisplayImage') {
                    var logdate = new Date(eventLog.UnixTimestamp);
                    var date = HG.WebApp.Utility.FormatDateTime(logdate);

                    s += '<table width="100%"><tr><td width="48" rowspan="2">';
                    s += '<a _href="#dialog_netplay_show_popup" -data-rel="popup"><img src="images/playcontrol.png" width="48" height="48"></a>';
                    s += '</td><td valign="top" align="left">';
                    s += '<span style="color:gray;font-size:8pt;">AirPlay Service</span><br><b>Remote image display reuqest</b>';
                    s += '</td><td align="right" style="color:lime;font-size:12pt">    </td></tr>';
                    s += '<tr><td colspan="2" align="right"><span style="color:gray;font-size:8pt;">' + date + '</span>';
                    s += '</td></tr></table>';

                    var displayid = eventLog.Value;
                    var cts = eventLog.UnixTimestamp;

                    HG.WebApp.Apps.NetPlay.SlideShow.DisplayImage(displayid, cts);

                }
            }*/
            break;
    }
    //
    if (popupdata != null && !HG.WebApp.Events.PopupHasIgnore(eventLog.Domain, eventLog.Source, eventLog.Property)) {

        HG.WebApp.Events.ShowEventPopup(popupdata, eventLog);

    }

};

HG.WebApp.Events.PopupRefreshIgnore = function () {
    var ignoreList = dataStore.get('UI.EventPopupIgnore');
    if (ignoreList == null) return;
    $('#popupsettings_ignorelist li:gt(0)').remove();
    for (var i = 0; i < ignoreList.length; i++)
    {
        var item = '<li data-icon="minus">';
        item += '    <a href="#" class="ui-grid-b" onclick="HG.WebApp.Events.PopupRemoveIgnore(' + i + ')">';
        item += '        <div class="ui-block-a hg-label" style="width:40%">' + ignoreList[i].Domain + '</div>';
        item += '        <div class="ui-block-b hg-label" style="width:20%" align="center">' + ignoreList[i].Address + '</div>';
        item += '        <div class="ui-block-c hg-label" style="width:40%">' + ignoreList[i].Property + '</div>';
        item += '    </a>';
        item += '</li>';
        $('#popupsettings_ignorelist').append(item);
    }
    $('#popupsettings_ignorelist').listview().trigger('create');
    $('#popupsettings_ignorelist').listview('refresh');
}

HG.WebApp.Events.PopupHasIgnore = function (domain, address, property) {
    var ignoreList = dataStore.get('UI.EventPopupIgnore');
    if (ignoreList == null) return false;
    var exists = false;
    for(var i = 0; i < ignoreList.length; i++)
    {
        if (ignoreList[i].Domain == domain && ignoreList[i].Address == address && ignoreList[i].Property == property)
        {
            exists = true;
            break;
        }
    }
    return exists;
};

HG.WebApp.Events.PopupRemoveIgnore = function (index) {
    var ignoreList = dataStore.get('UI.EventPopupIgnore');
    if (ignoreList == null || ignoreList.length <= index) return;
    ignoreList.splice(index, 1);
    HG.WebApp.Events.PopupRefreshIgnore();
};

HG.WebApp.Events.PopupAddIgnore = function (domain, address, property) {
    var ignoreList = dataStore.get('UI.EventPopupIgnore');
    if (ignoreList == null)
    {
        ignoreList = Array();
    }
    if (!HG.WebApp.Events.PopupHasIgnore(domain, address, property))
    {
        ignoreList.push({ Domain: domain, Address: address, Property: property });
        dataStore.set('UI.EventPopupIgnore', ignoreList);
    }
    $('#statuspopup').css('display', 'none');
};

HG.WebApp.Events.ShowEventPopup = function (popupdata, eventLog) {

    var s = '<table width="100%"><tr><td width="42" valign="top">';
    s += '<img src="' + popupdata.icon + '" width="42">';
    s += '</td><td valign="top" align="left">';
    s += popupdata.title;
    s += '</td><td align="right" style="color:lime;font-size:12pt">' + popupdata.text + '</td></tr>';
    s += '<tr style="color:gray;font-size:9pt;"><td colspan="2">' + popupdata.timestamp + '</td>';
    if (eventLog)
    {
        s += '<td align="right"><a href="#" title="Block popup from this source" onclick="HG.WebApp.Events.PopupAddIgnore(\'' + eventLog.Domain + '\',\'' + eventLog.Source + '\',\'' + eventLog.Property + '\')"><img border="0" alt="Block popup from this source" src="images/halt.png" /></a></td>';
    }
    s += '</tr>';
    s += '</table>';
    $('#statuspopup').html(s);

    // hide timeout
    if (HG.WebApp.Events._popupHideTimeout != null) {
        clearTimeout(HG.WebApp.Events._popupHideTimeout);
    }
    else {
        $('#statuspopup').css('display', '');
        $('#statuspopup').animate({ opacity: '0' }, 0, function () {
            $('#statuspopup').animate({ right: '5px', opacity: '0.90' }, 300);
        });
    }
    HG.WebApp.Events._popupHideTimeout = setTimeout(function () {
        if ($('#statuspopup').css('display') != 'none') {
            $('#statuspopup').animate({ right: '-300px', opacity: '0.0' }, 300, function () {
                $('#statuspopup').css('display', 'none');
            });
        }
        HG.WebApp.Events._popupHideTimeout = null;
    }, 5000);

}