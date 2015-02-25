//
// namespace : HG.WebApp.Control namespace
// info      : -
//
HG.WebApp.Control = HG.WebApp.Control || {};
HG.WebApp.Control.PageId = 'page_control';
HG.WebApp.Control._RefreshTimeoutObject = null;
HG.WebApp.Control._RefreshIntervalObject = null;
HG.WebApp.Control._RefreshInterval = 10000;
HG.WebApp.Control._WidgetConfiguration = [];
HG.WebApp.Control._Widgets = [];
//
HG.WebApp.Control.InitializePage = function () {
    var page = $('#'+HG.WebApp.Control.PageId);
    page.on('pageinit', function (e) {
        $('#toolbar_macrorecord').hide();
        $('#toolbar_control').show();
        $('#control_macrorecord_optionspopup').bind('popupafterclose', function () {
            if ($('#macrorecord_delay_none').prop('checked')) {
                HG.Automation.Macro.SetDelay('None', '');
            }
            else if ($('#macrorecord_delay_mimic').prop('checked')) {
                HG.Automation.Macro.SetDelay('Mimic', '');
            }
            else if ($('#macrorecord_delay_fixed').prop('checked')) {
                HG.Automation.Macro.SetDelay('Fixed', $('#macrorecord_delay_seconds').val());
            }
        });
        $('#control_groupselect').change(function () {
            var gid = $('#control_groupselect').val();
            HG.WebApp.Control.ShowGroup(gid);
        });
        $.ajax({
            url: "pages/control/widgets/configuration.json",
            type: 'GET',
            success: function (data) {
                HG.WebApp.Control._WidgetConfiguration = eval(data);
            },
            error: function (data) {
                alert('error loading widgets configuration');
            }
        });
    });
    page.on('pagebeforeshow', function (e) {
        $.mobile.loading('show');
        HG.Configure.Groups.List('Control', function () 
        {
            if ($('#control_groupslist').children().length == 0) 
            {
                HG.WebApp.Control.RenderGroups();
            }
            HG.WebApp.Control.UpdateModules();
        });    
        HG.Automation.Macro.GetDelay(function(data){
            $('#macrorecord_delay_none').prop('checked', false).checkboxradio( 'refresh' );
            $('#macrorecord_delay_mimic').prop('checked', false).checkboxradio( 'refresh' );
            $('#macrorecord_delay_fixed').prop('checked', false).checkboxradio( 'refresh' );
            $('#macrorecord_delay_' + data.DelayType.toLowerCase()).prop('checked', true).checkboxradio( 'refresh' );
            $('#macrorecord_delay_seconds').val(data.DelayOptions);
        });
    });
};
//
HG.WebApp.Control.ToggleMenu = function () {
    $('#control_groupsmenu').slideToggle(400);
};
//
HG.WebApp.Control.ShowGroup = function (gid) {
    $.mobile.loading('show');
    HG.WebApp.Data._CurrentGroup = HG.WebApp.Data.Groups[gid].Name;
    HG.WebApp.Data._CurrentGroupIndex = gid;
    HG.WebApp.Control.RefreshGroupIndicators();
    $('#control_groupsmenu').slideUp(400, function () {
        $('#control_groupcontent').children('div').hide();
        $('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex).show();
        HG.WebApp.Control.RenderGroupModules(gid);
    });
};
//
HG.WebApp.Control.UpdateModules = function () {
    $.mobile.loading('show');
    HG.Configure.Modules.List(function (data) {
        //
        try {
            HG.WebApp.Data.Modules = eval(data);
        } catch (e) { }
        //
        HG.Automation.Programs.List(function () {
            $.mobile.loading('hide');
            HG.WebApp.Control.ShowGroup(HG.WebApp.Data._CurrentGroupIndex);
        });
    });
};
//
HG.WebApp.Control.RecordMacroStart = function () {
    $('#control_actionmenu').popup('close');
    HG.Automation.Macro.Record();
    $('#toolbar_control').hide('slidedown');
    setTimeout(function () {
        $('#toolbar_macrorecord').show('slideup');
    }, 500);
    $('#btn_control_macrorecord').qtip({
        content: {
            title: HG.WebApp.Locales.GetLocaleString('control_macrorecord_recording'),
            text: HG.WebApp.Locales.GetLocaleString('control_macrorecord_description'),
            button: HG.WebApp.Locales.GetLocaleString('control_macrorecord_close'),
        },
        show: { event: false, ready: true, delay: 1500 },
        events: {
            hide: function () {
                $(this).qtip('destroy');
            }
        },
        hide: { event: false, inactive: 5000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { my: 'bottom center', at: 'top center' }
    });
}
//
HG.WebApp.Control.RecordMacroSave = function (mode) {
    $.mobile.loading('show');
    HG.Automation.Macro.Save(mode, function (data) {
        HG.Automation.Programs.List(function () {
            HG.WebApp.AutomationGroupsList._CurrentGroup = '';
            HG.WebApp.ProgramEdit._CurrentProgram.Address = data;
            HG.Configure.Groups.List('Automation', function () {
                HG.WebApp.ProgramsList.EditProgram();
                $.mobile.changePage($('#page_automation_editprogram'), { transition: "slide" });
                $.mobile.loading('hide');
            });
        });
    });
    $('#toolbar_control').show('slideup');
    $('#toolbar_macrorecord').hide('slidedown');
}
//
HG.WebApp.Control.RecordMacroDiscard = function () {
    HG.Automation.Macro.Discard();
    $('#toolbar_control').show('slideup');
    $('#toolbar_macrorecord').hide('slidedown');
}
//
HG.WebApp.Control.RenderGroups = function () {
    $('#control_groupcontent').empty();
    $('#control_groupsmenu').empty();
    //
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (i == 0) {
            HG.WebApp.Data._CurrentGroup = HG.WebApp.Data.Groups[i].Name;
        }
        $('#control_groupsmenu').append('<li onclick="HG.WebApp.Control.ShowGroup(' + i + ')">' + HG.WebApp.Data.Groups[i].Name + '<table align="right" style="position:absolute;right:10px;top:8px;vertical-align:middle"><tr id="control_groupindicators_' + i + '"></tr></table></li>');
        var el = $('#control_groupcontent').append('<div id="groupdiv_modules_' + i + '" />');
    }
    $('#control_groupsmenu').listview('refresh');
};
//
HG.WebApp.Control.GetWidget = function (widgetpath, callback) {

    var widgetcached = false;
    for (var o = 0; o < HG.WebApp.Control._Widgets.length; o++) {
        if (HG.WebApp.Control._Widgets[o].Widget == widgetpath) {
            widgetcached = true;
            if (callback != null) callback(HG.WebApp.Control._Widgets[o]);
            break;
        }
    }
    if (widgetpath != '' && !widgetcached) {
        $.ajax({
            url: "pages/control/widgets/" + widgetpath + ".js",
            type: 'GET',
            success: function (data) {
                var widget = null;
                var widgetjson = null;
                try {
                    widgetjson = data;
                    widget = eval(widgetjson)[0];
                } catch (e) {
                    alert(widgetpath + " Loading Error:\n" + e);
                }
                //
                $.get("pages/control/widgets/" + widgetpath + ".html", function (responsedata) {

                    var widgetobj = { Widget: widgetpath, Instance: widget, Json: widgetjson, Model: responsedata };
                    HG.WebApp.Control._Widgets.push(widgetobj);
                    //
                    if (callback != null) callback(widgetobj);
                });
            },
            error: function (data) {

                if (callback != null) callback(null);

            }
        });
    }
    else {
        if (callback != null) callback(null);
    }

};
//
var widgetsloadqueue = [];
var widgetsloadtimer = null;
HG.WebApp.Control.RenderModule = function () {
    clearTimeout(widgetsloadtimer);
    if (widgetsloadqueue.length > 0) {
        //
        // extract and render element 
        var rendermodule = widgetsloadqueue.splice(0, 1)[0];
        var widget = $('#' + rendermodule.ElementId).data('homegenie.widget');
        if (widget != null && widget != 'undefined') {
            widget.RenderView('#' + rendermodule.ElementId, rendermodule.Module);
            widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule()', 10);
        }
        else {
            var html = '<div class="freewall"><div id="' + rendermodule.ElementId + '" class="hg-widget-container" data-context-group="' + rendermodule.GroupName + '" data-context-value="' + HG.WebApp.Utility.GetModuleIndexByDomainAddress(rendermodule.Module.Domain, rendermodule.Module.Address) + '">';
            HG.WebApp.Control.GetWidget(rendermodule.Module.Widget, function (w) {
                if (w != null) {
                    html += w.Model;
                    html += '</div></div>';
                    rendermodule.GroupElement.append(html);
                    rendermodule.GroupElement.trigger('create');
                    //rendermodule.GroupElement.listview('refresh');
                    //
                    if (w.Json != null) {
                        try {
                            var myinstance = eval(w.Json)[0];
                            // localize widget
                            HG.WebApp.Locales.LocalizeWidget(rendermodule.Module.Widget, rendermodule.ElementId);
                            // render widget view and store reference to its instance
                            myinstance.RenderView('#' + rendermodule.ElementId, rendermodule.Module);
                            $('#' + rendermodule.ElementId).data('homegenie.widget', myinstance);
                            rendermodule.Module.WidgetInstance = myinstance;
                            $('#module_options_button').unbind('click').bind('click.widget', function (event) {
                                HG.WebApp.GroupModules.ShowModuleOptions(HG.WebApp.GroupModules.CurrentModule.Domain, HG.WebApp.GroupModules.CurrentModule.Address);
                            });
                        } catch (e) {
                            console.log('[' + rendermodule.Module.Widget + '] widget error: "' + e + '", Line ' + e.lineNumber + ', Column ' + e.columnNumber);
                            //alert(rendermodule.Module.Widget + " Widget RenderView Error:\n" + e);
                        }
                    }
                    else {
                        alert(rendermodule.Module.Widget + " Widget Instance Error:\n" + e);
                    }

                }
                else {
                    //alert(rendermodule.Module.Widget + " Widget Error.");
                    // setTimeout('HG.WebApp.Control.RenderModule()', 10);
                }

                widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule()', 10);
            });
        }

    }
    else {
        rendermodulesbusy = false;
        $.mobile.loading('hide');
        //
        $('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex).isotope({
            itemSelector: '.freewall',
            layoutMode: 'fitRows'
        }).isotope();
        //
        HG.WebApp.Control.UpdateActionsMenu();
    }

};

