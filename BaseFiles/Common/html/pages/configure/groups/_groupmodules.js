HG.WebApp.GroupModules = HG.WebApp.GroupModules || new function () { var $$ = this;

    $$.CurrentGroup = '(none)';
    $$.CurrentModule = null;
    $$.CurrentModuleProperty = null;
    $$.EditModule = [];
    $$.SeparatorItemDomain = 'HomeGenie.UI.Separator';

    $$.InitializePage = function () {
        var page = $$.getContainer();
        page.on('pageinit', function (e) {
            $$.field('#group_delete_button', true).on('click', function (event) {
                $$.DeleteGroup($$.CurrentGroup);
            });
            $$.field('#btn_configure_group_deletegroup', true).on('click', function (event) {
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_delete');
            });
            $$.field('#btn_configure_group_addmodule', true).on('click', function (event) {
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_modulechoose');
            });
            $$.field('#btn_configure_group_addseparator', true).on('click', function (event) {
                $$.EditModule.Domain = '';
                $$.EditModule.Address = '';
                HG.Ui.SwitchPopup('#listmodules_actionmenu', '#automation_group_separator_edit');
            });
            $$.field('#btn_configure_group_editseparatoradd', true).on('click', function (event) {
                var label = $$.field('#automation_group_separatorlabel', true).val().trim();
                if (label != '') {
                    $$.AddGroupModule($$.CurrentGroup, $$.SeparatorItemDomain, label, function () {
                        var item = $$.field('#page_configure_groupmodules_list', true).find('ul li').last();
                        $$.field('#page_configure_groupmodules_list', true).find('ul').parent().animate({scrollTop: item.position().top}, 'slow');
                    });
                }
            });
            $$.field('#automation_group_separator_edit', true).on('popupbeforeposition', function (event) {
                if ($$.EditModule.Address != '') {
                    $$.field('#automation_group_separatorlabel', true).val($$.EditModule.Address);
                    $$.field('#automation_group_separatorlabel', true).addClass('ui-disabled');
                    $$.field('#btn_configure_group_editseparatoradd', true).hide();
                }
                else {
                    $$.field('#automation_group_separatorlabel', true).val('');
                    $$.field('#automation_group_separatorlabel', true).removeClass('ui-disabled');
                    $$.field('#btn_configure_group_editseparatoradd', true).show();
                }
            });

            $$.field('#automation_group_modulechoose', true).on('popupbeforeposition', function (event) {
                $$.GetModulesListViewItems($$.CurrentGroup);
            });

            $('#automation_group_module_list').on('click', 'li', function () {
                $.mobile.loading('show');
                //
                var domain = $(this).attr('data-context-domain');
                var address = $(this).attr('data-context-address');
                $$.AddGroupModule($$.CurrentGroup, domain, address, function () {
                    var list = $$.field('#page_configure_groupmodules_list', true).find('ul');
                    var item = list.find('li').last();
                    list.parent().animate({ scrollTop: item.position().top });

                    $$.GetModulesListViewItems($$.CurrentGroup);
                    $.mobile.loading('hide');
                });
            });

            $$.GetModulesListViewItems = function (groupname) {
                var groupmodules = HG.Configure.Groups.GetGroupModules(groupname);
                $('#automation_group_module_list').empty();
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

                            if (cursect != module.Domain) {
                                $('#automation_group_module_list').append($('<li/>', { 'data-role': 'list-divider' }).append(module.Domain));
                                cursect = module.Domain;
                            }
                            $('#automation_group_module_list').append($('<li/>', {
                                'data-icon': 'minus',
                                'data-context-domain': module.Domain,
                                'data-context-address': module.Address
                            })
                                .append($('<a/>',
                                    {
                                        'text': module.Address + ' ' + (module.Name != '' ? module.Name : (module.Description != '' ? module.Description : module.DeviceType))
                                    })));
                        }
                    }
                }
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

            $$.field('#automation_group_module_propdelete', true).on('click', function () {
                if ($$.CurrentModuleProperty != null) {
                    $$.ModulePropertyDelete($$.CurrentModuleProperty.find('input[type=text]').first().val());
                    $$.CurrentModuleProperty.remove();
                    $$.CurrentModuleProperty = null;
                }
            });
            var groupName = $$.field('#groupmodules_groupname', true);
            groupName.change(function () {
                HG.Configure.Groups.RenameGroup('Control', $$.CurrentGroup, groupName.val(), function (res) {
                    $$.CurrentGroup = groupName.val();
                    $$.field("#configure_groupslist", true).attr('selected-group-name', groupName.val());
                    $.mobile.loading('show');
                    HG.Configure.Modules.List(function (data) {
                        try {
                            HG.WebApp.Data.Modules = data;
                        } catch (e) {
                        }
                        HG.Automation.Programs.List(function () {
                            HG.WebApp.GroupsList.LoadGroups();
                            $.mobile.loading('hide');
                        });
                    });
                });
            });
            $$.field('#groupmodules_groupwall', true).on('change', function () {
                if ($(this).val() == '')
                    $$.field('#configure_group_wallpaper', true).css('background-image', '');
                else
                    $$.field('#configure_group_wallpaper', true).css('background-image', 'url(images/wallpapers/' + $(this).val() + ')');
                HG.Configure.Groups.WallpaperSet($$.CurrentGroup, $(this).val(), function () {
                });
            });
            $$.field('#groupmodules_group_deletebtn', true).on('click', function () {
                $.mobile.loading('show');
                HG.Configure.Groups.WallpaperDelete($$.field('#groupmodules_groupwall', true).val(), function () {
                    var group = HG.Configure.Groups.GetGroupByName($$.CurrentGroup);
                    group.Wallpaper = '';
                    $$.RefreshWallpaper();
                    $.mobile.loading('hide');
                });
            });
            $$.field('#groupmodules_group_uploadbtn', true).on('click', function () {
                $$.field('#groupmodules_group_uploadfile', true).click();
            });
            $$.wallpaper = $$.field('#configure_group_wallpaper', true);
        });
        page.on('pagebeforeshow', function () {
            $$.field('#page_configure_groupmodules_list', true).find('ul').parent().animate({scrollTop: 0});
            $$.LoadGroupModules();
            $.mobile.loading('show');
            $$.RefreshWallpaper();
        });
        $$.field('#groupmodules_group_uploadfile', true).fileupload({
            url: '/api/HomeAutomation.HomeGenie/Config/Groups.WallpaperAdd',
            replaceFileInput: false,
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
                $$.field('#configure_group_wallpaper', true).css('background-image', 'url(images/wallpapers/' + wp + ')');
                HG.Configure.Groups.WallpaperSet($$.CurrentGroup, wp, function () {
                    var group = HG.Configure.Groups.GetGroupByName($$.CurrentGroup);
                    group.Wallpaper = wp;
                    if ($.mobile.activePage.attr('id') == 'page_control')
                        $$.field('div[data-ui-field="wallpaper"]', true).css('background-image', 'url(images/wallpapers/' + wp + ')');
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
        $$.field('#groupmodules_groupwall', true).find('option:gt(0)').remove();
        HG.Configure.Groups.WallpaperList(function (list) {
            $.each(list, function (k, v) {
                var selected = (group.Wallpaper == v ? ' selected' : '');
                $$.field('#groupmodules_groupwall', true).append('<option value="' + v + '"' + selected + '>' + v + '</option>');
            });
            $$.field('#groupmodules_groupwall', true).selectmenu('refresh');
            $.mobile.loading('hide');
        });
    };

    $$.SetModuleIcon = function (img) {
        var icon = $(img).attr('src');
        $$.field('#module_icon', true).attr('src', icon);
        HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon', icon);
        var iconprop = HG.WebApp.Utility.GetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon');
        iconprop.NeedsUpdate = 'true';
        $$.field('#configure_module_iconslist', true).hide(200);
    };

    $$.ModuleIconsToggle = function () {

        if ($$.EditModule.DeviceType.toLowerCase() != 'program') {
            if ($$.field('#configure_module_iconslist', true).css('display') != 'none') {
                $$.field('#configure_module_iconslist', true).hide(200);
            }
            else {
                $$.field('#configure_module_iconslist', true).show(200);
            }
        }

    };

    $$.SortModules = function () {

        var neworder = '';
        $$.field('#page_configure_groupmodules_list', true).find('ul').children('li').each(function () {
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
        var moduleParams = $$.field('#automation_group_module_params', true);
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
                $$.field('#automation_group_module_params', true).append(item);
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
        $$.field('#automation_group_module_params input', true).blur(function () {
            $(this).closest('div').parent().parent().parent().css('background', $(this).attr('originalbackground'));
            setTimeout("$('#automation_group_module_propdelete').addClass('ui-disabled')", 250);
            //        setTimeout("$('#automation_group_module_propsave').addClass('ui-disabled')", 250);
        });
    };

    $$.UpdateWMWatts = function (module, wmwatts) {
        $$.EditModule.WMWatts = wmwatts;
    };

    $$.UpdateModuleType = function (type) {
        var mtype = type.toLowerCase();
        //
        $$.field('#module_options_1', true).css('display', '');
        $$.field('#module_options_2', true).css('display', '');
        $$.field('#module_options_3', true).css('display', '');
        $$.field('#module_update_button', true).removeClass('ui-disabled');
        //
        if (mtype == 'light' || mtype == 'dimmer' || mtype == 'switch') {
            $$.field('#module_vmwatts', true).val('0');
            $$.field('#module_vmwatts_label', true).removeClass('ui-disabled');
            $$.field('#module_vmwatts', true).removeClass('ui-disabled');
        }
        else if (mtype == 'program') {
            $$.field('#module_options_1', true).css('display', 'none');
            $$.field('#module_options_2', true).css('display', 'none');
            $$.field('#module_options_3', true).css('display', 'none');
            $$.field('#module_update_button', true).addClass('ui-disabled');
        }
        else if (mtype && mtype != undefined && mtype != 'generic' && mtype != '') {
            $$.field('#module_vmwatts', true).val('');
            $$.field('#module_vmwatts_label', true).addClass('ui-disabled');
            $$.field('#module_vmwatts', true).addClass('ui-disabled');
        }
        //
        if ($$.EditModule.DeviceType != type) {
            $$.EditModule.DeviceType = type;
            HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon', '');
            var iconprop = HG.WebApp.Utility.GetModulePropertyByName($$.EditModule, 'Widget.DisplayIcon');
            iconprop.NeedsUpdate = 'true';
            HG.Ui.GetModuleIcon($$.EditModule, function (icon) {
                $$.field('#module_icon', true).attr('src', icon);
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
                $.mobile.changePage($$.field('#page_configure_groups', true), {transition: 'fade', changeHash: true});
            }, 200);
        });
        $$.field('#control_groupslist', true).empty();
        HG.WebApp.Data._CurrentGroupIndex = 0;
    };

    $$.LoadGroupModules = function () {
        $$.CurrentGroup = $$.field("#configure_groupslist", true).attr('selected-group-name');
        //
        var groupmodules = HG.Configure.Groups.GetGroupModules($$.CurrentGroup);
        //
        $$.field('#groupmodules_groupname', true).val(groupmodules.Name);
        //
        if ($$.field('#page_configure_groupmodules_list', true).find('ul').hasClass('ui-sortable')) {
            $$.field('#page_configure_groupmodules_list', true).find('ul').sortable('destroy');
            $$.field('#page_configure_groupmodules_list', true).find('ul').off('sortstop');
        }
        $$.field('#page_configure_groupmodules_list', true).find('ul').empty();
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
        $$.field('#page_configure_groupmodules_list', true).find('ul').append(html);
        $$.field('#page_configure_groupmodules_list', true).find('ul').listview().listview('refresh');
        $$.field('#page_configure_groupmodules_list', true).find('ul').sortable({handle: '.handle', axis: 'y', scrollSpeed: 10});
        $$.field('#page_configure_groupmodules_list', true).find('ul').on('sortstop', function (event, ui) {
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
        $$.field("#page_configure_groupmodules_list", true).find("ul li").each(function (index) {
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
        $$.field("#configure_groupslist", true).listview().listview("refresh");
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
            $$.field('#automation_group_separator_edit', true).popup('open', {transition: 'pop', positionTo: 'window'});
        } else {
            $$.ModuleEdit(function () {
                // module updated callback
                $$.field('#control_groupslist', true).empty();
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
        $$.field('#module_options_button', true).removeClass('ui-disabled');
        if ($$.CurrentModule.Domain != 'HomeAutomation.ZWave') {
            $$.field('#module_options_button', true).addClass('ui-disabled');
        }
        else if (HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualModule.ParentId") != null) {
            var parentid = HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, "VirtualModule.ParentId").Value;
            if (parentid != $$.CurrentModule.Address) {
                $$.field('#module_options_button', true).addClass('ui-disabled');
            }
        }
        //
        $$.UpdateModuleType($$.CurrentModule.DeviceType);
        //
        $$.field('#module_title', true).html($$.EditModule.Domain.split('.')[1] + ' ' + $$.EditModule.Address + ' - Settings');
        $$.field('#module_name', true).val($$.EditModule.Name);
        $$.field('#module_type', true).val($$.EditModule.DeviceType).attr('selected', true).siblings('option').removeAttr('selected');
        $$.field('#module_type', true).selectmenu('refresh', true);
        //
        $$.field('#configure_module_iconslist', true).hide();
        HG.Ui.GetModuleIcon($$.CurrentModule, function (icon) {
            $$.field('#module_icon', true).attr('src', icon);
        });
        $$.field('#module_vmwatts', true).val($$.EditModule.WMWatts > 0 ? $$.EditModule.WMWatts : '0');
        //
        $$.UpdateFeatures();
        //
        $$.field('#automation_group_module_edit', true).popup('open', {transition: 'pop', positionTo: 'window'});

    };

    $$.ShowFeatures = function (programid) {
        $$.field('#module_programs_features', true).empty();
        var desc = HG.WebApp.Locales.GetProgramLocaleString(HG.WebApp.Data.Programs[programid].Address, 'Description', HG.WebApp.Data.Programs[programid].Description);
        $$.field('#module_programs_featuredesc', true).html(desc);
        var refreshHandler = null;
        for (var p = 0; p < $$.EditModule.Properties.length; p++) {
            var mp = $$.EditModule.Properties[p];
            if (mp.ProgramIndex == programid) {
                mp.Index = p;
                var context = {
                    parent: $$.field('#module_programs_features', true),
                    program: HG.WebApp.Data.Programs[programid],
                    module: $$.EditModule,
                    parameter: mp
                };
                var featureField = HG.Ui.GenerateWidget('widgets/' + mp.FieldType, context, function (handler) {
                    handler.onChange = function (val) {
                        $$.FeatureUpdate(handler.context, val);
                    };
                    $$.field('#automation_group_module_edit', true).popup("reposition", {positionTo: 'window'});
                    if (refreshHandler != null)
                        clearTimeout(refreshHandler);
                    refreshHandler = setTimeout(function () {
                        $$.field('#automation_group_module_edit', true).popup("reposition", {positionTo: 'window'});
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
        $$.field('#module_options_features', true).hide();
        $$.field('#module_programs_featureset', true).empty();
        $$.field('#module_programs_features', true).empty();
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
                        // <prop>.Data is a special field to store extra data related to <prop> (eg. input form state)
                        var propData = HG.WebApp.Utility.GetModulePropertyByName($$.CurrentModule, property+'.Data');
                        if (propData != null)
                            HG.WebApp.Utility.SetModulePropertyByName($$.EditModule, propData.Name, propData.Value);
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
            $$.field('#module_programs_featureset', true).append(featureset);
            $$.field('#module_programs_featureset', true).selectmenu('refresh', true);
            $$.field('#module_options_features', true).show();
            //
            if (selected != -1) {
                $$.ShowFeatures(selected);
            }
        }
        $$.field('#automation_group_module_edit', true).popup("reposition", {positionTo: 'window'});
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
                    $.mobile.changePage($$.field('#configurepage_OptionZWave', true), {transition: 'fade', changeHash: true});
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
HG.Ui.CreatePage(HG.WebApp.GroupModules, 'page_configure_groupmodules');