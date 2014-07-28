
$(document).ready(function () {

    HG.Configure.LoadData(function () {

        // preload widgets
        HG.Control.BS3.Widgets.LoadAll(HG.WebApp.Data.Modules, function () {

            var cgroup_idx = 0;
            var cgroup_obj = null;

            var itemclick_handler = function (e) {
                $('#hg_menu_groups li').removeClass('active');
                $(this).addClass('active');
                var groupname = $(this).attr('data-hg-groupname');
                //
                // update group modules and render them
                //
                HG.Configure.Groups.ModulesList(groupname, function (modules) {

                    $('#hg_group_menu').empty();
                    for (var m = 0; m < modules.length; m++) {

                        var module = HG.WebApp.Utility.GetModuleByDomainAddress(modules[m].Domain, modules[m].Address);
                        module.Properties = modules[m].Properties;
                        //
                        var displaywidget = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
                        if (displaywidget != null && displaywidget.Value == 'homegenie/generic/program') {
                            $('#hg_group_menu').append('<li><a href="javascript:HG.Control.BS3.Macros.ProgramRun(\'' + module.Address + '\', \'' + groupname + '\');">' + module.Name + '</a></li>');
                        }
                    }
                    HG.Control.BS3.Groups.RenderModules(groupname);

                });
            };

            $('#hg_menu_groups').empty();
            for (var g = 0; g < HG.WebApp.Data.Groups.length; g++) {
                var group = HG.WebApp.Data.Groups[g];
                $('#hg_menu_groups').append(function () {
                    var modcount = group.Modules.length;
                    return $('<li data-hg-groupname="' + group.Name + '" ' + (g == 0 ? ' class="active"' : '') + '><a data-toggle="collapse" data-target=".navbar-menu-collapse" href="#"><span class="badge">' + modcount + '</span> ' + group.Name + '</a></li>').click(itemclick_handler);
                });
                if (g == cgroup_idx) // default group
                {
                    cgroup_obj = group;
                }
            }

            if (cgroup_obj != null) {
                HG.Control.BS3.Groups.RenderModules(cgroup_obj.Name);
            }

        });

    });

    HG.Control.BS3.ClockRun();

    /*
    $('#scheme_color_navbar').minicolors({
    animationSpeed: 100,
    animationEasing: 'swing',
    change: function () { $('#change_color_navbar').removeClass('disabled'); },
    changeDelay: 0,
    control: 'saturation',
    hide: null,
    hideSpeed: 100,
    inline: false,
    letterCase: 'lowercase',
    opacity: false,
    position: 'bottom left',
    show: null,
    showSpeed: 100,
    swatchPosition: 'left',
    textfield: true
    });
    */

});    // <-- document.ready


// HG.Control.BS3 namespace

HG = HG || {};
HG.Control = HG.Control || {};
HG.Control.BS3 = HG.Control.BS3 || {};
//
HG.Control.BS3.Groups = HG.Control.BS3.Groups || {};
HG.Control.BS3.Groups._CurrentGroup = '';
HG.Control.BS3._GroupsRefreshTimeout = null;
//
HG.Control.BS3.Macros = HG.Control.BS3.Macros || {};
//
HG.Control.BS3.Service = HG.Control.BS3.Service || {};
//
HG.Control.BS3.Widgets = HG.Control.BS3.Widgets || {};
HG.Control.BS3.Widgets._CurrentLoadIndex = 0;
HG.Control.BS3.Widgets._Widgets = Array();

HG.Control.BS3.Widgets.MapToLocal = function (module) {
    var localwidget = '';
    var displaywidget = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
    if (displaywidget != null) {
        displaywidget = displaywidget.Value;
        switch (displaywidget) {
            case 'homegenie/generic/light':
            case 'homegenie/generic/dimmer':
            case 'homegenie/generic/switch':
            case 'homegenie/generic/siren':
            case 'homegenie/generic/colorlight':
                localwidget = 'light';
                break;
            case 'homegenie/generic/sensor':
            case 'homegenie/generic/temperature':
            case 'homegenie/generic/sensor':
            case 'homegenie/generic/doorwindow':
                localwidget = 'sensor';
                break;
        }
    }
    if (localwidget == '') {
        switch (module.DeviceType) {
            case 'Light':
            case 'Dimmer':
            case 'Switch':
            case 'Siren':
                localwidget = 'light';
                break;
            case 'Sensor':
            case 'Temperature':
            case 'Thermostat':
                localwidget = 'sensor';
                break;
        }
    }
    return localwidget;
};

HG.Control.BS3.Widgets.LoadAll = function (modules, callback) {
    var midx = HG.Control.BS3.Widgets._CurrentLoadIndex;
    if (midx < modules.length) {
        var modwidget = HG.Control.BS3.Widgets.MapToLocal(modules[midx]);
        if (modwidget != '') {
            HG.Control.BS3.Widgets.Load(modwidget, function (widgetobj) {
                HG.Control.BS3.Widgets._CurrentLoadIndex++;
                HG.Control.BS3.Widgets.LoadAll(modules, callback);
            });
        }
        else {
            HG.Control.BS3.Widgets._CurrentLoadIndex++;
            HG.Control.BS3.Widgets.LoadAll(modules, callback);
        }
    } else {
        // TODO: loading finished (implement loading dialog)
        callback();
    }
};

