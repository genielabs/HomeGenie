//
// namespace : HG.Configure.Interfaces namespace
// info      : -
//
HG.Configure.Interfaces = HG.Configure.Interfaces || {};
HG.Configure.Interfaces.ServiceCall = function (ifacefn, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/' + ifacefn + '/',
        type: 'GET',
        success: function (data) {
            callback(data);
        }
    });
};
HG.Configure.Interfaces.ListConfig = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.ListConfig/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};