HG.WebApp.GroupModules = HG.WebApp.GroupModules || {};
HG.WebApp.GroupModules.PageId = 'page_configure_groupmodules';
HG.WebApp.GroupModules.CurrentGroup = '(none)';
HG.WebApp.GroupModules.CurrentModule = null;
HG.WebApp.GroupModules.CurrentModuleProperty = null;
HG.WebApp.GroupModules.EditModule = Array();
HG.WebApp.GroupModules.SeparatorItemDomain = 'HomeGenie.UI.Separator';

HG.WebApp.GroupModules.InitializePage = function () {
    var page = $('#'+HG.WebApp.GroupModules.PageId);
    page.on('pageinit', function (e) {

        $('#module_add_button').bind('click', function (event) {
            var selectedopt = $('#automation_group_moduleadd').find(":selected");
            var domain = selectedopt.attr('data-context-domain');
            var address = selectedopt.attr('data-context-value');
            HG.WebApp.GroupModules.AddGroupModule(HG.WebApp.GroupModules.CurrentGroup, domain, address, function () {
                var item = $('#page_configure_groupmodules_list ul li').last();
                $('#page_configure_groupmodules_list ul').parent().animate({scrollTop: item.position().top});
                /*
                var availHeight = $(window).height()-item.height()-50;
                $.mobile.silentScroll(item.position().top-availHeight);
                HG.WebApp.GroupModules.EditCurrentModule(item);
                $.mobile.loading('show');
                setTimeout("$('#automation_group_module_edit').popup('open');$.mobile.loading('hide');", 1000);
                */
            });
        });
        //
        $('#group_delete_button').bind('click', function (event) {
            HG.WebApp.GroupModules.DeleteGroup(HG.WebApp.GroupModules.CurrentGroup);
        });
        //
        $('#module_options_button').bind('click', function (event) {
            HG.WebApp.GroupModules.ShowModuleOptions(HG.WebApp.GroupModules.CurrentModule.Domain, HG.WebApp.GroupModules.CurrentModule.Address);
        });
        //
        $('#btn_configure_group_deletegroup').bind('click', function (event) {
            HG.WebApp.Utility.SwitchPopup('#listmodules_actionmenu', '#automation_group_delete');
        });
        $('#btn_configure_group_addmodule').bind('click', function (event) {
            HG.WebApp.Utility.SwitchPopup('#listmodules_actionmenu', '#automation_group_modulechoose');
        });
        $('#btn_configure_group_addseparator').bind('click', function (event) {
            HG.WebApp.GroupModules.EditModule.Domain = '';
            HG.WebApp.GroupModules.EditModule.Address = '';
            HG.WebApp.Utility.SwitchPopup('#listmodules_actionmenu', '#automation_group_separator_edit');
        });
        $('#btn_configure_group_editseparatorremove').bind('click', function (event) {
            HG.WebApp.GroupModules.DeleteGroupModule(HG.WebApp.GroupModules.CurrentGroup, HG.WebApp.GroupModules.CurrentModule);
            HG.WebApp.GroupsList.SaveGroups(null);
        });
        $('#btn_configure_group_editseparatoradd').bind('click', function (event) {
            var label = $('#automation_group_separatorlabel').val().trim();
            if (label != '') {
                HG.WebApp.GroupModules.AddGroupModule(HG.WebApp.GroupModules.CurrentGroup, HG.WebApp.GroupModules.SeparatorItemDomain, label, function () {
                    var item = $('#page_configure_groupmodules_list ul li').last();
                    $('#page_configure_groupmodules_list ul').parent().animate({scrollTop: item.position().top}, 'slow');
                });
            }
        });
        //
        $('#automation_group_separator_edit').on('popupbeforeposition', function (event) {
            if (HG.WebApp.GroupModules.EditModule.Address != '') {
                $('#automation_group_separatorlabel').val(HG.WebApp.GroupModules.EditModule.Address);
                $('#automation_group_separatorlabel').addClass('ui-disabled');
                $('#btn_configure_group_editseparatoradd').hide();
                $('#btn_configure_group_editseparatorremove').show();
            }
            else {
                $('#automation_group_separatorlabel').val('');
                $('#automation_group_separatorlabel').removeClass('ui-disabled');
                $('#btn_configure_group_editseparatoradd').show();
                $('#btn_configure_group_editseparatorremove').hide();
            }
        });
        //
        $('#automation_group_modulechoose').on('popupbeforeposition', function (event) {
            $('#automation_group_moduleadd').empty();
            $('#automation_group_moduleadd').append(HG.WebApp.GroupModules.GetModulesListViewItems(HG.WebApp.GroupModules.CurrentGroup));
            $('#automation_group_moduleadd').selectmenu('refresh');
        });
        //
        $('#page_configure_groupmodules_propspopup').on('popupbeforeposition', function (event) {
            $('#automation_group_module_params').scrollTop(0);
            HG.WebApp.GroupModules.LoadModuleParameters();
        });
        //
        $('#automation_group_module_propdelete').bind('click', function () {
            if (HG.WebApp.GroupModules.CurrentModuleProperty != null) {
                HG.WebApp.GroupModules.ModulePropertyDelete(HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').first().val());
                HG.WebApp.GroupModules.CurrentModuleProperty.remove();
                HG.WebApp.GroupModules.CurrentModuleProperty = null;
            }
        });
        //
        $('#groupmodules_groupname').change(function () {
            HG.Configure.Groups.RenameGroup('Control', HG.WebApp.GroupModules.CurrentGroup, $('#groupmodules_groupname').val(), function (res) {
                HG.WebApp.GroupModules.CurrentGroup = $('#groupmodules_groupname').val();
                $("#configure_groupslist").attr('selected-group-name', $('#groupmodules_groupname').val());
                $.mobile.loading('show');
                HG.Configure.Modules.List(function (data) {
                    try {
                        HG.WebApp.Data.Modules = eval(data);
                    } catch (e) { }
                    HG.Automation.Programs.List(function () {
                        HG.WebApp.GroupsList.LoadGroups();
                        $.mobile.loading('hide');
                    });
                });
            });
        });

        $('#groupmodules_groupwall').on('change', function(){
            if ($(this).val()=='')
                $('#configure_group_wallpaper').css('background-image', '');
            else
                $('#configure_group_wallpaper').css('background-image', 'url(images/wallpapers/'+$(this).val()+')');
            HG.Configure.Groups.WallpaperSet(HG.WebApp.GroupModules.CurrentGroup, $(this).val(), function() { });
        });

        $('#groupmodules_group_deletebtn').on('click', function(){
            $.mobile.loading('show');
            HG.Configure.Groups.WallpaperDelete($('#groupmodules_groupwall').val(), function(){
                var group = HG.Configure.Groups.GetGroupByName(HG.WebApp.GroupModules.CurrentGroup);
                group.Wallpaper = '';
                HG.WebApp.GroupModules.RefreshWallpaper();
                $.mobile.loading('hide');
            });
        });
        $('#groupmodules_group_uploadbtn').on('click', function(){
            $('#groupmodules_group_uploadfile').click();
        });
    });
    page.on('pagebeforeshow', function(){
        HG.WebApp.GroupModules.LoadGroupModules();
        $.mobile.loading('show');
        HG.WebApp.GroupModules.RefreshWallpaper();
    });
    $('#groupmodules_group_uploadfile').fileupload({
        url: '/api/HomeAutomation.HomeGenie/Config/Groups.WallpaperAdd',
        dropZone: $('[data-upload-dropzone=wallpaper]'),
        progressall: function (e, data) {
            var progress = parseInt(data.loaded / data.total * 100, 10);
            $.mobile.loading('show', { text: 'Saving background... '+progress+'%', textVisible: true, theme: 'a', html: '' });
        },
        start: function(e) {
            $.mobile.loading('show', { text: 'Saving background...', textVisible: true, theme: 'a', html: '' });
        },
        fail: function (e, data) {
            $.mobile.loading('hide');
        },
        done: function (e, data) {
            var wp = data.result.ResponseValue;
            $('#configure_group_wallpaper').css('background-image', 'url(images/wallpapers/'+wp+')');
            HG.Configure.Groups.WallpaperSet(HG.WebApp.GroupModules.CurrentGroup, wp, function() {
                var group = HG.Configure.Groups.GetGroupByName(HG.WebApp.GroupModules.CurrentGroup);
                group.Wallpaper = wp;
                if ($.mobile.activePage.attr('id') == 'page_control')
                    $('div[data-ui-field="wallpaper"]').css('background-image', 'url(images/wallpapers/'+wp+')');
                else
                    HG.WebApp.GroupModules.RefreshWallpaper();
                $.mobile.loading('hide');
            });
        }
    });
};

HG.WebApp.GroupModules.RefreshWallpaper = function() {
    var group = HG.Configure.Groups.GetGroupByName(HG.WebApp.GroupModules.CurrentGroup);
    $('#configure_group_wallpaper').css('background-image', 'url(images/wallpapers/'+group.Wallpaper+')');
    $('#configure_group_wallpaper').css('background-size', '164px 92px');
    $('#groupmodules_groupwall').find('option:gt(0)').remove();
    HG.Configure.Groups.WallpaperList(function(list){
        $.each(list, function(k, v){
            var selected = (group.Wallpaper == v ? ' selected' : '');
            $('#groupmodules_groupwall').append('<option value="'+v+'"'+selected+'>'+v+'</option>');
        });
        $('#groupmodules_groupwall').selectmenu('refresh');
        $.mobile.loading('hide');
    });
};

HG.WebApp.GroupModules.SetModuleIcon = function (img) {
    var icon = $(img).attr('src');
    $('#module_icon').attr('src', icon);
    HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.EditModule, 'Widget.DisplayIcon', icon);
    var iconprop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.EditModule, 'Widget.DisplayIcon');
    iconprop.NeedsUpdate = 'true';
    $('#configure_module_iconslist').hide(200);
};

