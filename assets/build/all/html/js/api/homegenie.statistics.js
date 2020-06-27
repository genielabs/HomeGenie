//
// namespace : HG.Statistics
// info      : -
//
HG.Statistics = HG.Statistics || new function(){ var $$ = this;

    $$.ServiceCall = function (fn, opt1, opt2, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/' + fn + '/' + opt1 + '/' + opt2,
            type: 'GET',
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                callback(data);
            },
            error: function(xhr, status, error) {
                console.log('HG.Statistics.ServiceCall ERROR: '+xhr.status+':'+xhr.statusText);
                callback();
            }
        });
    };

    $$.DatabaseReset = function () {
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Database.Reset/', function (data) {
        });
    };

    $$.SetStatisticsDatabaseMaximumSize = function (mb, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/Statistics.SetStatisticsDatabaseMaximumSize/' + mb + '/',
            type: 'GET',
            success: function (data) {
                if (callback != null) callback(data);
            }
        });
    };

};
