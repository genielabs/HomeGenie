HG.WebApp.WidgetsList = HG.WebApp.WidgetsList || {};
HG.WebApp.WidgetsList.PageId = 'page_configure_widgetlist';
HG.WebApp.WidgetsList._currentWidget = '';

HG.WebApp.WidgetsList.InitializePage = function () {
    var page = $('#'+HG.WebApp.WidgetsList.PageId);
    var importPopup = page.find('[data-ui-field=import-popup]');
    var importButton = page.find('[data-ui-field=import-btn]');
    var importForm = page.find('[data-ui-field=import-form]');
    var uploadButton = page.find('[data-ui-field=upload-btn]');
    var uploadFile = page.find('[data-ui-field=upload-file]');
    var uploadFrame = page.find('[data-ui-field=upload-frame]');
    
    page.on('pageinit', function (e) {
        // initialize controls used in this page
        importPopup.popup();
    });
    page.on('pagebeforeshow', function (e) {
        HG.WebApp.WidgetsList.LoadWidgets();
    });
    
    importButton.bind('click', function () {
        importPopup.popup('open');
    });
    uploadButton.bind('click', function () {
        if (uploadFile.val() == "") {
            alert('Select a file to import first');
            uploadFile.parent().stop().animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250)
                .animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250);
        } else {
            importButton.removeClass('ui-btn-active');
            importPopup.popup('close');
            $.mobile.loading('show', { text: 'Importing, please wait...', textVisible: true, html: '' });
            importForm.submit();
        }
    });
    uploadFrame.bind('load', function () {
        uploadFile.val('');
        $.mobile.loading('hide');
        HG.WebApp.WidgetsList.LoadWidgets();
    });            
};

HG.WebApp.WidgetsList.LoadWidgets = function() {
    $.mobile.loading('show');
    HG.Configure.Widgets.List(function(items) {
        HG.WebApp.WidgetsList.RefreshList(items);
        $.mobile.loading('hide');
    });
};

HG.WebApp.WidgetsList.RefreshList = function(items) {
    var page = $('#'+HG.WebApp.WidgetsList.PageId);
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
        HG.WebApp.WidgetsList._currentWidget = $(this).attr('data-item-name');
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