HG.WebApp.GroupModules.ModuleIconsToggle = function () {

    if (HG.WebApp.GroupModules.EditModule.DeviceType.toLowerCase() != 'program') {
        if ($('#configure_module_iconslist').css('display') != 'none') {
            $('#configure_module_iconslist').hide(200);
        }
        else {
            $('#configure_module_iconslist').show(200);
        }
    }

};

HG.WebApp.GroupModules.SortModules = function () {

    var neworder = '';
    $('#page_configure_groupmodules_list ul').children('li').each(function () {
        var midx = $(this).attr('data-module-index');
        if (midx >= 0) {
            neworder += (midx + ';')
        }
    });
    $.mobile.loading('show');
    HG.Configure.Groups.SortModules('Control', HG.WebApp.GroupModules.CurrentGroup, neworder, function (res) {
        HG.Configure.Groups.List('Control', function () {
            HG.WebApp.GroupModules.LoadGroupModules();
            $.mobile.loading('hide');
        });
    });

};

HG.WebApp.GroupModules.ModulePropertyDelete = function (name) {
    var module = HG.WebApp.GroupModules.CurrentModule;
    for (var p = 0; p < module.Properties.length; p++) {

        if (module.Properties[p].Name == name) {
            delete module.Properties[p];
            module.Properties.splice(p, 1);
        }

    }
};

