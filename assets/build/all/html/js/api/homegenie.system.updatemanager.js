//
// namespace : HG.System.UpdateManager namespace
// info      : -
//
HG.System.UpdateManager = HG.System.UpdateManager || new function(){ var $$ = this;

    $$.UpdateCheck = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.Check/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.GetUpdateList = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.UpdatesList/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.DownloadUpdate = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.DownloadUpdate/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.InstallProgramsList = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallProgramsList/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

    $$.InstallUpdate = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/UpdateManager.InstallUpdate/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

};