HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || {};
HG.WebApp.SystemSettings.PageId = 'page_configure_interfaces';
HG.WebApp.SystemSettings.Interfaces = HG.WebApp.SystemSettings.Interfaces || {};

HG.WebApp.SystemSettings.InitializePage = function () {
    var page = $('#'+HG.WebApp.SystemSettings.PageId);
    var importPopup = page.find('[data-ui-field=import-popup]');
    var importButton = page.find('[data-ui-field=interface_import]');
    var importForm = page.find('[data-ui-field=import-form]');
    var uploadButton = page.find('[data-ui-field=upload-btn]');
    var uploadFile = page.find('[data-ui-field=upload-file]');
    var uploadFrame = page.find('[data-ui-field=upload-frame]');
    page.on('pageinit', function (e) {
        // initialize controls used in this page
        importPopup.popup();
    });    
    page.on('pagebeforeshow', function (e) {
        HG.WebApp.SystemSettings.ListInterfaces();
    });
    importButton.on('click', function() {
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
        $.mobile.loading('hide');
        // import completed...
        if (uploadFile.val() != '') {
            uploadFile.val('');
            HG.WebApp.SystemSettings.ListInterfaces();        
        }        
    });
};

HG.WebApp.SystemSettings.ListInterfaces = function() {
    var page = $('#'+HG.WebApp.SystemSettings.PageId);
    var interfaceList = page.find('[data-ui-field=interface_list]');
    HG.Configure.Interfaces.ListConfig(function(ifaceList){
        interfaceList.empty();
        $.each(ifaceList, function(k,v){
            var domain = v.Domain;
            var name = domain.substring(domain.lastIndexOf('.')+1);
            var item = $('<div data-role="collapsible" data-inset="true" />');
            var itemHeader = $('<h3><span data-ui-field="title">'+name+'</span><img src="images/interfaces/'+name.toLowerCase()+'.png" style="position:absolute;right:8px;top:12px"></h3>');
            item.append(itemHeader);
            var configlet = $('<p />');
            item.append(configlet);
            configlet.load('pages/configure/interfaces/configlet/'+name.toLowerCase()+'.html', function(){
                var displayName = name;
                if (HG.WebApp.SystemSettings.Interfaces[domain].Localize) {
                    HG.WebApp.SystemSettings.Interfaces[domain].Localize();
                    displayName = HG.WebApp.Locales.GetLocaleString('title', name, HG.WebApp.SystemSettings.Interfaces[domain].Locale);
                }
                itemHeader.find('[data-ui-field=title]').html(displayName);
                configlet.trigger('create');
                HG.WebApp.SystemSettings.Interfaces[domain].Initialize();
            });
            interfaceList.append(item);
        });
        interfaceList.trigger('create');
    });        
}

HG.WebApp.SystemSettings.GetInterface = function (domain) {
    var iface = null;
    var interfaces = HG.WebApp.Data.Interfaces;
    if (interfaces && interfaces != 'undefined') {
        for (i = 0; i < interfaces.length; i++) {
            if (interfaces[i].Domain == domain) {
                iface = interfaces[i];
                break;
            }
        }
    }
    return iface;
};

HG.WebApp.SystemSettings.ShowPortTip = function (el) {
    $(el).qtip({
        content: {
            title: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_title'),
            text: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_text'),
            button: HG.WebApp.Locales.GetLocaleString('systemsettings_selectport_button')
        },
        show: { event: false, ready: true, delay: 1000 },
        events: {
            hide: function () {
                $(this).qtip('destroy');
            }
        },
        hide: { event: false, inactive: 3000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { my: 'left center', at: 'right center' }
    });
};