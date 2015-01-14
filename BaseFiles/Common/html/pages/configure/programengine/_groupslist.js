HG.WebApp.AutomationGroupsList = HG.WebApp.AutomationGroupsList || {};
HG.WebApp.AutomationGroupsList._CurrentGroup = '';

HG.WebApp.AutomationGroupsList.InitializePage = function () {
    $('#page_configure_automationgroups').on('pageinit', function (e) {
        $('[data-role=popup]').on('popupbeforeposition', function (event) {
            if (this.id == 'automationgroup_add') {
                $('#automationgroup_new_name').val('');
            }
        });
        //	
        $('#automationgroup_new_button').bind('click', function (event) {
            HG.WebApp.AutomationGroupsList.GroupsAdd($('#automationgroup_new_name').val());
        });
        //	
        $.mobile.loading('show');
        HG.Configure.Groups.List('Automation', function () {
            HG.WebApp.AutomationGroupsList.GetGroupsListViewItems();
            $.mobile.loading('hide');
        });
        //
        HG.WebApp.AutomationGroupsList.LoadGroups();
        //
        $("#configure_automationgroupslist").sortable();
        $("#configure_automationgroupslist").disableSelection();
        //<!-- Refresh list to the end of sort for having a correct display -->
        $("#configure_automationgroupslist").bind("sortstop", function (event, ui) {
            HG.WebApp.AutomationGroupsList.SortGroups();
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
    $('#configure_automationgroupslist').empty();
    $('#configure_automationgroupslist').append('<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_grouplist') + '</li>');
    //
    var ifaceZwave = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.ZWave');
    var ifaceInsteon = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.Insteon');
    var ifaceX10 = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.X10');
    var ifaceW800rf = HG.WebApp.SystemSettings.GetInterface('HomeAutomation.W800RF');
    //
    var i = 0;
    for (; i < HG.WebApp.Data.AutomationGroups.length; i++) {
        var groupName = HG.WebApp.Data.AutomationGroups[i].Name;
        //
        // filter non valid entries for the running configuration
        if (groupName == 'Raspberry Pi' && HOST_SYSTEM.substring(0, 3) == 'Win') continue;
        else if (groupName == 'X10' && ifaceX10 == null && ifaceInsteon == null && ifaceW800rf == null) continue;
        else if (groupName == 'Z-Wave' && ifaceZwave == null) continue;
        //
        // count modules
        var modulescount = 0;
        for (p = 0; p < HG.WebApp.Data.Programs.length; p++) {
            if (HG.WebApp.Data.Programs[p].Group == HG.WebApp.Data.AutomationGroups[i].Name) {
                modulescount++;
            }
        }
        $('#configure_automationgroupslist').append('<li data-group-name="' + groupName + '" data-group-index="' + i + '"><a href="#page_automation_programs" data-transition="slide">' + groupName + '</a><span class="ui-li-count">' + (modulescount) + '</span></li>');
    }
    //
    // programs with no group are shown in "Ungrouped" special group
    modulescount = 0;
    for (p = 0; p < HG.WebApp.Data.Programs.length; p++) {
        if (!HG.WebApp.Data.Programs[p].Group || HG.WebApp.Data.Programs[p].Group == '' || HG.WebApp.Data.Programs[p].Group == 'undefined') {
            modulescount++;
        }
    }
    if (modulescount > 0) {
        $('#configure_automationgroupslist').append('<li data-group-name=""><a href="#page_automation_programs" data-transition="slide">Ungrouped</a><span class="ui-li-count">' + (modulescount) + '</span></li>');
    }
    //
    $('#configure_automationgroupslist').listview();
    $('#configure_automationgroupslist').listview('refresh');
    //
    $("#configure_automationgroupslist li").on('click', function () {
        HG.WebApp.AutomationGroupsList._CurrentGroup = $(this).attr('data-group-name');
    });
    //
    $("#configure_automationgroupslist li").bind("click", function () {
        var group = $(this).text();
        $("#configure_automationgroupslist").attr('selected-group-name', $(this).attr('data-group-name'));
        $("#configure_automationgroupslist").attr('selected-group-index', $(this).attr('data-group-index'));
    });
    $("#configure_automationgroupslist").listview("refresh");
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
                /*					if (HG.WebApp.Data.AutomationGroups[i].Modules[c].Domain == 'HomeAutomation.HomeGenie.Automation')
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

HG.WebApp.AutomationGroupsList.SaveGroups = function () {
    $.mobile.loading('show');
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/Automation/',
        data: JSON.stringify(HG.WebApp.Data.AutomationGroups),
        success: function (response) {
            $('#control_automationgroupslist').empty();
            //
            // TODO: reload group programs
            ///				HG.WebApp.GroupModules.LoadGroupModules();
            //
            $.mobile.loading('hide');
        },
        error: function (a, b, c) {
            $.mobile.loading('hide');
        }
    });
};
//