HG.Control.BS3.Widgets.GetWidget = function (module, callback) {
    var widget = HG.Control.BS3.Widgets.MapToLocal(module);
    if (widget != '') {
        HG.Control.BS3.Widgets.Load(widget, function (widgetobj) {
            callback(widgetobj);
        });
    }
    else {
        callback(null);
    }
};

HG.Control.BS3.Widgets.Load = function (widgetname, callback) {
    var widgetobj = null;
    var found = false;
    //
    for (var w = 0; w < HG.Control.BS3.Widgets._Widgets.length; w++) {
        var widget = HG.Control.BS3.Widgets._Widgets[w];
        if (widget.Name == widgetname) {
            widgetobj = widget;
            found = true;
            break;
        }
    }
    //
    if (!found) {
        $.get('widgets/' + widgetname + '.html', function (data) {
            var WidgetClass = function (w, d) {
                var _self = this;
                this.Name = w;
                this.Markup = d;
                this.RendererClass = null;
                this.CreateInstance = function (module, element) {
                    var instance = eval(_self.RendererClass)[0];
                    instance.Context = module;
                    instance.UiElement = element;
                    element.append(_self.Markup);
                    return instance;
                }
            };
            widgetobj = new WidgetClass(widgetname, data);
            $.get('widgets/' + widgetname + '.json', function (data) {
                widgetobj.RendererClass = data;
                HG.Control.BS3.Widgets._Widgets.push(widgetobj);
                callback(widgetobj);
            }).fail(function () {
                callback(null);
            });
        }).fail(function () {
            alert("error");
            callback(null);
        });
    }
    else {
        callback(widgetobj);
    }
};

HG.Control.BS3.Groups.RenderModules = function (groupname) {

    HG.Control.BS3.Groups._CurrentGroup = groupname;
    $('#hg_group_title').html(groupname);
    $('#mainpanel').empty();
    //
    var groupmodules = HG.Configure.Groups.GetGroupModules(groupname).Modules;
    var groupelement = $('<ul class="media-list" />');
    for (var m = 0; m < groupmodules.length; m++) {
        var module = groupmodules[m];
        //
        HG.Control.BS3.Widgets.GetWidget(module, function (w) {
            if (w != null) {
                var element = $('<li class="media" style="border-bottom:solid 3px #f1f1f1;" data-toggle="collapse" data-target=".nav-collapse" />');
                module.RenderClass = w.CreateInstance(module, element);
                module.RenderClass.GroupName = groupname;
                groupelement.append(element);
            }
        });
    }
    //
    $('#mainpanel').append(groupelement);
    $('#mainpanel .make-switch').bootstrapSwitch();
    $('#mainpanel .slider').slider();
    //
    HG.Control.BS3.Groups.RefreshGroupView(groupname, null);
};

HG.Control.BS3.Service.Call = function(fn, domain, nodeid, fnopt) // TODO: implement callback
{
    var _this = this;
	$.get('/' + HG.WebApp.Data.ServiceKey + '/' + domain + '/' + nodeid + '/' + fn + '/' + fnopt + '/' + (new Date().getTime()), function (data) 
	{
	    HG.Control.BS3.Groups.RefreshGroupView(HG.Control.BS3.Groups._CurrentGroup, function () {

	        if (HG.Control.BS3._GroupsRefreshTimeout != null) {
	            clearTimeout(HG.Control.BS3._GroupModulesRefreshInterval);
	        }
	        HG.Control.BS3._GroupsRefreshTimeout = setTimeout(function () {
	            HG.Control.BS3.Groups.RefreshGroupView(HG.Control.BS3.Groups._CurrentGroup, null);
	        }, 5000);
        
        });

	});
};

HG.Control.BS3.Groups.RefreshGroupView = function(groupname, callback)
{
    HG.Configure.Groups.ModulesList(groupname, function (modules) {
            
        for (var m = 0; m < modules.length; m++) {
            var module = HG.WebApp.Utility.GetModuleByDomainAddress(modules[m].Domain, modules[m].Address);
            module.Properties = modules[m].Properties;
            if (typeof module.RenderClass != 'undefined') module.RenderClass.RenderView();
        }

        if (callback != null) callback();

    });
}

HG.Control.BS3.Macros.ProgramRun = function(pid, options)
{

    HG.Automation.Programs.Run(pid, options, function(res){
        if (HG.Control.BS3._GroupsRefreshTimeout != null) {
            clearTimeout(HG.Control.BS3._GroupModulesRefreshInterval);
        }
        HG.Control.BS3._GroupsRefreshTimeout = setTimeout(function () {
            HG.Control.BS3.Groups.RefreshGroupView(HG.Control.BS3.Groups._CurrentGroup, null);
        }, 5000);
    });

};

HG.Control.BS3.ClockRun = function () 
{
    var today = new Date();
    var h = today.getHours();
    var m = today.getMinutes();
    var s = today.getSeconds();
    // add a zero in front of numbers<10
    h = h < 10 ? "0" + h : h;
    m = m < 10 ? "0" + m : m;
    s = s < 10 ? "0" + s : s;
    $('#hg-clock').html(h + ":" + m + ":" + s);
    t = setTimeout(function () { HG.Control.BS3.ClockRun() }, 500);
};
