HG.WebApp.GroupsList = HG.WebApp.GroupsList || {};
HG.WebApp.GroupsList.PageId = 'page_configure_groups';

HG.WebApp.GroupsList.InitializePage = function () {
    var page = $('#'+HG.WebApp.GroupsList.PageId);
    page.on('pageinit', function (e) {
        page.find('[id=configure_group_groupadd]').on('popupbeforeposition', function (event) {
            $('#group_new_name').val('');
        });
        page.find('[id=group_new_button]').on('click', function (event) {
            HG.WebApp.GroupsList.GroupsAdd($('#group_new_name').val());
        });
    });
    page.on('pagebeforeshow', function(){
        HG.WebApp.GroupsList.LoadGroups();
    });
};
//
HG.WebApp.GroupsList.LoadGroups = function () {
    $.mobile.loading('show');
    HG.Configure.Groups.List('Control', function () {
        HG.WebApp.GroupsList.GetGroupsListViewItems();
        $.mobile.loading('hide');
    });
};
//
HG.WebApp.GroupsList.GetGroupsListViewItems = function () {
    if ($('#configure_groupslist').hasClass('ui-sortable')) {
        $('#configure_groupslist').sortable('destroy');
        $('#configure_groupslist').off('sortstop');
    }
    $('#configure_groupslist').empty();
    var html = '<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_grouplist') + '</li>';
    for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        // count modules
        var modulescount = 0;
        for (var c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
            var m = HG.WebApp.Data.Groups[i].Modules[c];
            if (HG.WebApp.Utility.GetModuleIndexByDomainAddress(m.Domain, m.Address) != -1) {
                modulescount++;
            }
        }
        html += '<li data-icon="false" data-group-name="' + HG.WebApp.Data.Groups[i].Name + '" data-group-index="' + i + '">';
        html += '<a href="#">' + HG.WebApp.Data.Groups[i].Name + '</a>';
        html += '<span class="ui-li-count">' + (modulescount) + '</span>';
        html += '<div style="position:absolute;right:40px;top:0;height:100%;overflow:hidden"><a class="handle ui-btn ui-icon-fa-sort ui-btn-icon-notext ui-list-btn-option-mini"></a></div>';
        html += '</li>';
    }
    $('#configure_groupslist').append(html);
    $('#configure_groupslist').listview().listview('refresh');
    $('#configure_groupslist').sortable({ handle : '.handle', axis: 'y', scrollSpeed: 10 }).sortable('refresh');
    $('#configure_groupslist').on('sortstop', function (event, ui) {
        HG.WebApp.GroupsList.SortGroups();
    });
    $('#configure_groupslist li').bind('click', function () {
        HG.WebApp.GroupsList.ConfigureGroup($(this).attr('data-group-index'));
    });
};
//
HG.WebApp.GroupsList.ConfigureGroup = function (gidx) {
    $('#configure_groupslist').attr('selected-group-name', HG.WebApp.Data.Groups[gidx].Name);
    $('#configure_groupslist').attr('selected-group-index', gidx);
    $.mobile.changePage($('#page_configure_groupmodules'), { transition: 'fade', changeHash: true });
};
//
HG.WebApp.GroupsList.GetModuleGroup = function (module) {
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
HG.WebApp.GroupsList.GroupsAdd = function (grpname) {

    HG.Configure.Groups.AddGroup('Control', grpname, function () {
        HG.WebApp.GroupsList.LoadGroups();
    });

};
//

HG.WebApp.GroupsList.SortGroups = function () {

    var neworder = '';
    var current = 0;
    $('#configure_groupslist').children('li').each(function () {
        var gidx = $(this).attr('data-group-index');
        if (gidx >= 0) {
            neworder += (gidx + ';')
            $(this).attr('data-module-index', current);
            current++;
        }
    });
    // 
    $('#control_groupslist').empty();
    //
    $.mobile.loading('show');
    HG.Configure.Groups.Sort('Control', neworder, function (res) {
        $.mobile.loading('hide');
        HG.WebApp.GroupsList.LoadGroups();
    });

}

HG.WebApp.GroupsList.SaveGroups = function (callback) {
    $.mobile.loading('show');
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/',
        data: JSON.stringify(HG.WebApp.Data.Groups),
        success: function (response) {
            $('#control_groupslist').empty();
            //
            HG.WebApp.GroupModules.LoadGroupModules();
            //
            $.mobile.loading('hide');
            //
            if (callback != null) callback();
        },
        error: function (a, b, c) {
            $.mobile.loading('hide');
        }
    });
};
