//
// namespace : HG.System
// info      : -
//
HG.System = HG.System || {};
//
HG.System.GetVersion = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.GetVersion/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.SetHttpPort = function (port, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetPort/' + port + '/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.SetHostHeader = function (hostHeader, callback) {
   $.ajax({
       url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetHostHeader/' + hostHeader + '/',
       type: 'GET',
       success: function (data) {
           if (callback != null) callback(data);
       }
   });
};
HG.System.SetPassword = function (pass, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.SetPassword/' + pass + '/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.ClearPassword = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.ClearPassword/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.HasPassword = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.HasPassword/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.LoggingEnable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Enable/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.LoggingDisable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Disable/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.LoggingIsEnabled = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.IsEnabled/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data.ResponseValue);
        }
    });
};


HG.System.WebCacheEnable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/1/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.WebCacheDisable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/0/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.WebCacheIsEnabled = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.GetWebCacheEnabled/',
        type: 'GET',
        success: function (data) {
            if (callback != null)
                callback(data.ResponseValue);
        }
    });
};

// Should this be added to a new namespace like "HG.System.Statistics"? It's a setting, so thought it might not belong in homegenie.statstics.js... Opinions?
HG.System.SetStatisticsDatabaseMaximumSize = function (mb, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Statistics.SetStatisticsDatabaseMaximumSize/' + mb + '/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};