HG.WebApp.GroupModules.ModulePropertyAdd = function (module, name, value) {
    var doesexists = false;
    for (var p = 0; p < module.Properties.length; p++) {
        if (module.Properties[p].Name == name) {
            module.Properties[p].Value = value;
            module.Properties[p].NeedsUpdate = 'true';
            doesexists = true;
            break;
        }
    }
    if (!doesexists) {
        module.Properties.push({ Name: name, Value: value, NeedsUpdate: 'true' });
    }
    return doesexists;
};

HG.WebApp.GroupModules.UpdateModule = function (module, callback) {
    $.mobile.loading('show', { text: 'Saving module settings', textVisible: true, theme: 'a', html: '' });
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Update/',
        data: JSON.stringify(module, function (key, value) { if (key == "WidgetInstance") return undefined; else return value; }),
        success: function (response) {
            $.mobile.loading('hide');
            if (callback != null) {
                callback();
            }
        },
        error: function (a, b, c) {
            $.mobile.loading('hide');
            if (callback != null) {
                callback();
            }
        }
    });
};


HG.WebApp.GroupModules.UpdateCurrentModuleParameter = function () {
    var isNewProp = HG.WebApp.GroupModules.ModulePropertyAdd(HG.WebApp.GroupModules.CurrentModule, HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').first().val(), HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').last().val());
    // TODO: enable custom property adding?
    //if (isNewProp)
    //{
    //    var stop = $('#automation_group_module_params').children().last().offset().top - 314;
    //    $('#automation_group_module_params').animate({ scrollTop: '+=' + stop }, 1000);
    //    $('#automation_group_module_params').children().last().find('input[type=text]').first().focus();
    //    HG.WebApp.GroupModules.LoadModuleParameters();
    //}
};

