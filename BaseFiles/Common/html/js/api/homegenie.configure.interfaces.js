//
// namespace : HG.Configure.Interfaces namespace
// info      : -
//
HG.Configure.Interfaces = HG.Configure.Interfaces || new function(){ var $$ = this;

    $$.ServiceCall = function (ifacefn, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/' + ifacefn + '/',
            type: 'GET',
            success: function (data) {
                callback(data);
            }
        });
    };

    $$.ListConfig = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.ListConfig/',
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined' && callback != null) callback(data);
            }
        });
    };

};