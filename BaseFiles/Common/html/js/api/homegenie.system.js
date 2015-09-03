//
// namespace : HG.System
// info      : -
//
HG.System = HG.System || {};
//
HG.System.SetHttpPort = function (port, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetPort/' + port + '/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.SetHostHeader = function (hostHeader, callback) {
   $.ajax({
       url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetHostHeader/' + hostHeader + '/' + (new Date().getTime()),
       type: 'GET',
       success: function (data) {
           if (callback != null) callback(data);
       }
   });
};
HG.System.SetPassword = function (pass, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.SetPassword/' + pass + '/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.ClearPassword = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.ClearPassword/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.HasPassword = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Security.HasPassword/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            //var haspass = eval(data)[0];
            if (callback != null) callback(data.ResponseValue);
        }
    });
};
HG.System.LoggingEnable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Enable/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.LoggingDisable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.Disable/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.LoggingIsEnabled = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/SystemLogging.IsEnabled/' + (new Date().getTime()),
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            var haslog = eval(data)[0];
            if (callback != null) callback(haslog.ResponseValue);
        }
    });
};


HG.System.WebCacheEnable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/1/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.WebCacheDisable = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.SetWebCacheEnabled/0/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
HG.System.WebCacheIsEnabled = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/HttpService.GetWebCacheEnabled/' + (new Date().getTime()),
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
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Statistics.SetStatisticsDatabaseMaximumSize/' + mb + '/' + (new Date().getTime()),
        type: 'GET',
        dataType: "text",
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};


HG.System.UpdateManager = HG.System.UpdateManager || {};
HG.System.UpdateManager.UpdateCheck = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.Check/' + (new Date().getTime()),
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.GetUpdateList = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.UpdatesList/' + (new Date().getTime()),
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.DownloadUpdate = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.DownloadUpdate/' + (new Date().getTime()),
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};


HG.System.UpdateManager.InstallProgramsList = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallProgramsList/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.InstallUpdate = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallUpdate/' + (new Date().getTime()),
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};