HG.WebApp.GroupModules.LoadModuleParameters = function () {
    $('#automation_group_module_params').empty();
    //
    var module = HG.WebApp.GroupModules.CurrentModule;
    if (module.Properties != null) {
        for (var p = 0; p < module.Properties.length; p++) {
            var item = '<li>';
            item += '        <div class="ui-grid-a">';
            item += '            <div class="ui-block-a" style="padding-right:7.5px;width:70%;"><input type="text" value="' + module.Properties[p].Name + '" onchange="HG.WebApp.GroupModules.UpdateCurrentModuleParameter()" style="font-size:11pt" -class="ui-disabled" /></div>';
            item += '            <div class="ui-block-b" style="padding-left:7.5px;width:30%;"><input type="text" value="' + module.Properties[p].Value + '" onchange="HG.WebApp.GroupModules.UpdateCurrentModuleParameter()" style="font-size:11pt" -class="ui-disabled" /></div>';
            item += '        </div>';
            item += '   </li>';
            $('#automation_group_module_params').append(item);
        }
    }
    //
    $('#automation_group_module_params').trigger('create');
    $('#automation_group_module_params').listview().listview('refresh');
    $('#automation_group_module_params input').focus(function () {
        var back = $(this).closest('div').parent().parent().parent();
        if (back.css('background') != '#E6E6FA') {
            $(this).attr('originalbackground', back.css('background'));
            back.css('background', '#E6E6FA');
            setTimeout("$('#automation_group_module_propdelete').removeClass('ui-disabled')", 500);
            //            setTimeout("$('#automation_group_module_propsave').removeClass('ui-disabled')", 500);
            HG.WebApp.GroupModules.CurrentModuleProperty = back;
        }
    });
    $('#automation_group_module_params input').blur(function () {
        $(this).closest('div').parent().parent().parent().css('background', $(this).attr('originalbackground'));
        setTimeout("$('#automation_group_module_propdelete').addClass('ui-disabled')", 250);
        //        setTimeout("$('#automation_group_module_propsave').addClass('ui-disabled')", 250);
    });
};

HG.WebApp.GroupModules.GetModulesListViewItems = function (groupname) {
    var groupmodules = HG.Configure.Groups.GetGroupModules(groupname);
    var htmlopt = '';
    var cursect = '';
    if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length) {
        for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
            var module = HG.WebApp.Data.Modules[m];
            var haselement = $.grep(groupmodules.Modules, function (value) {
                return (value.Domain == module.Domain && value.Address == module.Address);
            });
            // module it's not present in current group
            if (haselement.length == 0) {
                var propwidget = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
                var vmparentid = HG.WebApp.Utility.GetModulePropertyByName(module, "VirtualModule.ParentId");
                var widget = (propwidget != null && propwidget.Value != null) ? propwidget.Value : '';
                var vid = (vmparentid != null && vmparentid.Value != null) ? vmparentid.Value : '';
                // check if no explicit witdget is specified and it's not a virtual module or program
                if (module.Domain == 'HomeAutomation.HomeGenie.Automation') {
                    var pid = (vid != '' && vid != module.Address) ? vid : module.Address;
                    var cp = HG.WebApp.Utility.GetProgramByAddress(pid);
                    if (cp != null) {
                        if (!cp.IsEnabled)
                            continue;
                        else if (cp.Type.toLowerCase() != 'wizard' && widget == '')
                            continue;
                    }
                }
                //
                if (cursect != module.Domain) {
                    cursect = module.Domain;
                    htmlopt += '<optgroup label="' + cursect + '"></optgroup>';
                }
                var displayname = (module.Name != '' ? module.Name : (module.Description != '' ? module.Description : module.DeviceType));
                displayname += ' (' + module.Address + ')';
                htmlopt += '<option data-context-domain="' + module.Domain + '" data-context-value="' + module.Address + '">' + displayname + '</option>';
            }
        }
    }
    return htmlopt;
};

HG.WebApp.GroupModules.AddGroupModule = function (group, domain, address, callback) {
    var alreadyexists = false;
    var moduleindex = -1;
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (HG.WebApp.Data.Groups[i].Name == group) {
            for (c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
                if (domain == HG.WebApp.Data.Groups[i].Modules[c].Domain && address == HG.WebApp.Data.Groups[i].Modules[c].Address) {
                    alreadyexists = true;
                    break;
                }
            }
            if (!alreadyexists) {
                HG.WebApp.Data.Groups[i].Modules.push({ 'Address': address, 'Domain': domain });
                moduleindex = HG.WebApp.Data.Groups[i].length - 1;
            }
            //
            break;
        }
    }
    //
    HG.WebApp.GroupsList.SaveGroups(function () {
        callback();
    });
};

