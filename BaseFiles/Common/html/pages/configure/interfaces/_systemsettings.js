HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || {};
HG.WebApp.SystemSettings.PageId = 'page_configure_interfaces';
HG.WebApp.SystemSettings.Interfaces = HG.WebApp.SystemSettings.Interfaces || {};

HG.WebApp.SystemSettings.InitializePage = function () {
    var page = $('#'+HG.WebApp.SystemSettings.PageId);
    var importPopup = page.find('[data-ui-field=import-popup]');
    var importButton = page.find('[data-ui-field=interface_import]');
    var importForm = page.find('[data-ui-field=import-form]');
    var downloadButton = page.find('[data-ui-field=download-btn]');
    var downloadUrl = page.find('[data-ui-field=download-url]');
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
    importPopup.on('popupafteropen', function(event) {
        importButton.removeClass('ui-btn-active');
    });
    importButton.on('click', function() {
        importPopup.popup('open');
    });
    downloadButton.on('click', function() {
        if (downloadUrl.val() == '') {
            alert('Insert add-on package URL');
            downloadUrl.parent().stop().animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250)
                .animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250);
        } else {
            importPopup.popup('close');
            $.mobile.loading('show', { text: 'Downloading, please wait...', textVisible: true, html: '' });
            $.get('../HomeAutomation.HomeGenie/Config/Interface.Import/'+encodeURIComponent(downloadUrl.val()), function(data){
                $.mobile.loading('hide');
                downloadUrl.val('');
                var response = eval(arguments[2].responseText)[0];
                HG.WebApp.SystemSettings.AddonInstall(response.ResponseValue);
            });
        }
    });
    uploadButton.on('click', function () {
        if (uploadFile.val() == '') {
            alert('Select a file to import first');
            uploadFile.parent().stop().animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250)
                .animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250);
        } else {
            importPopup.popup('close');
            $.mobile.loading('show', { text: 'Uploading, please wait...', textVisible: true, html: '' });
            importForm.submit();
        }
    });
    uploadFrame.bind('load', function () {
        $.mobile.loading('hide');
        // import completed...
        uploadFile.val('');
        var response = uploadFrame[0].contentWindow.document.body;
        if (typeof response != 'undefined' && response != '' && (response.textContent || response.innerText)) {
            response = eval(response.textContent || response.innerText)[0];
            HG.WebApp.SystemSettings.AddonInstall(response.ResponseValue);
        }
    });
};

HG.WebApp.SystemSettings.AddonInstall = function(text) {
    var urlRegex = /(https?:\/\/[^\s]+)/g;
    text = text.replace(urlRegex, '<a href="$1" target="_blank">$1</a>');
    HG.WebApp.Utility.ConfirmPopup(HG.WebApp.Locales.GetLocaleString('systemsettings_addonsinstall_title', 'Install add-on?'), '<pre>'+text+'</pre>', function(confirm){
        if (confirm) {
            $.mobile.loading('show', { text: 'Installing, please wait...', textVisible: true, html: '' });
            $.get('../HomeAutomation.HomeGenie/Config/Interface.Install', function(data){
                HG.WebApp.SystemSettings.ListInterfaces();        
                $.mobile.loading('hide');
            });
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
            var item = $('<div data-role="collapsible" data-inset="true" class="ui-mini" />');
            var itemHeader = $('<h3><span data-ui-field="title">'+name+'</span><img src="images/interfaces/'+name.toLowerCase()+'.png" style="position:absolute;right:8px;top:6px"></h3>');
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
                itemHeader.attr('description', v.Description);
                if (v.Description != null && v.Description.trim() != '') {
                    itemHeader.qtip({
                        content: {
                            text: v.Description
                        },
                        show: { event: 'mouseover', ready: false, delay: 500 },
                        hide: { event: 'mouseout' },
                        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                        position: { my: 'bottom center', at: 'top center' }
                    });
                }
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