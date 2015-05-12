HG.Ui = HG.Ui || {};
eval('hg = {}; hg.ui =  HG.Ui;');
HG.Ui._widgetQueueCount = 0;
HG.Ui._widgetCache = [];

HG.Ui.Popup = HG.Ui.Popup || {};

HG.Ui.GenerateWidget = function(fieldType, context, callback) {
    // fieldType: 
    //    widgets/text, widgets/password, widgets/checkbox, widgets/slider, 
    //    widgets/store.text, widgets/store.password, widgets/store.checkbox,
    //    widgets/store.list, store.edit
    //    core/popup.cronwizard
    var widgetWrapper = $('<div/>');
    context.parent.append(widgetWrapper);
    var options = [];
    if (fieldType.indexOf(':') > 0) {
        options = fieldType.split(':');
        fieldType = options[0];
        options.shift();
    }
    // pick it from cache if exists
    var cached = false;
    $.each(HG.Ui._widgetCache, function(k,v){
        if (v.widget == fieldType) {
            var element = $(v.html);
            var handler = eval(v.json)[0];
            element.one('create', function() {
                handler.element = element;
                handler.context = context;
                setTimeout(function(){
                    callback(handler);
                }, 200);
                if (handler.init) handler.init(options);
                if (handler.bind) handler.bind();
                widgetWrapper.show();
            });
            widgetWrapper.hide();
            widgetWrapper.append(element);
            element.trigger('create');
            cached = true;
            return false;
        }
    });
    if (cached) return widgetWrapper;
    // ... or load it from web
    if (HG.Ui._widgetQueueCount++ == 0)
        $.mobile.loading('show');
    $.ajax({
        url: "ui/" + fieldType + ".html",
        type: 'GET',
        success: function (htmlData) {
            var element = $(htmlData);
            $.ajax({
                url: "ui/" + fieldType + ".js",
                type: 'GET',
                success: function (jsonData) {
                    HG.Ui._widgetCache.push({ widget: fieldType, html: htmlData, json: jsonData });
                    var handler = null;
                    try { handler = eval(jsonData)[0]; }
                    catch (e) { console.log(e); callback(null); return; }
                    element.one('create', function() {
                        handler.element = element;
                        handler.context = context;
                        callback(handler);
                        if (handler.init) handler.init(options);
                        if (handler.bind) handler.bind();
                        widgetWrapper.show();
                    });
                    widgetWrapper.hide();
                    widgetWrapper.append(element);
                    element.trigger('create');
                    if (--HG.Ui._widgetQueueCount == 0) {
                        $.mobile.loading('hide');
                    }
                },
                error: function (data) {
                    if (callback != null) callback(null);
                }
            });
        },
        error: function (data) {
            if (callback != null) callback(null);
        }
    });
    return widgetWrapper; 
};

HG.Ui.GetModuleIcon = function(module, callback, elid) {
    var icon = 'pages/control/widgets/homegenie/generic/images/unknown.png';
    if (module != null && module.DeviceType && module.DeviceType != '' && module.DeviceType != 'undefined') {
        var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
        var widget = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayModule');
        if (widget != null && widget.Value != '') {
            widget = widget.Value;
        }
        else {
            widget = 'homegenie/generic/' + module.DeviceType.toLowerCase();
        }
        //
        if (widgeticon != null && widgeticon.Value != '') {
            icon = widgeticon.Value;
        }
        else if (module.WidgetInstance && module.WidgetInstance != null && module.WidgetInstance != 'undefined') {
            icon = module.WidgetInstance.IconImage;
        }
        else {
            // get reference to generic type widget 
            HG.WebApp.WidgetsList.GetWidgetIcon(widget, elid, callback);
            return icon;
        }
    }
    if (callback != null) callback(icon, elid);
    return icon;
};
