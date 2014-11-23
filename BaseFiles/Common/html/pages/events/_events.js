HG.WebApp.Events = HG.WebApp.Events || {};
HG.WebApp.Events._eventQueueCapacity = 200;
HG.WebApp.Events._ledOffTimeout = null;
HG.WebApp.Events._popupHideTimeout = null;

HG.WebApp.Events.InitializePage = function () {

    $('#events_filter_property').change(function () {
        HG.WebApp.Events.Refresh();
    });
    $('#events_filter_source').change(function () {
        HG.WebApp.Events.Refresh();
    });
    $('#events_filter_domain').change(function () {
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
        // update event source (the module that is raising this event)
        var module = HG.WebApp.Utility.GetModuleByDomainAddress(event.Domain, event.Source);
        if (module != null) {
            var curprop = HG.WebApp.Utility.GetModulePropertyByName(module, event.Property);
            // discard dupes if event is not a automation program event
            if ((module.Domain != 'HomeAutomation.HomeGenie.Automation' && curprop !== null && curprop.Value == event.Value) == false) {
                // update current event property 
                HG.WebApp.Utility.SetModulePropertyByName(module, event.Property, event.Value, event.Timestamp);
            }
			HG.WebApp.Control.RefreshGroupIndicators();
        }
        // send message to UI for updating UI elements related to this event (widgets, popup and such)
        HG.WebApp.Events.SendEventToUi(module, event);
        //
        //
        if (sessvars.UserSettings.EventsHistory)
        {
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
	        if ($.mobile.activePage.attr("id") == "page_events") {
	            HG.WebApp.Events.Refresh();
	        }
		}
    }
}

HG.WebApp.Events.Refresh = function () {

    var rows = '';
    for (var e = HG.WebApp.Data.Events.length - 1; e >= 0; e--) {
        var event = HG.WebApp.Data.Events[e];
        //
        var filterProperty = $('#events_filter_property').val();
        var filterSource = $('#events_filter_source').val();
        var filterDomain = $('#events_filter_domain').val();
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
    $('#page_events_table tbody').html(rows);
    $('#page_events_table').table().table("refresh");
    //{"Timestamp":"2014-04-10T12:55:46.1195672Z","Domain":"HomeAutomation.PhilipsHue","Source":"2","Description":"","Property":"Meter.Watts","Value":"0","UnixTimestamp":1397134546119.5674} ;

};

HG.WebApp.Events.SendEventToUi = function (module, eventLog) {

    // refresh widget associated to the module that raised the event
    if (module != null) {
        HG.WebApp.Control.UpdateModuleWidget(eventLog.Domain, eventLog.Source);
        // when event is an automation program event, we update the whole module
        if (module.Domain == 'HomeAutomation.HomeGenie.Automation') {
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
                var iconImage = configurepage_GetModuleIcon(module, null);
                popupdata = {
                    icon: iconImage,
                    title: eventLog.Property + '<br/>' + eventLog.Value,
                    text: '',
                    timestamp: date
                };
            }
            break;

        case 'HomeAutomation.HomeGenie.Automation':
            var iconImage = configurepage_GetModuleIcon(module, null);
            if (eventLog.Property == 'Runtime.Error') {
                if (eventLog.Value != '')
                {
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">Program ' + module.Address + '</span><br><b>' + eventLog.Value + '</b>',
                        text: 'Runtime<br />Error',
                        timestamp: date
                    };
                    if ($.mobile.activePage.attr("id") == "page_automation_editprogram") {
                        HG.WebApp.ProgramEdit.RefreshProgramOptions();
                    }
                }
                else if (HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address) {
                    //var cp = HG.WebApp.Utility.GetProgramByAddress(module.Address);
                    //if (cp != null) cp.ScriptErrors = '';
                    HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = '';
                    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                }
            }
            else if (eventLog.Property == 'Program.Status') {
                if (HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address)
                {
                    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                }
            }
            else {
                //var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
                //if (module.Name != '') name = module.Name;
                popupdata = {
                    icon: iconImage,
                    title: '<span style="color:yellow;font-size:9pt;">' + eventLog.Property + '</span><br>' + eventLog.Value,
                    text: "",
                    timestamp: date
                };
            }
            break;

        case 'HomeAutomation.ZWave':
            // events from Z-Wave controller (node 1)
            if (eventLog.Source == '1') {
                $('#configure_system_zwavediscovery_log').prepend('*&nbsp;' + eventLog.Value + '<br/>');
                popupdata = {
                    icon: 'images/genie.png',
                    title: '<span style="color:yellow;font-size:9pt;">' + eventLog.Property + '</span><br/>' + eventLog.Value,
                    text: '',
                    timestamp: date
                };
                break;
            }
            // continue to default processing

        default:
            if (module != null && eventLog.Property != 'Meter.Watts') {

                var iconImage = configurepage_GetModuleIcon(module, null);
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
                    var value = (parseFloat(eventLog.Value.replace(',', '.')).toFixed(2));
                    // TODO: should level be reported as is??
                    if (propname == 'Level') {
                        value = value * 100;
                        if (value > 98) value = 100;
                    }
                    value += '%';
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
    if (popupdata != null) {

        HG.WebApp.Events.ShowEventPopup(popupdata);

    }

};

HG.WebApp.Events.ShowEventPopup = function (popupdata) {

    var s = '<table width="100%"><tr><td width="48" rowspan="2">';
    s += '<img src="' + popupdata.icon + '" width="48">';
    s += '</td><td valign="top" align="left">';
    s += popupdata.title;
    s += '</td><td align="right" style="color:lime;font-size:12pt">' + popupdata.text + '</td></tr>';
    s += '<tr><td colspan="2" align="right"><span style="color:gray;font-size:8pt;">' + popupdata.timestamp + '</span>';
    s += '</td></tr></table>';
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