HG.WebApp.GroupModules.UpdateWMWatts = function (module, wmwatts) {
    HG.WebApp.GroupModules.EditModule.WMWatts = wmwatts;
};

HG.WebApp.GroupModules.UpdateModuleType = function (type) {
    var mtype = type.toLowerCase();
    //
    $('#module_options_1').css('display', '');
    $('#module_options_2').css('display', '');
    $('#module_options_3').css('display', '');
    $('#module_update_button').removeClass('ui-disabled');
    //
    if (mtype == 'light' || mtype == 'dimmer' || mtype == 'switch') {
        $('#module_vmwatts').val('0');
        $('#module_vmwatts_label').removeClass('ui-disabled');
        $('#module_vmwatts').removeClass('ui-disabled');
    }
    else if (mtype == 'program') {
        $('#module_options_1').css('display', 'none');
        $('#module_options_2').css('display', 'none');
        $('#module_options_3').css('display', 'none');
        $('#module_update_button').addClass('ui-disabled');
    }
    else if (mtype && mtype != undefined && mtype != 'generic' && mtype != '') {
        $('#module_vmwatts').val('');
        $('#module_vmwatts_label').addClass('ui-disabled');
        $('#module_vmwatts').addClass('ui-disabled');
    }
    //
    if (HG.WebApp.GroupModules.EditModule.DeviceType != type) {
        HG.WebApp.GroupModules.EditModule.DeviceType = type;
        HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.EditModule, 'Widget.DisplayIcon', '');
        var iconprop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.EditModule, 'Widget.DisplayIcon');
        iconprop.NeedsUpdate = 'true';
        HG.Ui.GetModuleIcon(HG.WebApp.GroupModules.EditModule, function (icon) {
            $('#module_icon').attr('src', icon);
        });
    }
    //
    HG.WebApp.GroupModules.UpdateFeatures();
};

HG.WebApp.GroupModules.DeleteGroupModule = function (groupname, module) {
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (HG.WebApp.Data.Groups[i].Name == groupname) {
            HG.WebApp.Data.Groups[i].Modules = $.grep(HG.WebApp.Data.Groups[i].Modules, function (value) {
                return value.Domain != module.Domain || value.Address != module.Address;
            });
            break;
        }
    }
};

HG.WebApp.GroupModules.DeleteGroup = function (group) {
    $.mobile.loading('show');
    HG.Configure.Groups.DeleteGroup('Control', group, function () {
        $.mobile.loading('hide');
        setTimeout(function () {
            $.mobile.changePage($('#page_configure_groups'));
        }, 200);
    });
    $('#control_groupslist').empty();
    HG.WebApp.Data._CurrentGroupIndex = 0;
};

