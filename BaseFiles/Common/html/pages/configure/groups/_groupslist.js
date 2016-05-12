HG.WebApp.GroupsList = HG.WebApp.GroupsList || new function () { var $$ = this;

    $$.PageId = 'page_configure_groups';

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        page.on('pageinit', function (e) {
            page.find('[id=configure_group_groupadd]').on('popupbeforeposition', function (event) {
                $('#group_new_name').val('');
            });
            page.find('[id=group_new_button]').on('click', function (event) {
                $$.GroupsAdd($('#group_new_name').val());
            });
        });
        page.on('pagebeforeshow', function () {
            $$.LoadGroups();
        });
        $$.groupList = $('#configure_groupslist');
    };

    $$.LoadGroups = function () {
        $.mobile.loading('show');
        HG.Configure.Groups.List('Control', function () {
            $$.GetGroupsListViewItems();
            $.mobile.loading('hide');
        });
    };

    $$.GetGroupsListViewItems = function () {
        if ($$.groupList.hasClass('ui-sortable')) {
            $$.groupList.sortable('destroy');
            $$.groupList.off('sortstop');
        }
        $$.groupList.empty();
        var html = '<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_grouplist') + '</li>';
        for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var group = HG.WebApp.Data.Groups[i];
            // count modules
            var modulescount = 0;
            for (var c = 0; c < group.Modules.length; c++) {
                var m = group.Modules[c];
                if (HG.WebApp.Utility.GetModuleIndexByDomainAddress(m.Domain, m.Address) != -1) {
                    modulescount++;
                }
            }
            html += '<li data-icon="false" data-group-name="' + group.Name + '" data-group-index="' + i + '">';
            html += '<a href="#">' + group.Name + '</a>';
            html += '<span class="ui-li-count">' + (modulescount) + '</span>';
            html += '<div style="position:absolute;right:40px;top:0;height:100%;overflow:hidden"><a class="handle ui-btn ui-icon-fa-sort ui-btn-icon-notext ui-list-btn-option-mini"></a></div>';
            html += '</li>';
        }
        $$.groupList.append(html);
        $$.groupList.listview().listview('refresh');
        $$.groupList.sortable({handle: '.handle', axis: 'y', scrollSpeed: 10}).sortable('refresh');
        $$.groupList.on('sortstop', function (event, ui) {
            $$.SortGroups();
        });
        $$.groupList.find('li').bind('click', function () {
            $$.ConfigureGroup($(this).attr('data-group-index'));
        });
    };

    $$.ConfigureGroup = function (gidx) {
        $$.groupList.attr('selected-group-name', HG.WebApp.Data.Groups[gidx].Name);
        $$.groupList.attr('selected-group-index', gidx);
        $.mobile.changePage($('#page_configure_groupmodules'), {transition: 'fade', changeHash: true});
    };

    $$.GetModuleGroup = function (module) {
        var group = null;
        for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var cg = HG.WebApp.Data.Groups[i];
            for (var c = 0; c < cg.Modules.length; c++) {
                if (module.Domain == cg.Modules[c].Domain && module.Address == cg.Modules[c].Address) {
                    group = cg;
                    break;
                }
            }
            if (group != null) break;
        }
        return group;
    };

    $$.GroupsAdd = function (grpname) {
        HG.Configure.Groups.AddGroup('Control', grpname, function () {
            $$.LoadGroups();
        });
    };

    $$.SortGroups = function () {

        var neworder = '';
        var current = 0;
        $$.groupList.children('li').each(function () {
            var gidx = $(this).attr('data-group-index');
            if (gidx >= 0) {
                neworder += (gidx + ';')
                $(this).attr('data-module-index', current);
                current++;
            }
        });
        $$.groupList.empty();
        $.mobile.loading('show');
        HG.Configure.Groups.Sort('Control', neworder, function (res) {
            $.mobile.loading('hide');
            $$.LoadGroups();
        });

    };

    $$.SaveGroups = function (callback) {
        $.mobile.loading('show');
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/',
            data: JSON.stringify(HG.WebApp.Data.Groups),
            success: function (response) {
                $$.groupList.empty();
                HG.WebApp.GroupModules.LoadGroupModules();
                $.mobile.loading('hide');
                if (callback != null) callback();
            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
            }
        });
    };

};