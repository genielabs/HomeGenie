//
// namespace : HG.Automation.Macro namespace
// info      : -
//
HG.Automation.Macro = HG.Automation.Macro || new function(){ var $$ = this;

    $$.Record = function () {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Record/',
            type: 'GET'
        });
    };

    $$.Save = function (mode, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Save/' + mode + '/',
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                callback(data);
            }
        });
    };

    $$.Discard = function () {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.Discard/',
            type: 'GET'
        });
    };

    $$.SetDelay = function (type, args) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Macro.SetDelay/' + type + '/' + args,
            type: 'GET'
        });
    };

    $$.GetDelay = function (callback) {
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

};