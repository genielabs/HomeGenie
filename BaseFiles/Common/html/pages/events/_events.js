HG.WebApp.Events = HG.WebApp.Events || new function() { var $$ = this;

    $$._eventQueueCapacity = 200;
    $$._ledOffTimeout = null;
    $$._popupHideTimeout = null;
    $$._listeners = [];

    $$.InitializePage = function () {
        var page = $$.getContainer();
        page.on('pagebeforeshow', function (e) {
            $$.Refresh();
        });
        $$.field('property-txt').change(function () {
            $$.Refresh();
        });
        $$.field('source-txt').change(function () {
            $$.Refresh();
        });
        $$.field('domain-txt').change(function () {
            $$.Refresh();
        });
        setTimeout(function () {
            $$.Setup();
        }, 2000);
    };

    $$.AddListener = function (listener) {
        // listener object must implement listener.parameterEventCallback functionn
        listener._eventListenerId = ($$._listeners.length + 1);
        $$._listeners.push(listener);
    };

    $$.RemoveListener = function (listener) {
        for (var l = 0; l < $$._listeners.length; l++) {
            if ($$._listeners[l]._eventListenerId == listener._eventListenerId) {
                $$._listeners.splice(l, 1);
                break;
            }
        }
    };

    $$.Setup = function () {
        var es = new EventSource('/events');
        es.onopen = function(e) {
            $$.ShowEventPopup({
                            icon: 'images/genie.png',
                            title: 'HomeGenie<br/>Event Stream<br/>CONNECTED!',
                            text: '',
                            timestamp: ''
                        });
        };
        es.onerror = function(e) {
            $$.ShowEventPopup({
                            icon: 'images/genie.png',
                            title: 'HomeGenie<br/>EventStream<br/>DISCONNECTED!',
                            text: '',
                            timestamp: ''
                        });
            es.close();
            setTimeout($$.Setup, 1000);
        };
        es.onmessage = function (e) {
            var event = JSON && JSON.parse(e.data) || $.parseJSON(e.data);
            event.Value = event.Value.toString();
            //
            var module = null;
            if ((event.Domain == 'HomeGenie.System' && event.Property == 'Console.Output') == false) {
                // update event source (the module that is raising this event)
                module = HG.WebApp.Utility.GetModuleByDomainAddress(event.Domain, event.Source);
                if (module != null) {
                    HG.WebApp.Utility.SetModulePropertyByName(module, event.Property, event.Value, event.Timestamp);
                    HG.WebApp.Control.RefreshGroupIndicators();
                }
                // send message to UI for updating UI elements related to this event (widgets, popup and such)
                $$.SendEventToUi(module, event);
            }
            //
            if (event.Domain == 'MIGService.Interfaces') {
                HG.WebApp.Home.UpdateInterfacesStatus();
            }
            //
            if (dataStore.get('UI.EventsHistory')) {
                // add message to local events queue
                HG.WebApp.Data.Events.push(event);
                if (HG.WebApp.Data.Events.length > $$._eventQueueCapacity) {
                    HG.WebApp.Data.Events.shift();
                }
                // blink hg activity led
                if ($$._ledOffTimeout != null) {
                    clearTimeout($$._ledOffTimeout);
                }
                $$.field('#event_status_off', true).hide();
                $$.field('#event_status_on', true).show();
                $$._ledOffTimeout = setTimeout(function () {
                    $$._ledOffTimeout = null;
                    $$.field('#event_status_on', true).hide();
                    $$.field('#event_status_off', true).show();
                }, 500);
                //
                // refresh events list page if currently open
                if ($.mobile.activePage.attr("id") == $$.PageId) {
                    $$.Refresh();
                }
            }

            for (var l = 0; l < $$._listeners.length; l++) {
                $$._listeners[l].parameterEventCallback(module, event);
            }
        }
    }

    $$.Refresh = function () {
        var page = $('#'+$$.PageId);
        var rows = '';
        for (var e = HG.WebApp.Data.Events.length - 1; e >= 0; e--) {
            var event = HG.WebApp.Data.Events[e];
            //
            var filterProperty = $$.field('property-txt').val();
            var filterSource = $$.field('source-txt').val();
            var filterDomain = $$.field('domain-txt').val();
            if (filterDomain != '' && event.Domain.indexOf(filterDomain) < 0) continue;
            if (filterSource != '' && event.Source.indexOf(filterSource) < 0) continue;
            if (filterProperty != '' && event.Property.indexOf(filterProperty) < 0) continue;
            //
            var d = new Date(event.UnixTimestamp);
            var longDate = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d);
            rows += '<tr>';
            rows += '<td><abbr title="' + longDate + '">' + HG.WebApp.Utility.FormatDateTime(d, 'sm') + '</abbr></td>';
            rows += '<td>' + event.Property + '</td>';
            rows += '<td>' + event.Value + '</td>';
            rows += '<td>' + event.Source + '</td>';
            rows += '<td>' + event.Domain + '</td>';
            rows += '</tr>';
        }
        var eventTable = $$.field('events-tbl');
        eventTable.find('tbody').html(rows);
        eventTable.table().table("refresh");
        //{"Timestamp":"2014-04-10T12:55:46.1195672Z","Domain":"HomeAutomation.PhilipsHue","Source":"2","Description":"","Property":"Meter.Watts","Value":"0","UnixTimestamp":1397134546119.5674} ;

    };

    $$.SendEventToUi = function (module, eventLog) {

        // refresh widget associated to the module that raised the event
        if (module != null) {
            HG.WebApp.Control.UpdateModuleWidget(eventLog.Domain, eventLog.Source, eventLog.Property, eventLog.Value);
            if ($.mobile.activePage.attr("id") == HG.WebApp.WidgetEditor.PageId) {
                HG.WebApp.WidgetEditor.RenderView(eventLog);
            }
            // when event is an automation program event or the 'Program.UiRefresh' one, we update the whole module
            if ((module.Domain == 'HomeAutomation.HomeGenie.Automation' && eventLog.Property != 'Program.Status') || eventLog.Property == 'Program.UiRefresh') {
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
        // show event popup as needed
        var logdate = new Date(eventLog.UnixTimestamp);
        var date = HG.WebApp.Utility.FormatDateTime(logdate);
        var popupdata = null;
        switch (eventLog.Domain) {

            case 'HomeGenie.System':
                if (eventLog.Value == 'STARTED') {
                    $$.field('#configure_system_updateinstall_status', true).html('Update install complete. HomeGenie started.');
                    setTimeout(function () {
                        document.location.href = '/';
                    }, 3000);
                }
                // continue to default 'HomeGenie.*' event processing

            case 'HomeGenie.UpdateChecker':
                if (eventLog.Property == 'InstallProgress.Message') {
                    $$.field('#configure_system_updateinstall_log', true).prepend('*&nbsp;' + eventLog.Value + '<br/>');
                } else {
                    $$.field('#configure_system_updateinstall_log', true).prepend('*&nbsp;<strong>' + eventLog.Property + '</strong><br/>&nbsp;&nbsp;' + eventLog.Value + '<br/>');
                    var iconImage = 'images/genie.png';
                    popupdata = {
                        icon: iconImage,
                        title: eventLog.Property + '<br/>' + eventLog.Value,
                        text: '',
                        timestamp: date
                    };
                }
                break;

            case 'HomeGenie.PackageInstaller':
                var log = $$.field('#systemsettings_browserepo', true).find('[data-ui-field=install_text]');
                var restore_log = $$.field('#systemsettings_backuprestores1', true).find('[data-ui-field=restore_log]');
                if (eventLog.Property == 'InstallProgress.Message') {
                    log.append('* ' + eventLog.Value + '<br/>');
                    restore_log.append('* ' + eventLog.Value + '<br/>');
                } else {
                    log.append('* <strong>' + eventLog.Property + '</strong><br/>&nbsp;&nbsp;' + eventLog.Value + '<br/>');
                    restore_log.append('* <strong>' + eventLog.Property + '</strong><br/>&nbsp;&nbsp;' + eventLog.Value + '<br/>');
                    var iconImage = 'images/genie.png';
                    popupdata = {
                        icon: iconImage,
                        title: eventLog.Property + '<br/>' + eventLog.Value,
                        text: '',
                        timestamp: date
                    };
                }
                var height = log.parent()[0].scrollHeight;
                log.parent().scrollTop(height);
                height = restore_log[0].scrollHeight;
                restore_log.scrollTop(height);
                break;

            case 'HomeGenie.BackupRestore':
                var restore_log = $$.field('#systemsettings_backuprestores1', true).find('[data-ui-field=restore_log]');
                restore_log.append('* ' + eventLog.Value + '<br/>');
                var height = restore_log[0].scrollHeight;
                restore_log.scrollTop(height);
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
                    } else if (module != null && HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address) {
                        //var cp = HG.WebApp.Utility.GetProgramByAddress(module.Address);
                        //if (cp != null) cp.ScriptErrors = '';
                        HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = '';
                        HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                    }
                } else if (eventLog.Property == 'Program.Status') {
                    if (module != null && HG.WebApp.ProgramEdit._CurrentProgram.Address == module.Address) {
                        HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                    }
                } else if (eventLog.Property == 'Program.Notification') {
                    var notification = JSON.parse(eventLog.Value);
                    popupdata = {
                        icon: iconImage,
                        title: '<span style="color:yellow;font-size:9pt;">' + notification.Title + '</span><br>' + notification.Message,
                        text: '',
                        timestamp: date
                    };
                } else if (!$$.IsBlacklisted(eventLog.Property)) {
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
                    break;
                }
                // continue to default processing

            default:
                if (module != null && !$$.IsBlacklisted(eventLog.Property)) {

                    var iconImage = HG.Ui.GetModuleIcon(module, null);
                    if ((module.Address == 'RF' || module.Address == 'IR') && eventLog.Value != '') {
                        iconImage = 'images/remote.png';
                        popupdata = {
                            icon: iconImage,
                            title: '<span style="color:yellow;font-size:9pt;">' + module.Domain.substring(module.Domain.indexOf('.') + 1) + '</span><br/>' + eventLog.Value,
                            text: module.Address,
                            timestamp: date
                        };
                    } else if (eventLog.Property.substring(0, 7) == 'Sensor.') {
                        var group = HG.WebApp.GroupsList.GetModuleGroup(module);
                        if (group != null) group = group.Name;
                        var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
                        var propname = eventLog.Property.substring(eventLog.Property.indexOf('.') + 1);
                        //
                        switch (propname) {
                            case 'Temperature':
                                iconImage = 'pages/control/widgets/homegenie/generic/images/temperature.png';
                                var temperature = eventLog.Value.replace(',', '.');
                                eventLog.Value = HG.WebApp.Utility.FormatTemperature(temperature);
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
                    } else if (eventLog.Property.substring(0, 7) == 'Status.') {
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
                    } else if (HG.WebApp.ProgramEdit._IsCapturingCommands && eventLog.Value != '') {
                        var command = HG.WebApp.Utility.GetCommandFromEvent(module, eventLog);
                        if (command != null) {
                            HG.WebApp.ProgramEdit._CurrentProgram.Commands.push(command);
                            automationpage_CommandsRefresh();
                        }
                    }
                }
                break;
        }

        if (popupdata != null 
                && !$$.PopupHasIgnore(eventLog.Domain, eventLog.Source, eventLog.Property) && eventLog.Description.indexOf(':nopopup:') < 0
                && !$$.IsBlacklisted(eventLog.Property)) {

            $$.ShowEventPopup(popupdata, eventLog);

        }

    };

    $$.IsBlacklisted = function(property) {
        if (property == 'Meter.Watts' || property.startsWith('ConfigureOptions.') || property == 'Program.UiRefresh')
            return true;
        return false;
    };

    $$.PopupRefreshIgnore = function () {
        var ignoreList = dataStore.get('UI.EventPopupIgnore');
        if (ignoreList == null) return;
        $$.field('#popupsettings_ignorelist', true).find('li:gt(0)').remove();
        for (var i = 0; i < ignoreList.length; i++)
        {
            var item = '<li data-icon="minus">';
            item += '    <a href="#" class="ui-grid-b" onclick="$$.PopupRemoveIgnore(' + i + ')">';
            item += '        <div class="ui-block-a hg-label" style="width:40%">' + ignoreList[i].Domain + '</div>';
            item += '        <div class="ui-block-b hg-label" style="width:20%" align="center">' + ignoreList[i].Address + '</div>';
            item += '        <div class="ui-block-c hg-label" style="width:40%">' + ignoreList[i].Property + '</div>';
            item += '    </a>';
            item += '</li>';
            $$.field('#popupsettings_ignorelist', true).append(item);
        }
        $$.field('#popupsettings_ignorelist', true).listview().trigger('create');
        $$.field('#popupsettings_ignorelist', true).listview('refresh');
    }

    $$.PopupHasIgnore = function (domain, address, property) {
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

    $$.PopupRemoveIgnore = function (index) {
        var ignoreList = dataStore.get('UI.EventPopupIgnore');
        if (ignoreList == null || ignoreList.length <= index) return;
        ignoreList.splice(index, 1);
        $$.PopupRefreshIgnore();
    };

    $$.PopupAddIgnore = function (domain, address, property) {
        var ignoreList = dataStore.get('UI.EventPopupIgnore');
        if (ignoreList == null)
        {
            ignoreList = Array();
        }
        if (!$$.PopupHasIgnore(domain, address, property))
        {
            ignoreList.push({ Domain: domain, Address: address, Property: property });
            dataStore.set('UI.EventPopupIgnore', ignoreList);
        }
        $$.field('#statuspopup', true).css('display', 'none');
    };

    $$.ShowEventPopup = function (popupdata, eventLog) {

        var s = '<table width="100%"><tr><td width="42" valign="top">';
        s += '<img src="' + popupdata.icon + '" width="42">';
        s += '</td><td valign="top" align="left">';
        s += popupdata.title;
        s += '</td><td align="right" style="color:lime;font-size:12pt">' + popupdata.text + '</td></tr>';
        s += '<tr style="color:gray;font-size:9pt;"><td colspan="2">' + popupdata.timestamp + '</td>';
        if (eventLog)
        {
            s += '<td align="right"><a href="#" title="Block popup from this source" onclick="$$.PopupAddIgnore(\'' + eventLog.Domain + '\',\'' + eventLog.Source + '\',\'' + eventLog.Property + '\')"><img border="0" alt="Block popup from this source" src="images/halt.png" /></a></td>';
        }
        s += '</tr>';
        s += '</table>';
        var popup = $$.field('#statuspopup', true);
        popup.html(s);
        // hide timeout
        if ($$._popupHideTimeout != null) {
            clearTimeout($$._popupHideTimeout);
        }
        else {
            popup.css('display', '');
            popup.animate({ opacity: '0' }, 0, function () {
                popup.animate({ right: '5px', opacity: '0.90' }, 300);
            });
        }
        $$._popupHideTimeout = setTimeout(function () {
            if (popup.css('display') != 'none') {
                popup.animate({ right: '-300px', opacity: '0.0' }, 300, function () {
                    popup.css('display', 'none');
                });
            }
            $$._popupHideTimeout = null;
        }, 5000);

    };

};
HG.Ui.CreatePage(HG.WebApp.Events, 'page_events');