HG.WebApp.WidgetsList = HG.WebApp.WidgetsList || {};
HG.WebApp.WidgetsList._CurrentWidget = '';
HG.WebApp.WidgetsList.PageId = '#page_configure_widgetsgroups';

HG.WebApp.WidgetsList.InitializePage = function () {
    var page = $(HG.WebApp.WidgetsList.PageId);
    page.on('pageinit', function (e) {
    });
    page.on('pagebeforeshow', function (e) {
//        $('[data-role=popup]').on('popupbeforeposition', function (event) {
//            if (this.id == 'automationgroup_add') {
//                $('#automationgroup_new_name').val('');
//            }
//        });
        //	
//        $('#automationgroup_new_button').bind('click', function (event) {
//            HG.WebApp.AutomationGroupsList.GroupsAdd($('#automationgroup_new_name').val());
//        });
        //	
        $.mobile.loading('show');
        HG.Configure.Widgets.List(function(items) {
            HG.WebApp.WidgetsList.RefreshList(items);
            $.mobile.loading('hide');
        });
    });
};

HG.WebApp.WidgetsList.RefreshList = function(items) {
    var page = $(HG.WebApp.WidgetsList.PageId);
    var listMenu = page.find('[data-ui-field=group-list]');
    listMenu.empty();
    listMenu.append('<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_widgetlist', 'Widget List') + '</li>');
    for (var i = 0; i < items.length; i++) {
        listMenu.append('<li data-item-name="' + items[i] + '" data-item-index="' + i + '"><a href="#page_widgeteditor_editwidget" data-transition="slide"><img src="test" width="36" height="36" style="margin-left:7px;margin-top:2px" />' + items[i] + '</a></li>');
    }
    listMenu.listview();
    listMenu.listview('refresh');
    for (var i = 0; i < items.length; i++) {
        HG.WebApp.WidgetsList.GetWidgetIcon(items[i], i, function(icon, id) {
            listMenu.find('li a img').eq(id).attr('src', icon);
        });
    }
    listMenu.find('li').on('click', function () {
        HG.WebApp.WidgetsList._CurrentWidget = $(this).attr('data-item-name');
    });
};

HG.WebApp.WidgetsList.GetWidgetIcon = function (widget, elid, callback) {
    HG.WebApp.Control.GetWidget(widget, function (widgetobject) {
        if (widgetobject != null)
        {
            icon = widgetobject.Instance.IconImage;
            if (callback != null) callback(icon, elid);
        }
    });
}

HG.WebApp.WidgetsList.GroupsAdd = function (grpname) {
    HG.Configure.Groups.AddGroup('Automation', grpname, function () {
        HG.WebApp.AutomationGroupsList.LoadGroups();
    });
};

