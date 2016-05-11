//
// namespace : HG.System.UpdateManager namespace
// info      : -
//

HG.System.UpdateManager = HG.System.UpdateManager || {};
HG.System.UpdateManager.UpdateCheck = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.Check/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.GetUpdateList = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.UpdatesList/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.DownloadUpdate = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.DownloadUpdate/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};


HG.System.UpdateManager.InstallProgramsList = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallProgramsList/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};

HG.System.UpdateManager.InstallUpdate = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallUpdate/',
        type: 'GET',
        success: function (data) {
            if (callback != null) callback(data);
        }
    });
};