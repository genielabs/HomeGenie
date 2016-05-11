//
// namespace : HG.Automation.Macro namespace
// info      : -
//
HG.Automation.Macro = HG.Automation.Macro || {};
HG.Automation.Macro.Record = function () {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Record/',
        type: 'GET'
    });
};
//
HG.Automation.Macro.Save = function (mode, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Save/' + mode + '/',
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            callback(data);
        }
    });
};
//
HG.Automation.Macro.Discard = function () {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Discard/',
        type: 'GET'
    });
};
//
HG.Automation.Macro.SetDelay = function (type, args) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.SetDelay/' + type + '/' + args,
        type: 'GET'
    });
};

HG.Automation.Macro.GetDelay = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.GetDelay/',
        type: 'GET',
        success: function (data) {
            callback(data);
        },
        error: function(xhr, status, error) {
            console.log('HG.Automation.Macro.GetDelay ERROR: '+xhr.status+':'+xhr.statusText);
            callback();
        }
    });
};