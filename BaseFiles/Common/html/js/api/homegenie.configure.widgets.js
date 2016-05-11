//
// namespace : HG.Configure.Widgets namespace
// info      : -
// 
HG.Configure.Widgets = HG.Configure.Widgets || {};
HG.Configure.Widgets.List = function(callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Widgets.List/',
        type: 'GET',
        success: function (data) {
            callback(data);
        }
    });
};
HG.Configure.Widgets.Save = function(widgetPath, fileType, content, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Widgets.Save/' + fileType + '/' + encodeURIComponent(widgetPath),
        type: 'POST',
        data: content,
        processData: false,
        success: function (data) {
            if (callback)
                callback(data);
        },
        error: function (a, b, c) {
            if (callback) callback({ 'Status' : 'Error' });
        }
    });
};
HG.Configure.Widgets.Add = function(widgetPath, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Widgets.Add/' + encodeURIComponent(widgetPath),
        type: 'GET',
        success: function (data) {
            if (callback)
                callback(data);
        },
        error: function (a, b, c) {
            if (callback) callback({ 'Status' : 'Error' });
        }
    });
};
HG.Configure.Widgets.Delete = function(widgetPath, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Widgets.Delete/' + encodeURIComponent(widgetPath),
        type: 'GET',
        success: function (data) {
            if (callback)
                callback(data);
        },
        error: function (a, b, c) {
            if (callback) callback({ 'Status' : 'Error' });
        }
    });
};
HG.Configure.Widgets.Export = function(widgetPath) {
    
};
HG.Configure.Widgets.Parse = function(content, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Widgets.Parse/',
        type: 'POST',
        data: content,
        processData: false,
        success: function (data) {
            if (callback) {
                if (typeof data.ResponseValue != 'undefined')
                    data.ResponseValue = decodeURIComponent(data.ResponseValue);
                callback(data);
            }
        },
        error: function (a, b, c) {
            if (callback) callback({ 'ResponseValue' : 'ERROR' });
        }
    });
};