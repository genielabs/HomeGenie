HG.WebApp.AutomationGroupsList = HG.WebApp.AutomationGroupsList || {};
HG.WebApp.AutomationGroupsList.PageId = 'page_configure_automationgroups';
HG.WebApp.AutomationGroupsList._CurrentGroup = '';

HG.WebApp.AutomationGroupsList.InitializePage = function () {
    var page = $('#'+HG.WebApp.AutomationGroupsList.PageId);
    var widgetEditorButton = page.find('[data-ui-field=widgeteditor-btn]');
    page.on('pageinit', function (e) {
        page.find('[id=automationgroup_add]').on('popupbeforeposition', function (event) {
            page.find('[id=automationgroup_new_name]').val('');
        });
        page.find('[id=automationgroup_new_button]').bind('click', function (event) {
            HG.WebApp.AutomationGroupsList.GroupsAdd($('#automationgroup_new_name').val());
        });
        widgetEditorButton.bind('click', function (event) {
            $.mobile.pageContainer.pagecontainer('change', '#'+HG.WebApp.WidgetsList.PageId, { transition: "slide" });
        });
        $.mobile.loading('show');
        HG.Configure.Groups.List('Automation', function () {
            HG.WebApp.AutomationGroupsList.GetGroupsListViewItems();
            $.mobile.loading('hide');
        });
    });
    page.on('pagebeforeshow', function (e) {
        HG.Automation.Programs.List(function () {
            HG.WebApp.AutomationGroupsList.LoadGroups();
        });
    });
};
//
HG.WebApp.AutomationGroupsList.LoadGroups = function () {
    $.mobile.loading('show');
    HG.Configure.Groups.List('Automation', function () {
        HG.WebApp.AutomationGroupsList.GetGroupsListViewItems();
        $.mobile.loading('hide');
    });
};
//
HG.WebApp.AutomationGroupsList.GetGroupsListViewItems = function () {
    if ($('#configure_automationgroupslist').hasClass('ui-sortable')) {
        $('#configure_automationgroupslist').sortable('destroy');
        $('#configure_automationgroupslist').off('sortstop');
    }
    $('#configure_automationgroupslist').empty();
    //
    var ifaceZwave = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.ZWave');
    var ifaceInsteon = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.Insteon');
    var ifaceX10 = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.X10');
    var ifaceW800rf = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.W800RF');
    //
    var i = 0;
    var hiddenGroup = ' style="display:none"';
    var html = '<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_grouplist') + '</li>';
    for (; i < HG.WebApp.Data.AutomationGroups.length; i++) {
        var groupName = HG.WebApp.Data.AutomationGroups[i].Name;
        var itemVisibility = '';
        // hide non valid entries for the running configuration
        if ((groupName == 'Raspberry Pi' || groupName == 'CubieTruck') && HOST_SYSTEM.substring(0, 3) == 'Win') itemVisibility = hiddenGroup;
        else if (groupName == 'X10' && ifaceX10 == null && ifaceInsteon == null && ifaceW800rf == null) itemVisibility = hiddenGroup;
        else if (groupName == 'Z-Wave' && ifaceZwave == null) itemVisibility = hiddenGroup;
        // count modules
        var modulescount = 0;
        for (p = 0; p < HG.WebApp.Data.Programs.length; p++) {
            if (HG.WebApp.Data.Programs[p].Group == HG.WebApp.Data.AutomationGroups[i].Name) {
                modulescount++;
            }
        }
        html += '<li' + itemVisibility + ' data-icon="false" data-group-name="' + groupName + '" data-group-index="' + i + '">';
        html += '<a href="#page_automation_programs">' + groupName + '</a>';
        html += '<span class="ui-li-count">' + (modulescount) + '</span>';
        html += '<div style="position:absolute;right:40px;top:0;height:100%;overflow:hidden"><a class="handle ui-btn ui-icon-fa-sort ui-btn-icon-notext ui-list-btn-option-mini"></a></div>';
        html += '</li>';
    }
    // programs with no group are shown in "Ungrouped" special group
    modulescount = 0;
    for (p = 0; p < HG.WebApp.Data.Programs.length; p++) {
        if (!HG.WebApp.Data.Programs[p].Group || HG.WebApp.Data.Programs[p].Group == '' || HG.WebApp.Data.Programs[p].Group == 'undefined') {
            modulescount++;
        }
    }
    if (modulescount > 0) {
        html += '<li data-icon="false" data-group-name=""><a href="#page_automation_programs">Ungrouped</a><span class="ui-li-count">' + (modulescount) + '</span></li>';
    }
    //
    $('#configure_automationgroupslist').append(html);
    $('#configure_automationgroupslist').listview().listview('refresh');
    $('#configure_automationgroupslist').sortable({ handle : '.handle', axis: 'y', scrollSpeed: 10 }).sortable('refresh');
    $('#configure_automationgroupslist').on('sortstop', function (event, ui) {
        HG.WebApp.AutomationGroupsList.SortGroups();
    });
    $('#configure_automationgroupslist li').on("click", function () {
        HG.WebApp.AutomationGroupsList._CurrentGroup = $(this).attr('data-group-name');
        $('#configure_automationgroupslist').attr('selected-group-name', $(this).attr('data-group-name'));
        $('#configure_automationgroupslist').attr('selected-group-index', $(this).attr('data-group-index'));
    });
};
//
HG.WebApp.AutomationGroupsList.GetGroupModules = function (groupname) {
    var groupmodules = { 'Index': 0, 'Name': groupname, 'Modules': Array() };
    //
    for (var i = 0; i < HG.WebApp.Data.AutomationGroups.length; i++) {
        if (HG.WebApp.Data.AutomationGroups[i].Name == groupname) {
            groupmodules.Index = i;
            for (var c = 0; c < HG.WebApp.Data.AutomationGroups[i].Modules.length; c++) {
                var found = false;
                /*if (HG.WebApp.Data.AutomationGroups[i].Modules[c].Domain == 'HomeAutomation.HomeGenie.Automation')
                {
                for (var m = 0; m < HG.WebApp.Data.Programs.length; m++) {
                if (HG.WebApp.Data.Programs[m].Address == HG.WebApp.Data.AutomationGroups[i].Modules[c].Address) {
                groupmodules.Modules.push(HG.WebApp.Data.Programs[m]);
                found = true;
                break;
                }
                }
                }
                else*/
                {
                    for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                        if (HG.WebApp.Data.Modules[m].Domain == HG.WebApp.Data.AutomationGroups[i].Modules[c].Domain && HG.WebApp.Data.Modules[m].Address == HG.WebApp.Data.AutomationGroups[i].Modules[c].Address) {
                            groupmodules.Modules.push(HG.WebApp.Data.Modules[m]);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) {
                    // orphan module/program, it is not present in the modules list nor programs one
                    groupmodules.Modules.push(HG.WebApp.Data.AutomationGroups[i].Modules[c]);
                }
            }
            break;
        }
    }
    //
    return groupmodules;
};
//
HG.WebApp.AutomationGroupsList.GetModuleGroup = function (module) {
    var group = null;
    for (var i = 0; i < HG.WebApp.Data.AutomationGroups.length; i++) {
        for (var c = 0; c < HG.WebApp.Data.AutomationGroups[i].Modules.length; c++) {
            if (module.Domain == HG.WebApp.Data.AutomationGroups[i].Modules[c].Domain && module.Address == HG.WebApp.Data.AutomationGroups[i].Modules[c].Address) {
                group = HG.WebApp.Data.AutomationGroups[i];
                break;
            }
        }
        if (group != null) break;
    }
    return group;
};
//
HG.WebApp.AutomationGroupsList.GroupsAdd = function (grpname) {

    HG.Configure.Groups.AddGroup('Automation', grpname, function () {
        HG.WebApp.AutomationGroupsList.LoadGroups();
    });

};
//
HG.WebApp.AutomationGroupsList.SortGroups = function () {

    var neworder = '';

    $('#configure_automationgroupslist').children('li').each(function () {
        var gidx = $(this).attr('data-group-index');
        if (gidx >= 0) {
            neworder += (gidx + ';')
        }
    });

    HG.Configure.Groups.Sort('Automation', neworder, function (res) {
        HG.WebApp.AutomationGroupsList.LoadGroups();
    });

}
//
HG.WebApp.AutomationGroupsList.SaveGroups = function () {
    $.mobile.loading('show');
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/Automation/',
        data: JSON.stringify(HG.WebApp.Data.AutomationGroups),
        success: function (response) {
            $('#control_automationgroupslist').empty();
            //
            // TODO: reload group programs (?)
            ///HG.WebApp.GroupModules.LoadGroupModules();
            //
            $.mobile.loading('hide');
        },
        error: function (a, b, c) {
            $.mobile.loading('hide');
        }
    });
};