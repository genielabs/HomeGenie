//
// namespace : HG.Configure.Modules namespace
// info      : -
//  
HG.Configure.Modules = HG.Configure.Modules || {};
HG.Configure.Modules.List = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.List/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.Get = function (domain, address, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Get/' + domain + '/' + address + '/',
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.Delete = function (domain, address, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Delete/' + domain + '/' + address + '/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.RoutingReset = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.RoutingReset/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.ParameterSet = function (domain, address, parameter, value, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.ParameterSet/' + domain + '/' + address + '/' + parameter + '/' + encodeURIComponent(value),
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};