HG.WebApp.GroupModules.LoadGroupModules = function () {
    HG.WebApp.GroupModules.CurrentGroup = $("#configure_groupslist").attr('selected-group-name');
    //
    var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.GroupModules.CurrentGroup);
    //
    $('#groupmodules_groupname').val(groupmodules.Name);
    //
    if ($('#page_configure_groupmodules_list ul').hasClass('ui-sortable')) {
        $('#page_configure_groupmodules_list ul').sortable('destroy');
        $('#page_configure_groupmodules_list ul').off('sortstop');
    }
    $('#page_configure_groupmodules_list ul').empty();
    //
    var html = '';
    for (var m = 0; m < groupmodules.Modules.length; m++) {
        var domain_label = groupmodules.Modules[m].Domain.substring(groupmodules.Modules[m].Domain.lastIndexOf('.') + 1);

        if (groupmodules.Modules[m].Domain == HG.WebApp.GroupModules.SeparatorItemDomain) {
            html += '<li class="ui-header" data-icon="false" data-module-index="' + m + '">';
            html += '<a style="height:25px;line-height:24px;font-size:12pt;font-weight:bold">&nbsp;' + groupmodules.Modules[m].Address + '</a>';
            html += '<div class="ui-grid-a" style="position:absolute;right:0;top:1px;">';
            html += '<div class="ui-block-a"><a class="handle ui-btn ui-icon-fa-sort ui-btn-icon-notext ui-list-btn-option-mini">' + HG.WebApp.Locales.GetLocaleString('configure_module_parameters_linktitle') + '</a></div>';
            html += '<div class="ui-block-b"><a data-ui-field="btn_delete" class="ui-btn ui-icon-delete ui-btn-icon-notext ui-list-btn-option-mini">' + HG.WebApp.Locales.GetLocaleString('configure_module_parameters_linktitle') + '</a></div>';
            html += '</div>';
            html += '</li>';
        } else {
            var iconid = 'module_icon_image_' + m;
            var icon = 'pages/control/widgets/homegenie/generic/images/unknown.png';
            html += '<li data-icon="false" data-module-index="' + m + '">';
            html += '<a>';
            html += '<table><tr><td rowspan="2" align="left"><img id="' + iconid + '" height="54" src="' + icon + '"></td>';
            html += '<td style="padding-left:10px"><span>' + groupmodules.Modules[m].Name + '</span></td></tr>';
            html += '<tr><td style="padding-left:10px"><span style="color:gray">' + domain_label + '</span> ' + groupmodules.Modules[m].Address + '</td>';
            html += '</tr></table>';
            html += '</a>';
            html += '<div class="ui-grid-c" style="position:absolute;right:0;top:0;height:100%;">';
            html += '<div class="ui-block-a"><a data-ui-field="btn_settings" title="Settings" data-rel="popup" data-transition="pop" class="ui-btn ui-icon-gear ui-btn-icon-notext ui-list-btn-option"></a></div>';
            html += '<div class="ui-block-b"><a data-ui-field="btn_parameters" title="Parameters" data-rel="popup" data-transition="pop" class="ui-btn ui-icon-bars ui-btn-icon-notext ui-list-btn-option">' + HG.WebApp.Locales.GetLocaleString('configure_module_parameters_linktitle') + '</a></div>';
            html += '<div class="ui-block-c"><a title="Drag to sort" class="handle ui-btn ui-icon-fa-sort ui-btn-icon-notext ui-list-btn-option"></a></div>';
            html += '<div class="ui-block-d"><a data-ui-field="btn_delete" title="Remove from this group" class="ui-btn ui-icon-delete ui-btn-icon-notext ui-list-btn-option"></a></div>';
            html += '</div>';
            html += '</li>';
        }
    }
    $('#page_configure_groupmodules_list ul').append(html);
    $('#page_configure_groupmodules_list ul').listview().listview('refresh');
    $('#page_configure_groupmodules_list ul').sortable({ handle : '.handle', axis: 'y', scrollSpeed: 10 });
    $('#page_configure_groupmodules_list ul').on('sortstop', function (event, ui) {
        HG.WebApp.GroupModules.SortModules();
    });
    // udate icons asynchronously
    for (var m = 0; m < groupmodules.Modules.length; m++) {
        var iconid = 'module_icon_image_' + m;
        HG.Ui.GetModuleIcon(groupmodules.Modules[m], function (iconimage, elid) {
            $('#' + elid).attr('src', iconimage);
        }, iconid);
    }
    // set on click handler for list items
    $("#page_configure_groupmodules_list ul li").each(function (index) {
        var item = $(this);
        item.find('[data-ui-field=btn_settings]').on('click', function () {
            HG.WebApp.SetCurrentModule(item);
            HG.WebApp.GroupModules.EditCurrentModule(item);
        });
        item.find('[data-ui-field=btn_parameters]').on('click', function () {
            HG.WebApp.SetCurrentModule(item);
            $('#page_configure_groupmodules_propspopup').popup('open');
        });
        item.find('[data-ui-field=btn_delete]').on('click', function (event) {
            HG.WebApp.SetCurrentModule(item);
            HG.WebApp.GroupModules.DeleteGroupModule(HG.WebApp.GroupModules.CurrentGroup, HG.WebApp.GroupModules.CurrentModule);
            HG.WebApp.GroupsList.SaveGroups(null);
        });
    });
    $("#configure_groupslist").listview().listview("refresh");
};

HG.WebApp.SetCurrentModule = function (item) {
    var m = item.attr('data-module-index');
    if (m) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.GroupModules.CurrentGroup);
        HG.WebApp.GroupModules.CurrentModule = HG.WebApp.Utility.GetModuleByDomainAddress(groupmodules.Modules[m].Domain, groupmodules.Modules[m].Address);
        if (HG.WebApp.GroupModules.CurrentModule == null) {
            // module not found, pheraps it was removed
            // so we return the data in the group module reference (address and domain only)
            HG.WebApp.GroupModules.CurrentModule = groupmodules.Modules[m];
            HG.WebApp.GroupModules.CurrentModule.DeviceType = '';
        }
    }
};

