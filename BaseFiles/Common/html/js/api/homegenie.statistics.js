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
        dataType: 'text',
        success: function (data) {
            var value = eval(data);
            if (typeof value == 'undefined') {
                value = data;
            }
            else if (typeof value[0] != 'undefined' && typeof value[0].ResponseValue != 'undefined') {
                value = value[0].ResponseValue;
            }
            callback(value);
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
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Database.Reset/' + (new Date().getTime()), function (data) {
    });
};

