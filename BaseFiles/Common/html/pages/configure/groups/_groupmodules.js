HG.WebApp.GroupModules = HG.WebApp.GroupModules || new function () { var $$ = this;

    $$.PageId = 'page_configure_groupmodules';
    $$.CurrentGroup = '(none)';
    $$.CurrentModule = null;
    $$.CurrentModuleProperty = null;
    $$.EditModule = [];
    $$.SeparatorItemDomain = 'HomeGenie.UI.Separator';

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        page.on('pageinit', function (e) {
            $('#module_add_button').on('click', function (event) {
                var selectedopt = $('#automation_group_moduleadd').find(":selected");
                var domain = selectedopt.attr('data-context-domain');
                var address = selectedopt.attr('data-context-value');
                $$.AddGroupModule($$.CurrentGroup, domain, address, function () {
                    var list = $('#page_configure_groupmodules_list ul');
                    var item = list.find('li').last();
                    list.parent().animate({scrollTop: item.position().top});
                    /*
                     var availHeight = $(window).height()-item.height()-50;
                     $.mobile.silentScroll(item.position().top-availHeight);
                     $$.EditCurrentModule(item);
                     $.mobile.loading('show');
                     setTimeout("$('#automation_group_module_edit').popup('open');$.mobile.loading('hide');", 1000);
                     */
                });
            });
            $('#group_delete_button').on('click', function (event) {
                $$.DeleteGroup($$.CurrentGroup);
            });
            $('#btn_configure_group_deletegroup').on('click', function (event) {
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_delete');
            });
            $('#btn_configure_group_addmodule').on('click', function (event) {
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_modulechoose');
            });
            $('#btn_configure_group_addseparator').on('click', function (event) {
                $$.EditModule.Domain = '';
                $$.EditModule.Address = '';
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_separator_edit');
            });
            $('#btn_configure_group_editseparatoradd').on('click', function (event) {
                var label = $('#automation_group_separatorlabel').val().trim();
                if (label != '') {
                    $$.AddGroupModule($$.CurrentGroup, $$.SeparatorItemDomain, label, function () {
                        var item = $('#page_configure_groupmodules_list ul li').last();
                        $('#page_configure_groupmodules_list ul').parent().animate({scrollTop: item.position().top}, 'slow');
                    });
                }
            });
            $('#automation_group_separator_edit').on('popupbeforeposition', function (event) {
                if ($$.EditModule.Address != '') {
                    $('#automation_group_separatorlabel').val($$.EditModule.Address);
                    $('#automation_group_separatorlabel').addClass('ui-disabled');
                    $('#btn_configure_group_editseparatoradd').hide();
                }
                else {
                    $('#automation_group_separatorlabel').val('');
                    $('#automation_group_separatorlabel').removeClass('ui-disabled');
                    $('#btn_configure_group_editseparatoradd').show();
                }
            });
            $('#automation_group_modulechoose').on('popupbeforeposition', function (event) {
                var moduleAdd = $('#automation_group_moduleadd');
                moduleAdd.empty();
                moduleAdd.append($$.GetModulesListViewItems($$.CurrentGroup));
                moduleAdd.selectmenu('refresh');
            });
            $('#automation_group_module_propdelete').on('click', function () {
                if ($$.CurrentModuleProperty != null) {
                    $$.ModulePropertyDelete($$.CurrentModuleProperty.find('input[type=text]').first().val());
                    $$.CurrentModuleProperty.remove();
                    $$.CurrentModuleProperty = null;
                }
            });
            var groupName = $('#groupmodules_groupname');
            groupName.change(function () {
                HG.Configure.Groups.RenameGroup('Control', $$.CurrentGroup, groupName.val(), function (res) {
                    $$.CurrentGroup = groupName.val();
                    $("#configure_groupslist").attr('selected-group-name', groupName.val());
                    $.mobile.loading('show');
                    HG.Configure.Modules.List(function (data) {
                        try {
                            HG.WebApp.Data.Modules = eval(data);
                        } catch (e) {
                        }
                        HG.Automation.Programs.List(function () {
                            HG.WebApp.GroupsList.LoadGroups();
                            $.mobile.loading('hide');
                        });
                    });
                });
            });
            $('#groupmodules_groupwall').on('change', function () {
                if ($(this).val() == '')
                    $('#configure_group_wallpaper').css('background-image', '');
                else
                    $('#configure_group_wallpaper').css('background-image', 'url(images/wallpapers/' + $(this).val() + ')');
                HG.Configure.Groups.WallpaperSet($$.CurrentGroup, $(this).val(), function () {
                });
            });
            $('#groupmodules_group_deletebtn').on('click', function () {
                $.mobile.loading('show');
                HG.Configure.Groups.WallpaperDelete($('#groupmodules_groupwall').val(), function () {
                    var group = HG.Configure.Groups.GetGroupByName($$.CurrentGroup);
                    group.Wallpaper = '';
                    $$.RefreshWallpaper();
                    $.mobile.loading('hide');
                });
            });
            $('#groupmodules_group_uploadbtn').on('click', function () {
                $('#groupmodules_group_uploadfile').click();
            });
            $$.wallpaper = $('#configure_group_wallpaper');
        });
        page.on('pagebeforeshow', function () {
            $('#page_configure_groupmodules_list ul').parent().animate({scrollTop: 0});
            $$.LoadGroupModules();
            $.mobile.loading('show');
            $$.RefreshWallpaper();
        });
        $('#groupmodules_group_uploadfile').fileupload({
            url: '/api/HomeAutomation.HomeGenie/Config/Groups.WallpaperAdd',
            dropZone: $('[data-upload-dropzone=wallpaper]'),
            progressall: function (e, data) {
                var progress = parseInt(data.loaded / data.total * 100, 10);
                $.mobile.loading('show', {
                    text: 'Saving background... ' + progress + '%',
                    textVisible: true,
                    theme: 'a',
                    html: ''
                });
            },
            start: function (e) {
                $.mobile.loading('show', {text: 'Saving background...', textVisible: true, theme: 'a', html: ''});
            },
            fail: function (e, data) {
                $.mobile.loading('hide');
            },
            done: function (e, data) {
                var wp = data.result.ResponseValue;
                $('#configure_group_wallpaper').css('background-image', 'url(images/wallpapers/' + wp + ')');
                HG.Configure.Groups.WallpaperSet($$.CurrentGroup, wp, function () {
                    var group = HG.Configure.Groups.GetGroupByName($$.CurrentGroup);
                    group.Wallpaper = wp;
                    if ($.mobile.activePage.attr('id') == 'page_control')
                        $('div[data-ui-field="wallpaper"]').css('background-image', 'url(images/wallpapers/' + wp + ')');
                    else
                        $$.RefreshWallpaper();
                    $.mobile.loading('hide');
                });
            }
        });
    };

    $$.RefreshWallpaper = function () {
        var group = HG.Configure.Groups.GetGroupByName($$.CurrentGroup);
        $$.wallpaper.css('background-image', 'url(images/wallpapers/' + group.Wallpaper + ')');
        $$.wallpaper.css('background-size', '164px 92px');
        $('#groupmodules_groupwall').find('option:gt(0)').remove();
        HG.Configure.Groups.WallpaperList(function (list) {
            $.each(list, function (k, v) {
                var selected = (group.Wallpaper == v ? ' selected' : '');
                $('#groupmodules_groupwall').append('<option value="' + v + '"' + selected + '>' + v + '</option>');
            });
            $('#groupmodules_groupwall').selectmenu('refresh');
            $.mobile.loading('hide');
        });
    };

    $$.SetModuleIcon = function (img) {
        var icon = $(img).attr('src');
        $('#module_icon').attr('src', icon);
        HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon', icon);
        var iconprop = HG.WebApp.Utility.GetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon');
        iconprop.NeedsUpdate = 'true';
        $('#configure_module_iconslist').hide(200);
    };

    $$.ModuleIconsToggle = function () {

        if ($$.EditModule.DeviceType.toLowerCase() != 'program') {
            if ($('#configure_module_iconslist').css('display') != 'none') {
                $('#configure_module_iconslist').hide(200);
            }
            else {
                $('#configure_module_iconslist').show(200);
            }
        }

    };

    $$.SortModules = function () {

        var neworder = '';
        $('#page_configure_groupmodules_list ul').children('li').each(function () {
            var midx = $(this).attr('data-module-index');
            if (midx >= 0) {
                neworder += (midx + ';')
            }
        });
        $.mobile.loading('show');
        HG.Configure.Groups.SortModules('Control', $$.CurrentGroup, neworder, function (res) {
            HG.Configure.Groups.List('Control', function () {
                $$.LoadGroupModules();
                $.mobile.loading('hide');
            });
        });

    };

    $$.ModulePropertyDelete = function (name) {
        var module = $$.CurrentModule;
        for (var p = 0; p < module.Properties.length; p++) {

            if (module.Properties[p].Name == name) {
                delete module.Properties[p];
                module.Properties.splice(p, 1);
            }

        }
    };

    $$.ModulePropertyAdd = function (module, name, value) {
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
            module.Properties.push({Name: name, Value: value, NeedsUpdate: 'true'});
        }
        return doesexists;
    };

    $$.UpdateModule = function (module, callback) {
        $.mobile.loading('show', {text: 'Saving module settings', textVisible: true, theme: 'a', html: ''});
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Update/',
            data: JSON.stringify(module, function (key, value) {
                if (key == "WidgetInstance") return undefined; else return value;
            }),
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

    $$.UpdateCurrentModuleParameter = function () {
        var isNewProp = $$.ModulePropertyAdd($$.CurrentModule, $$.CurrentModuleProperty.find('input[type=text]').first().val(), $$.CurrentModuleProperty.find('input[type=text]').last().val());
        // TODO: enable custom property adding?
        //if (isNewProp)
        //{
        //    var stop = $('#automation_group_module_params').children().last().offset().top - 314;
        //    $('#automation_group_module_params').animate({ scrollTop: '+=' + stop }, 1000);
        //    $('#automation_group_module_params').children().last().find('input[type=text]').first().focus();
        //    $$.LoadModuleParameters();
        //}
    };

    $$.LoadModuleParameters = function () {
        var moduleParams = $('#automation_group_module_params');
        moduleParams.empty();
        var module = $$.CurrentModule;
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
        moduleParams.trigger('create');
        moduleParams.listview().listview('refresh');
        moduleParams.find('input').focus(function () {
            var back = $(this).closest('div').parent().parent().parent();
            if (back.css('background') != '#E6E6FA') {
                $(this).attr('originalbackground', back.css('background'));
                back.css('background', '#E6E6FA');
                setTimeout("$('#automation_group_module_propdelete').removeClass('ui-disabled')", 500);
                //            setTimeout("$('#automation_group_module_propsave').removeClass('ui-disabled')", 500);
                $$.CurrentModuleProperty = back;
            }
        });
        $('#automation_group_module_params input').blur(function () {
            $(this).closest('div').parent().parent().parent().css('background', $(this).attr('originalbackground'));
            setTimeout("$('#automation_group_module_propdelete').addClass('ui-disabled')", 250);
            //        setTimeout("$('#automation_group_module_propsave').addClass('ui-disabled')", 250);
        });
    };

    $$.GetModulesListViewItems = function (groupname) {
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

    $$.AddGroupModule = function (group, domain, address, callback) {
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
                    HG.WebApp.Data.Groups[i].Modules.push({'Address': address, 'Domain': domain});
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

    $$.UpdateWMWatts = function (module, wmwatts) {
        $$.EditModule.WMWatts = wmwatts;
    };

    $$.UpdateModuleType = function (type) {
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
        if ($$.EditModule.DeviceType != type) {
            $$.EditModule.DeviceType = type;
            HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon', '');
            var iconprop = HG.WebApp.Utility.GetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon');
            iconprop.NeedsUpdate = 'true';
            HG.Ui.GetModuleIcon($$.EditModule, function (icon) {
                $('#module_icon').attr('src', icon);
            });
        }
        //
        $$.UpdateFeatures();
    };

    $$.DeleteGroupModule = function (groupname, module) {
        for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            if (HG.WebApp.Data.Groups[i].Name == groupname) {
                HG.WebApp.Data.Groups[i].Modules = $.grep(HG.WebApp.Data.Groups[i].Modules, function (value) {
                    return value.Domain != module.Domain || value.Address != module.Address;
                });
                break;
            }
        }
    };

    $$.DeleteGroup = function (group) {
        $.mobile.loading('show');
        HG.Configure.Groups.DeleteGroup('Control', group, function () {
            $.mobile.loading('hide');
            setTimeout(function () {
                $.mobile.changePage($('#page_configure_groups'), {transition: 'fade', changeHash: true});
            }, 200);
        });
        $('#control_groupslist').empty();
        HG.WebApp.Data._CurrentGroupIndex = 0;
    };

    $$.LoadGroupModules = function () {
        $$.CurrentGroup = $("#configure_groupslist").attr('selected-group-name');
        //
        var groupmodules = HG.Configure.Groups.GetGroupModules($$.CurrentGroup);
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

            if (groupmodules.Modules[m].Domain == $$.SeparatorItemDomain) {
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
        $('#page_configure_groupmodules_list ul').sortable({handle: '.handle', axis: 'y', scrollSpeed: 10});
        $('#page_configure_groupmodules_list ul').on('sortstop', function (event, ui) {
            $$.SortModules();
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
                $$.EditCurrentModule(item);
            });
            item.find('[data-ui-field=btn_parameters]').on('click', function () {
                HG.WebApp.SetCurrentModule(item);
                $('#page_configure_groupmodules_propspopup').popup('open', {
                    transition: 'pop',
                    positionTo: 'window'
                });
            });
            item.find('[data-ui-field=btn_delete]').on('click', function (event) {
                HG.WebApp.SetCurrentModule(item);
                $$.DeleteGroupModule($$.CurrentGroup, $$.CurrentModule);
                HG.WebApp.GroupsList.SaveGroups(null);
            });
        });
        $("#configure_groupslist").listview().listview("refresh");
    };

    HG.WebApp.SetCurrentModule = function (item) {
        var m = item.attr('data-module-index');
        if (m) {
            var groupmodules = HG.Configure.Groups.GetGroupModules($$.CurrentGroup);
            $$.CurrentModule = HG.WebApp.Utility.GetModuleByDomainAddress(groupmodules.Modules[m].Domain, groupmodules.Modules[m].Address);
            if ($$.CurrentModule == null) {
                // module not found, pheraps it was removed
                // so we return the data in the group module reference (address and domain only)
                $$.CurrentModule = groupmodules.Modules[m];
                $$.CurrentModule.DeviceType = '';
            }
        }
    };

    $$.EditCurrentModule = function (item) {

        HG.WebApp.SetCurrentModule(item);
        //$("#configure_groupslist").attr('selected-module-index', item.attr('data-module-index'));
        if ($$.CurrentModule.Domain == $$.SeparatorItemDomain) {
            $$.EditModule.Address = $$.CurrentModule.Address;
            $$.EditModule.Domain = $$.CurrentModule.Domain;
            $('#automation_group_separator_edit').popup('open', {transition: 'pop', positionTo: 'window'});
        } else {
            $$.ModuleEdit(function () {
                // module updated callback
                $('#control_groupslist').empty();
                $$.LoadGroupModules();
            });
        }

    };

    $$.ModuleEdit = function (callback) {

        $$.ModuleUpdatedCallback = callback;
        $$.EditModule.Domain = $$.CurrentModule.Domain;
        $$.EditModule.Address = $$.CurrentModule.Address;
        $$.EditModule.Name = $$.CurrentModule.Name;
        $$.EditModule.Type = $$.CurrentModule.Type;
        $$.EditModule.DeviceType = $$.CurrentModule.DeviceType;
        //
        $$.EditModule.WMWatts = 0;
        if (HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualMeter.Watts") != null) {
            $$.EditModule.WMWatts = HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualMeter.Watts").Value;
        }
        //
        // disable option button if it's a virtual module
        $('#module_options_button').removeClass('ui-disabled');
        if ($$.CurrentModule.Domain != 'HomeAutomation.ZWave') {
            $('#module_options_button').addClass('ui-disabled');
        }
        else if (HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualModule.ParentId") != null) {
            var parentid = HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualModule.ParentId").Value;
            if (parentid != $$.CurrentModule.Address) {
                $('#module_options_button').addClass('ui-disabled');
            }
        }
        //
        $$.UpdateModuleType($$.CurrentModule.DeviceType);
        //
        $('#module_title').html($$.EditModule.Domain.split('.')[1] + ' ' + $$.EditModule.Address + ' - Settings');
        $('#module_name').val($$.EditModule.Name);
        $('#module_type').val($$.EditModule.DeviceType).attr('selected', true).siblings('option').removeAttr('selected');
        $('#module_type').selectmenu('refresh', true);
        //
        $('#configure_module_iconslist').hide();
        HG.Ui.GetModuleIcon($$.CurrentModule, function (icon) {
            $('#module_icon').attr('src', icon);
        });
        $('#module_vmwatts').val($$.EditModule.WMWatts > 0 ? $$.EditModule.WMWatts : '0');
        //
        $$.UpdateFeatures();
        //
        $('#automation_group_module_edit').popup('open', {transition: 'pop', positionTo: 'window'});

    };

    $$.ShowFeatures = function (programid) {
        $('#module_programs_features').empty();
        var desc = HG.WebApp.Locales.GetProgramLocaleString(HG.WebApp.Data.Programs[programid].Address, 'Description', HG.WebApp.Data.Programs[programid].Description);
        $('#module_programs_featuredesc').html(desc);
        var refreshHandler = null;
        for (var p = 0; p < $$.EditModule.Properties.length; p++) {
            var mp = $$.EditModule.Properties[p];
            if (mp.ProgramIndex == programid) {
                mp.Index = p;
                var context = {
                    parent: $('#module_programs_features'),
                    program: HG.WebApp.Data.Programs[programid],
                    module: $$.EditModule,
                    parameter: mp
                };
                var featureField = HG.Ui.GenerateWidget('widgets/' + mp.FieldType, context, function (handler) {
                    handler.onChange = function (val) {
                        $$.FeatureUpdate(handler.context, val);
                    };
                    $('#automation_group_module_edit').popup("reposition", {positionTo: 'window'});
                    if (refreshHandler != null)
                        clearTimeout(refreshHandler);
                    refreshHandler = setTimeout(function () {
                        $('#automation_group_module_edit').popup("reposition", {positionTo: 'window'});
                    }, 300);
                });
            }
        }
    };

    $$.MatchValues = function (valueList, matchValue) {
        var inclusionList = [valueList];
        if (valueList.indexOf(',') > 0)
            inclusionList = valueList.split(',');
        else if (valueList.indexOf('|') > 0)
            inclusionList = valueList.split('|');
        // build exclusion list and remove empty entries
        var exclusionList = [];
        $.each(inclusionList, function (idx, val) {
            if (val.trim().indexOf('!') == 0) {
                inclusionList.splice(idx, 1);
                exclusionList.push(val.trim().substring(1));
            } else if (val.trim() == '') {
                inclusionList.splice(idx, 1);
            }
            return true;
        });
        // check if matching
        var isMatching = (inclusionList.length == 0);
        $.each(inclusionList, function (idx, val) {
            if (val.trim() == matchValue.trim()) {
                isMatching = true;
                return false;
            }
            return true;
        });
        // check if not in exclusion list
        $.each(exclusionList, function (idx, val) {
            if (val.trim() == matchValue.trim()) {
                isMatching = false;
                return false;
            }
            return true;
        });
        return isMatching;
    };

    $$.UpdateFeatures = function () {
        $$.EditModule.Properties = Array(); // used to store "features" values
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
                    var featurematch = $$.MatchValues(features[f].ForDomains.toLowerCase(), $$.EditModule.Domain.toLowerCase());
                    featurematch = featurematch && $$.MatchValues(features[f].ForTypes.toLowerCase(), $$.EditModule.DeviceType.toLowerCase());
                    if (featurematch) {
                        var property = features[f].Property;
                        var prop = HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, property);
                        HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, property, (prop != null ? prop.Value : ""));
                        prop = HG.WebApp.Utility.GetModulePropertyByName($$.EditModule, property);
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
                $$.ShowFeatures(selected);
            }
        }
        $('#automation_group_module_edit').popup("reposition", {positionTo: 'window'});
    };

    $$.FeatureUpdate = function (context, value) {
        var program = context.program;
        var module = context.module;
        var property = context.parameter.Name;
        var mp = HG.WebApp.Utility.GetModulePropertyByName(module, property);
        HG.WebApp.Utility.SetModulePropertyByName(module, property, value);
    };

    $$.ShowModuleOptions = function (domain, address) {
        var module = HG.WebApp.Utility.GetModuleByDomainAddress(domain, address);
        //
        if (module != null) {
            switch (module.Domain) {
                case 'HomeAutomation.ZWave':
                    HG.Ext.ZWave.NodeSetup.Show(module);
                    $.mobile.changePage($('#configurepage_OptionZWave'), {transition: 'fade', changeHash: true});
                    break;
                //case 'HomeAutomation.X10':
                //    $.mobile.changePage($('#configurepage_OptionX10'), { transition: "slide" });
                //    break;
                default:
                    alert('No options page available for this module.');
            }
        }
    };

};