HG.WebApp.GroupModules.EditCurrentModule = function (item) {

    HG.WebApp.SetCurrentModule(item);
    //$("#configure_groupslist").attr('selected-module-index', item.attr('data-module-index'));
    if (HG.WebApp.GroupModules.CurrentModule.Domain == HG.WebApp.GroupModules.SeparatorItemDomain) {
        HG.WebApp.GroupModules.EditModule.Address = HG.WebApp.GroupModules.CurrentModule.Address;
        HG.WebApp.GroupModules.EditModule.Domain = HG.WebApp.GroupModules.CurrentModule.Domain;
        $('#automation_group_separator_edit').popup('open', { transition: 'pop', positionTo: 'window' });
    } else {
        HG.WebApp.GroupModules.ModuleEdit(function () {
            // module updated callback
            $('#control_groupslist').empty();
            HG.WebApp.GroupModules.LoadGroupModules();
        });
    }

};

HG.WebApp.GroupModules.ModuleEdit = function (callback) {

    HG.WebApp.GroupModules.ModuleUpdatedCallback = callback;
    HG.WebApp.GroupModules.EditModule.Domain = HG.WebApp.GroupModules.CurrentModule.Domain;
    HG.WebApp.GroupModules.EditModule.Address = HG.WebApp.GroupModules.CurrentModule.Address;
    HG.WebApp.GroupModules.EditModule.Name = HG.WebApp.GroupModules.CurrentModule.Name;
    HG.WebApp.GroupModules.EditModule.Type = HG.WebApp.GroupModules.CurrentModule.Type;
    HG.WebApp.GroupModules.EditModule.DeviceType = HG.WebApp.GroupModules.CurrentModule.DeviceType;
    //
    HG.WebApp.GroupModules.EditModule.WMWatts = 0;
    if (HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualMeter.Watts") != null) {
        HG.WebApp.GroupModules.EditModule.WMWatts = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualMeter.Watts").Value;
    }
    //
    // disable option button if it's a virtual module
    $('#module_options_button').removeClass('ui-disabled');
    if (HG.WebApp.GroupModules.CurrentModule.Domain != 'HomeAutomation.ZWave') {
        $('#module_options_button').addClass('ui-disabled');
    }
    else if (HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualModule.ParentId") != null) {
        var parentid = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualModule.ParentId").Value;
        if (parentid != HG.WebApp.GroupModules.CurrentModule.Address) {
            $('#module_options_button').addClass('ui-disabled');
        }
    }
    //
    HG.WebApp.GroupModules.UpdateModuleType(HG.WebApp.GroupModules.CurrentModule.DeviceType);
    //
    $('#module_title').html(HG.WebApp.GroupModules.EditModule.Domain.split('.')[1] + ' ' + HG.WebApp.GroupModules.EditModule.Address + ' - Settings');
    $('#module_name').val(HG.WebApp.GroupModules.EditModule.Name);
    $('#module_type').val(HG.WebApp.GroupModules.EditModule.DeviceType).attr('selected', true).siblings('option').removeAttr('selected');
    $('#module_type').selectmenu('refresh', true);
    //
    $('#configure_module_iconslist').hide();
    HG.Ui.GetModuleIcon(HG.WebApp.GroupModules.CurrentModule, function (icon) {
        $('#module_icon').attr('src', icon);
    });
    $('#module_vmwatts').val(HG.WebApp.GroupModules.EditModule.WMWatts > 0 ? HG.WebApp.GroupModules.EditModule.WMWatts : '0');
    //
    HG.WebApp.GroupModules.UpdateFeatures();
    //
    $('#automation_group_module_edit').popup('open', { transition: 'pop', positionTo: 'window' });

};