HG.WebApp.Control.EditModule = function (module) {
    HG.WebApp.GroupModules.CurrentGroup = HG.WebApp.Data._CurrentGroup;
    HG.WebApp.GroupModules.CurrentModule = module;
    var oldtype = module.DeviceType;
    $('#module_remove_button').hide();
    HG.WebApp.GroupModules.ModuleEdit(function () {
        if (oldtype != module.DeviceType) {
            var grp = $('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex);
            grp.empty();
            HG.WebApp.Control.ShowGroup(HG.WebApp.Data._CurrentGroupIndex);
        }
        else {
            HG.WebApp.Control.UpdateModuleWidget(module.Domain, module.Address);
        }
    });
};

HG.WebApp.Control.GetModuleUid = function (module) {
    var domain = module.Domain.substring(module.Domain.lastIndexOf('.') + 1).replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
    var address = module.Address.replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
    var id = domain + '_' + address;
    return id;
};

HG.WebApp.Control.UpdateActionsMenu = function () {
    $('#control_custom_actionmenu').empty();
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (i == HG.WebApp.Data._CurrentGroupIndex) {
            var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
            for (var m = 0; m < groupmodules.Modules.length; m++) {
                var module = groupmodules.Modules[m];
                if (module.Widget == 'homegenie/generic/program') {
                    // add item to actions menu
                    $('#control_custom_actionmenu').append('<li><a class="ui-btn ui-icon-bars ui-btn-icon-right" onclick="HG.Automation.Programs.Toggle(\'' + module.Address + '\', \'' + HG.WebApp.Data._CurrentGroup + '\')">' + module.Name + '</a></li>');
                }
            }
        }
    }
    $('#control_custom_actionmenu').listview('refresh');
};

