//
// namespace : HG.Statistics
// info      : -
//
HG.Statistics = HG.Statistics || {};
//
HG.Statistics.ServiceCall = function (fn, opt1, opt2, callback) {
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
//
// namespace : HG.Statistics.Global
// info      : -
//	
HG.Statistics.Global = HG.Statistics.Global || {};
HG.Statistics.Global.GetWattsCounter = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Global.CounterTotal/Meter.Watts',
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            var counter = eval(data)[0];
            callback(counter.ResponseValue);
        }
    });
};
//
// namespace : HG.Statistics.Database
// info      : -
//	
HG.Statistics.Database = HG.Statistics.Database || {};
HG.Statistics.Database.Reset = function () {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Database.Reset/', function (data) {
    });
};

