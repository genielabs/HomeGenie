HG.WebApp.GroupsList = HG.WebApp.GroupsList || {};

HG.WebApp.GroupsList.InitializePage = function () {
    $('#page_configure_groups').on('pageinit', function (e) {
        $('[data-role=popup]').on('popupbeforeposition', function (event) {
            if (this.id == 'automation_group_add') {
                $('#group_new_name').val('');
            }
        });
        //	
        $('#group_new_button').bind('click', function (event) {
            HG.WebApp.GroupsList.GroupsAdd($('#group_new_name').val());
        });
        //	
        $.mobile.showPageLoadingMsg();
        HG.Configure.Groups.List('Control', function () {
            HG.WebApp.GroupsList.GetGroupsListViewItems();
            $.mobile.hidePageLoadingMsg();
        });
        //
        HG.WebApp.GroupsList.LoadGroups();
        //
        $("#configure_groupslist").sortable();
        $("#configure_groupslist").disableSelection();
        //<!-- Refresh list to the end of sort for having a correct display -->
        $("#configure_groupslist").bind("sortstop", function (event, ui) {
            HG.WebApp.GroupsList.SortGroups();
        });

    });
};
//
HG.WebApp.GroupsList.LoadGroups = function()
{
    $.mobile.showPageLoadingMsg();
    HG.Configure.Groups.List('Control', function () {
        HG.WebApp.GroupsList.GetGroupsListViewItems();
        $.mobile.hidePageLoadingMsg();
    });
};
//
HG.WebApp.GroupsList.GetGroupsListViewItems = function() {
    $('#configure_groupslist').empty();
    $('#configure_groupslist').append('<li data-theme="a" data-icon="false" data-role="list-divider">Groups List</li>');
    //
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        // count modules
        var modulescount = 0;
        for (var c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
            var m = HG.WebApp.Data.Groups[i].Modules[c];
            if (HG.WebApp.Utility.GetModuleIndexByDomainAddress(m.Domain, m.Address) != -1)
            {
                modulescount++;
            }
        }
        $('#configure_groupslist').append('<li data-theme="' + uitheme + '" data-group-name="' + HG.WebApp.Data.Groups[i].Name + '" data-group-index="' + i + '"><a href="#page_configure_groupmodules" data-transition="slide">' + HG.WebApp.Data.Groups[i].Name + '</a><span class="ui-li-count">' + (modulescount) + '</span></li>');
    }
    $('#configure_groupslist').listview();
    $('#configure_groupslist').listview('refresh');
    //
    $("#configure_groupslist li").bind("click", function () {
        var group = $(this).text();
        $("#configure_groupslist").attr('selected-group-name', $(this).attr('data-group-name'));
        $("#configure_groupslist").attr('selected-group-index', $(this).attr('data-group-index'));
    });
    $("#configure_groupslist").listview("refresh");
};
//
HG.WebApp.GroupsList.GetModuleGroup = function(module) {
    var group = null;
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        for (var c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
            if (module.Domain == HG.WebApp.Data.Groups[i].Modules[c].Domain && module.Address == HG.WebApp.Data.Groups[i].Modules[c].Address) {
                group = HG.WebApp.Data.Groups[i];
                break;
            }
        }
        if (group != null) break;
    }
    return group;
};	
//
HG.WebApp.GroupsList.GroupsAdd = function(grpname) {

    HG.Configure.Groups.AddGroup('Control', grpname, function () { 
        HG.WebApp.GroupsList.LoadGroups(); 
    });

};
//

HG.WebApp.GroupsList.SortGroups = function () {

    var neworder = '';
    $('#configure_groupslist').children('li').each(function () {
        var gidx = $(this).attr('data-group-index');
        if (gidx >= 0) {
            neworder += (gidx + ';')
        }
    });
    // 
    $('#control_groupslist').empty();
    //
    HG.Configure.Groups.Sort('Control', neworder, function (res) {
        HG.WebApp.GroupsList.LoadGroups();
    });

}

HG.WebApp.GroupsList.SaveGroups = function(callback) {
    $.mobile.showPageLoadingMsg();
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/',
        //                        dataType: 'json',
        data: JSON.stringify(HG.WebApp.Data.Groups),
        success: function (response) {
            $('#control_groupslist').empty();
            //
            HG.WebApp.GroupModules.LoadGroupModules();
            //
            $.mobile.hidePageLoadingMsg();
            //
            if (callback != null) callback();
        },
        error: function (a, b, c) {
            $.mobile.hidePageLoadingMsg();
        }
    });
};
//
HG.WebApp.GroupsList.SaveModules = function() {
    $.mobile.showPageLoadingMsg();
    var modules = null;
    try
    {
        modules = JSON.stringify(HG.WebApp.Data.Modules);
    } 
    catch (e)
    {
        alert('Error saving modules!\nPlease retry.');
        $.mobile.showPageLoadingMsg();
    }
    if (modules != null)
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Save/Control/',
        //                        dataType: 'json',
        data: modules,
        success: function (response) {
            HG.WebApp.GroupsList.SaveGroups(null);                        
        },
        error: function (a, b, c) {
            $.mobile.hidePageLoadingMsg();
        }
    });
};
