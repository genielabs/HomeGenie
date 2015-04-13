HG.WebApp.SystemSettings = HG.WebApp.SystemSettings || {};
HG.WebApp.SystemSettings.PageId = 'page_configure_interfaces';
HG.WebApp.SystemSettings.Interfaces = HG.WebApp.SystemSettings.Interfaces || {};

HG.WebApp.SystemSettings.InitializePage = function () {
    
    $('#page_configure_interfaces').on('pagebeforeshow', function (e) {
        HG.Configure.Interfaces.ListConfig(function(ifaceList){
            $('#page_configure_interfaces_list').empty();
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
                $('#page_configure_interfaces_list').append(item);
            });
            $('#page_configure_interfaces_list').trigger('create');
        });        
    });
    
};

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