HG.WebApp.Control.UpdateModuleWidget = function (domain, address) {
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
        for (var m = 0; m < groupmodules.Modules.length; m++) {
            var module = groupmodules.Modules[m];
            if (module.Domain == domain && module.Address == address) {
                var uid = 'groupdiv_modules_' + groupmodules.Index + '_module_' + HG.WebApp.Control.GetModuleUid(module);
                var cuid = '#' + uid;
                var modui = $(cuid);
                var type = module.DeviceType + ''; type = type.toLowerCase();
                if (modui.length != 0) {
                    if (modui.data('homegenie.widget')) {
                        module.WidgetInstance = modui.data('homegenie.widget');
                        module.WidgetInstance.RenderView(cuid, module);
                    }
                }
            }
        }
    }
};

var rendermodulesdelay = null;
var rendermodulesbusy = false;
HG.WebApp.Control.RenderGroupModules = function (groupIndex) {
    if (widgetsloadqueue.length > 0 || rendermodulesbusy) {
        if (rendermodulesdelay != null) clearTimeout(rendermodulesdelay);
        rendermodulesdelay = setTimeout('HG.WebApp.Control.RenderGroupModules(' + groupIndex + ');', 100);
        return;
    }
    //
    rendermodulesbusy = true;
    rendermodulesdelay = null;
    //
    var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[groupIndex].Name);
    var grp = $('#groupdiv_modules_' + groupmodules.Index);
    for (var m = 0; m < groupmodules.Modules.length; m++) {
        var module = groupmodules.Modules[m];
        var uid = ($('#groupdiv_modules_' + groupmodules.Index).attr('id') + '_module_' + HG.WebApp.Control.GetModuleUid(module));
        var cuid = '#' + uid;
        var modui = $(cuid);
        var type = module.DeviceType + ''; type = type.toLowerCase();
        //
        var widgetfound = false;

        // look for UI Group Label (fake module with domain HomeGenie.UI.GroupLabel
        if (module.Domain == 'HomeGenie.UI.Separator') {
            module.Widget = 'homegenie/generic/grouplabel';
            widgetfound = true;
        }

        // look for explicit widget display module parameter
        if (!widgetfound) {
            var displaymodule = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
            if (displaymodule != null && displaymodule.Value != '') {
                module.Widget = displaymodule.Value;
                widgetfound = true;
            }
        }
        // fallback to configuration.json widgets mapping
        if (!widgetfound) {
            for (var wi = 0; wi < HG.WebApp.Control._WidgetConfiguration.length; wi++) {
                var widgetobj = HG.WebApp.Control._WidgetConfiguration[wi];
                var modprop = HG.WebApp.Utility.GetModulePropertyByName(module, widgetobj.MatchProperty);
                if (modprop != null && (widgetobj.MatchValue == "*" || modprop.Value == widgetobj.MatchValue)) {
                    module.Widget = widgetobj.Widget;
                    widgetfound = true;
                    break;
                }
            }
        }
        // last fall back.... select a generic widget based on DeviceType if no category specific widget has been found
        if (!widgetfound) {
            module.Widget = 'homegenie/generic/' + (type == 'undefined' ? 'unknown' : type);
        }
        //
        if (modui.length == 0) {
            widgetsloadqueue.push({ GroupName: HG.WebApp.Data.Groups[groupIndex].Name, GroupElement: grp, ElementId: uid, Module: module });
        }
        else {
            if (modui.data('homegenie.widget')) {
                module.WidgetInstance = modui.data('homegenie.widget');
                module.WidgetInstance.RenderView(cuid, module);
            }
        }
    }
    //
    widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule();', 10);
    if (widgetsloadqueue.length == 0) {
        HG.WebApp.Control.UpdateActionsMenu();
    }
};
//
HG.WebApp.Control.RefreshGroupIndicators = function () {
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
        var grouploadkw = 0;
        var operating_lights = 0;
        var operating_switches = 0;
        var group_temperature = null;
        var group_humidity = null;
        var group_luminance = null;
        //
        var grp = $('#groupdiv_modules_' + groupmodules.Index);
        for (var m = 0; m < groupmodules.Modules.length; m++) {
            var module = groupmodules.Modules[m];
            var uid = ($('#groupdiv_modules_' + groupmodules.Index).attr('id') + '_module_' + HG.WebApp.Control.GetModuleUid(module));
            var cuid = '#' + uid;
            var modui = $(cuid);
            var type = module.DeviceType + ''; type = type.toLowerCase();
            //
            var w = HG.WebApp.Utility.GetModulePropertyByName(module, "Meter.Watts");
            var l = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
            if (w != null && l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                grouploadkw += (parseFloat(w.Value.replace(',', '.')) / 1000.0);
            }
            if (l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                switch (type) {
                    case 'dimmer':
                    case 'light':
                        operating_lights++;
                        break;
                    case 'switch':
                        operating_switches++;
                        break;
                }
            }

            if (group_temperature == null) {
                var t = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Temperature");
                if (t != null && t.Value != '') {
                    group_temperature = parseFloat(t.Value.replace(',', '.'));
                }
            }

            if (group_humidity == null) {
                var h = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Humidity");
                if (h != null && h.Value != '') {
                    group_humidity = parseFloat(h.Value.replace(',', '.'));
                }
            }

            if (group_luminance == null) {
                var l = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Luminance");
                if (l != null && l.Value != '') {
                    group_luminance = parseFloat(l.Value.replace(',', '.'));
                }
            }
        }
        //

        //		'<td align="center"><img src="images/indicators/door.png" style="vertical-align:middle" /> <span style="font-size:12pt;color:whitesmoke">1</span></td>'+

        var indicators = '';
        if (group_temperature != null) {
            indicators += '<td align="center"><span class="hg-indicator-temperature">' + (group_temperature * 1).toFixed(1) + '</span></td><td>&nbsp;</td>';
        }
        if (group_humidity != null) {
            indicators += '<td align="center"><span class="hg-indicator-humidity">' + (group_humidity * 1).toFixed(0) + '</span></td><td>&nbsp;</td>';
        }
        if (group_luminance != null) {
            indicators += '<td align="center"><span class="hg-indicator-luminance">' + (group_luminance * 1).toFixed(0) + '</span></td><td>&nbsp;</td>';
        }
        if (operating_lights > 0) {
            indicators += '<td align="center"><span class="hg-indicator-bulb">' + operating_lights + '</span></td><td>&nbsp;</td>';
        }
        if (operating_switches > 0) {
            indicators += '<td align="center"><span class="hg-indicator-plug">' + operating_switches + '</span></td><td>&nbsp;</td>';
        }
        if (grouploadkw > 0) {
            indicators += '<td align="center"><span class="hg-indicator-energy">' + (grouploadkw * 1000).toFixed(1) + '</span></td><td>&nbsp;</td>';
        }

        $('#control_groupindicators_' + i).html(indicators);

        if (i == HG.WebApp.Data._CurrentGroupIndex) {
            $('#control_groupmenutitle').html(HG.WebApp.Data.Groups[i].Name);
            $('#control_groupindicators').html(indicators);
        }
    }
}
//
HG.WebApp.Control.LoadGroups = function () {
    $.mobile.loading('show');
    HG.Configure.Groups.List('Control', function () {
        HG.WebApp.Control.RenderGroups();
    });
};
//
HG.WebApp.Control.ModuleSetLevel = function (pv) {
    HG.Control.Modules.ServiceCall('Control.Level', HG.WebApp.Data._CurrentModule.Domain, HG.WebApp.Data._CurrentModule.Address, pv, function (data) { });
};
//
HG.WebApp.Control.GroupLightsOn = function (group) {
    HG.Automation.Groups.LightsOn(group);
};
//
HG.WebApp.Control.GroupLightsOff = function (group) {
    HG.Automation.Groups.LightsOff(group);
};

HG.WebApp.Control.Toggle = function (domain, address) {
    HG.Control.Modules.ServiceCall('Control.Toggle', domain, address, '', function (data) { });
};