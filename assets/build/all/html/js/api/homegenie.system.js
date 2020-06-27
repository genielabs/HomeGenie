//
// namespace : HG.System
// info      : -
//
HG.System = HG.System || new function(){ var $$ = this;

    $$.GetVersion = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.GetVersion/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.GetBootProgress = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.GetBootProgress/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.SetHttpPort = function (port, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetPort/' + port + '/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.SetHostHeader = function (hostHeader, callback) {
       $.ajax({
           url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetHostHeader/' + hostHeader + '/',
           type: 'GET',
           success: function (data) {
               if (callback != null) callback(data);
           }
       });
    };

    $$.SetPassword = function (pass, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.SetPassword/' + pass + '/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.ClearPassword = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.ClearPassword/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.HasPassword = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.HasPassword/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.LoggingEnable = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Enable/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.LoggingDisable = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Disable/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.LoggingIsEnabled = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.IsEnabled/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data.ResponseValue);
            }
        });
    };

    $$.WebCacheEnable = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/1/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.WebCacheDisable = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/0/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.WebCacheIsEnabled = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.GetWebCacheEnabled/',
            type: 'GET',
            success: function (data) {
                if (callback != null)
                    callback(data.ResponseValue);
            }
        });
    };

    $$.LocationGet = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Location.Get/',
            type: 'GET',
            success: function (data) {
                if (callback != null)
                    callback(data);
            }
        });
    };

    $$.LocationSet = function (locationData, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Location.Set/',
            type: 'POST',
            data: JSON.stringify(locationData),
            success: function (data) {
                if (callback != null)
                    callback(data);
            }
        });
    };
};
