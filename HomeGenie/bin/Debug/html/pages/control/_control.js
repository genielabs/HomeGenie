//
// namespace : HG.WebApp.Control namespace
// info      : -
//
HG.WebApp.Control = HG.WebApp.Control || {};
//
HG.WebApp.Control._RefreshTimeoutObject = null;
HG.WebApp.Control._RefreshIntervalObject = null;
HG.WebApp.Control._RefreshInterval = 10000;
HG.WebApp.Control._WidgetConfiguration = [];
HG.WebApp.Control._Widgets = [];
//
HG.WebApp.Control.InitializePage = function () {
    $('#page_control').on('pageinit', function (e) {

        $('#control_groupslist').on('click', 'li', function () {
            HG.WebApp.Data._CurrentGroup = $(this).attr('data-context-group');
            var modidx = $(this).attr('data-context-value');
            if (modidx != -1) {
                HG.WebApp.Data._CurrentModule = HG.WebApp.Data.Modules[modidx];
            }
            else {
                HG.WebApp.Data._CurrentModule = null;
            }
        });
        //
        //			$('#module_control_popup').on('popupbeforeposition', function (event) {
        //				$('#page_control li').removeClass("ui-btn-active");
        //				HG.WebApp.Control.RefreshModulePopup();
        //			});
        //
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
        //
        $.ajax({
            url: "pages/control/widgets/configuration.json",
            data: "{ dummy: 'dummy' }",
            //dataType: 'json',
            success: function (data) {
                HG.WebApp.Control._WidgetConfiguration = eval(data);
            },
            error: function (data) {
                alert('error loading widgets configuration');
            }
        });
    });
};
//
HG.WebApp.Control.SetAutoRefresh = function (autorefresh) {
    if (HG.WebApp.Control._RefreshIntervalObject != null) clearInterval(HG.WebApp.Control._RefreshIntervalObject);
    HG.WebApp.Control._RefreshIntervalObject = null;
    if (autorefresh) {
        HG.WebApp.Control._RefreshIntervalObject = setInterval('HG.WebApp.Control.Refresh();', HG.WebApp.Control._RefreshInterval);
    }
};
//
HG.WebApp.Control._RefreshTs = new Date().getTime();
HG.WebApp.Control.Refresh = function () {
    if (HG.WebApp.Control._RefreshTimeoutObject != null) window.clearTimeout(HG.WebApp.Control._RefreshTimeoutObject);
    var delay = 100;
    if (new Date().getTime() - HG.WebApp.Control._RefreshTs < 2000) delay = 2000;;
    HG.WebApp.Control._RefreshTs = new Date().getTime() + delay;
    HG.WebApp.Control._RefreshTimeoutObject = window.setTimeout('HG.WebApp.Control._RefreshFn()', delay);
};
//
HG.WebApp.Control._RefreshFn = function () {
    HG.WebApp.Data._IgnoreUIEvents = true;
    HG.Configure.Modules.List(function (data) {
        //
        try {
            HG.WebApp.Data.Modules = eval(data);
        } catch (e) { }
        //
        HG.Automation.Programs.List(function () {
            HG.WebApp.Control.RenderGroupModules();
            //
            //HG.WebApp.Control.RefreshModulePopup();
            //
        });
        HG.WebApp.Data._IgnoreUIEvents = false;
    });
};
//
HG.WebApp.Control.RecordMacroStart = function () {
    $('#control_actionmenu').popup('close');
    HG.Automation.Macro.Record();
    $('#toolbar_control').hide('slidedown');
    $('#toolbar_macrorecord').show('slideup');
}
//
HG.WebApp.Control.RecordMacroSave = function (mode) {
    $.mobile.showPageLoadingMsg();
    HG.Automation.Macro.Save(mode, function (data) {
        HG.Automation.Programs.List(function () {
            HG.WebApp.AutomationGroupsList._CurrentGroup = '';
            HG.WebApp.ProgramEdit._CurrentProgram.Address = data;
            HG.Configure.Groups.List('Automation', function () {
                HG.WebApp.ProgramsList.EditProgram();
                $.mobile.changePage($('#page_automation_editprogram'), { transition: "slide" });
                $.mobile.hidePageLoadingMsg();
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
HG.WebApp.Control.RenderGroupsCollapsibleItems = function () {
    $('#control_groupslist').empty();
    //
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (i == 0) {
            HG.WebApp.Data._CurrentGroup = HG.WebApp.Data.Groups[i].Name;
        }
        var el = $('#control_groupslist').append('<span id="group_load_info_' + i + '" style="z-index:100;position:absolute;right:50px;margin-top:14px;font-size:9pt;"></span><img id="group_load_icon_' + i + '" src="images/led-green-off.png" style="z-index:100;position:absolute;right:20px;margin-top:10px"><div data-role="collapsible" data-inset="false" id="groupdiv_' + i + '" data-collapsed="true" onclick="HG.WebApp.Data._CurrentGroup = \'' + HG.WebApp.Data.Groups[i].Name + '\'; "><h3>' + HG.WebApp.Data.Groups[i].Name + '</h3><ul id="groupdiv_modules_' + i + '" data-theme="c" data-content-theme="c" data-inset="false" /></div>');
    }
    //
    $('#control_groupslist a[data-role="button"]').button();
    $('#control_groupslist div[data-role*="controlgroup"]').controlgroup();
    $('#control_groupslist').collapsibleset();
    //	    $('#control_groupslist').collapsibleset('refresh');
    //
    $('#control_groupslist div[data-role="collapsible"]').each(function (args) {
        $(this).children().next().slideToggle(0);
        $(this).children().next().slideToggle(0);
    });
    //
    $('#control_groupslist div[data-role="collapsible"]').bind('expand collapse', function (event) {
        $(this).children().next().slideToggle(500);
        return false;
    });
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
            url: "pages/control/widgets/" + widgetpath + ".json",
            data: "{ dummy: 'dummy' }",
            //dataType: 'json',
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
var widgetsinitialized = false;
HG.WebApp.Control.RenderModule = function () {
    clearTimeout(widgetsloadtimer);
    if (widgetsloadqueue.length > 0) {
        //
        widgetsinitialized = false;
        //
        // extract and render element 
        var rendermodule = widgetsloadqueue.splice(0, 1)[0];
        var widget = $('#' + rendermodule.ElementId).data('homegenie.widget');
        if (widget != null && widget != 'undefined') {
            widget.RenderView('#' + rendermodule.ElementId, rendermodule.Module);
            widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule()', 1);
        }
        else {
            var html = '<li id="' + rendermodule.ElementId + '" data-icon="false" data-context-group="' + rendermodule.GroupName + '" data-context-value="' + HG.WebApp.Utility.GetModuleIndexByDomainAddress(rendermodule.Module.Domain, rendermodule.Module.Address) + '">';
            HG.WebApp.Control.GetWidget(rendermodule.Module.Widget, function (w) {
                if (w != null) {
                    html += w.Model;
                    html += '</li>';
                    rendermodule.GroupElement.append(html);
                    rendermodule.GroupElement.listview('refresh');
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
                        } catch (e) {
                            //alert(rendermodule.Module.Widget + " Widget RenderView Error:\n" + e);
                        }
                    }
                    else {
                        alert(rendermodule.Module.Widget + " Widget Instance Error:\n" + e);
                    }

                }
                else {
                    //alert(rendermodule.Module.Widget + " Widget Error.");
                    // setTimeout('HG.WebApp.Control.RenderModule()', 50);
                }

                widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule()', 1);
            });
        }

    }
    else {
        rendermodulesbusy = false;
        $.mobile.hidePageLoadingMsg();
        //
        if (!widgetsinitialized) {
            widgetsinitialized = true;
            setTheme(uitheme);
        }
    }

};

HG.WebApp.Control.GetModuleUid = function (module) {
    var domain = module.Domain.substring(module.Domain.lastIndexOf('.') + 1).replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
    var address = module.Address.replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
    var id = domain + '_' + address;
    return id;
};

HG.WebApp.Control.UpdateModuleWidget = function (domain, address) {
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
        for (var m = 0; m < groupmodules.Modules.length; m++) {
            var module = groupmodules.Modules[m];
            if (module.Domain == domain && module.Address == address) {
                var uid = ($('#groupdiv_modules_' + groupmodules.Index).attr('id') + '_module_' + HG.WebApp.Control.GetModuleUid(module));
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
HG.WebApp.Control.RenderGroupModules = function () {
    if (widgetsloadqueue.length > 0 || rendermodulesbusy) {
        if (rendermodulesdelay != null) clearTimeout(rendermodulesdelay);
        rendermodulesdelay = setTimeout('HG.WebApp.Control.RenderGroupModules();', 100);
        return;
    }
    //
    rendermodulesbusy = true;
    rendermodulesdelay = null;
    //
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
        var grouploadkw = 0;
        var somedevon = false;
        //
        var grp = $('#groupdiv_modules_' + groupmodules.Index);
        for (var m = 0; m < groupmodules.Modules.length; m++) {
            var module = groupmodules.Modules[m];
            var uid = ($('#groupdiv_modules_' + groupmodules.Index).attr('id') + '_module_' + HG.WebApp.Control.GetModuleUid(module));
            var cuid = '#' + uid;
            var modui = $(cuid);
            var type = module.DeviceType + ''; type = type.toLowerCase();
            //
            var widgetfound = false;
            // look for explicit widget display module parameter
            var displaymodule = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
            if (displaymodule != null && displaymodule.Value != '') {
                module.Widget = displaymodule.Value;
                widgetfound = true;
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
                widgetsloadqueue.push({ GroupName: HG.WebApp.Data.Groups[i].Name, GroupElement: grp, ElementId: uid, Module: module });
            }
            else {
                if (modui.data('homegenie.widget')) {
                    module.WidgetInstance = modui.data('homegenie.widget');
                    module.WidgetInstance.RenderView(cuid, module);
                }
            }
            //
            var w = HG.WebApp.Utility.GetModulePropertyByName(module, "Meter.Watts");
            var l = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
            if (w != null && l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                grouploadkw += (parseFloat(w.Value.replace(',', '.')) / 1000.0);
            }
            if (l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                somedevon = true;
            }
        }
        //
        $('#group_load_icon_' + i).attr('src', 'images/led-green-off.png');
        $('#group_load_info_' + i).html('');
        //
        if (grouploadkw > 0 || somedevon) {
            $('#group_load_icon_' + i).attr('src', 'images/led-green-on.png');
        }
        if (grouploadkw > 0) {
            $('#group_load_info_' + i).html('kW ' + grouploadkw.toFixed(3));
        }
        grp.listview();

    }
    //
    $('#control_groupslist').collapsibleset();
    //
    widgetsloadtimer = setTimeout('HG.WebApp.Control.RenderModule();', 1);
};
//
HG.WebApp.Control.LoadGroups = function () {
    $.mobile.showPageLoadingMsg();
    HG.Configure.Groups.List('Control', function () {
        HG.WebApp.Control.RenderGroupsCollapsibleItems();
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