HG.WebApp.GroupModules.ShowFeatures = function (programid) {
    $('#module_programs_features').empty();
    var desc = HG.WebApp.Locales.GetProgramLocaleString(HG.WebApp.Data.Programs[programid].Address, 'Description', HG.WebApp.Data.Programs[programid].Description);
    $('#module_programs_featuredesc').html(desc);
    for (var p = 0; p < HG.WebApp.GroupModules.EditModule.Properties.length; p++) {
        var mp = HG.WebApp.GroupModules.EditModule.Properties[p];
        if (mp.ProgramIndex == programid) {
            mp.Index = p;
            var context = {
                parent: $('#module_programs_features'),
                program: HG.WebApp.Data.Programs[programid],
                module: HG.WebApp.GroupModules.EditModule,
                parameter: mp
            };
            var featureField = HG.Ui.GenerateWidget('widgets/'+mp.FieldType, context, function(handler){
                handler.onChange = function(val){
                    HG.WebApp.GroupModules.FeatureUpdate(handler.context, val);
                };
                $('#automation_group_module_edit').popup("reposition", { positionTo: 'window' });
            });
        }
    }
};

HG.WebApp.GroupModules.UpdateFeatures = function () {
    HG.WebApp.GroupModules.EditModule.Properties = Array(); // used to store "features" values
    //
    $('#module_options_features').hide();
    $('#module_programs_featureset').empty();
    $('#module_programs_features').empty();
    //
    var featureset = '';
    var selected = -1;
    for (var p = 0; p < HG.WebApp.Data.Programs.length; p++) {
        var cprogram = -1;
        if (!HG.WebApp.Data.Programs[p].IsEnabled) continue;
        var features = HG.WebApp.Data.Programs[p].Features;
        if (features.length > 0) {
            for (var f = 0; f < features.length; f++) {
                var fd = ',' + features[f].ForDomains.toLowerCase() + ',|' + features[f].ForDomains.toLowerCase() + '|';
                var fs = ',' + features[f].ForTypes.toLowerCase() + ',|' + features[f].ForTypes.toLowerCase() + '|';
                var featurematch = (features[f].ForDomains == '' || fd.indexOf(HG.WebApp.GroupModules.EditModule.Domain.toLowerCase(), 0) >= 0) && (features[f].ForTypes == '' || fs.indexOf(HG.WebApp.GroupModules.EditModule.DeviceType.toLowerCase(), 0) >= 0);
                if (featurematch) {
                    var property = features[f].Property;
                    var prop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, property);
                    HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.EditModule, property, (prop != null ? prop.Value : ""));
                    prop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.EditModule, property);
                    prop.ProgramIndex = p;
                    prop.FieldType = features[f].FieldType;
                    prop.Description = features[f].Description;
                    //
                    if (cprogram < 0) {
                        var address = HG.WebApp.Data.Programs[p].Address;
                        var pname = HG.WebApp.Data.Programs[p].Name;
                        if (pname == '') pname = address;
                        pname = HG.WebApp.Locales.GetProgramLocaleString(address, 'Title', pname);
                        featureset += '<option value="' + p + '">' + pname + '</option>';
                        cprogram = p;
                        if (selected < 0) selected = p;
                    }
                }
            }
        }
    }
    //
    if (featureset != '') {
        $('#module_programs_featureset').append(featureset);
        $('#module_programs_featureset').selectmenu('refresh', true);
        $('#module_options_features').show();
        //
        if (selected != -1) {
            HG.WebApp.GroupModules.ShowFeatures(selected);
        }
    }
    $('#automation_group_module_edit').popup("reposition", { positionTo: 'window' });
};

HG.WebApp.GroupModules.FeatureUpdate = function (context, value) {
    var program = context.program;
    var module = context.module;
    var property = context.parameter.Name;
    var mp = HG.WebApp.Utility.GetModulePropertyByName(module, property);
    HG.WebApp.Utility.SetModulePropertyByName(module, property, value);
    // postback value change to the automation program
    HG.Automation.Programs.PostBack('ui/feature', program, module, property, value, function(res){
        // TODO: think about how the response could be used (eg. field value validation and such)
    });
    //property.Changed = true;
};

HG.WebApp.GroupModules.ShowModuleOptions = function (domain, address) {
    var module = HG.WebApp.Utility.GetModuleByDomainAddress(domain, address);
    //
    if (module != null) {
        switch (module.Domain) {
            case 'HomeAutomation.ZWave':
                HG.Ext.ZWave.NodeSetup.Show(module);
                $.mobile.changePage($('#configurepage_OptionZWave'), { transition: "slide" });
                break;
                //case 'HomeAutomation.X10':
                //    $.mobile.changePage($('#configurepage_OptionX10'), { transition: "slide" });
                //    break;
            default:
                alert('No options page available for this module.');
        }
    }
};