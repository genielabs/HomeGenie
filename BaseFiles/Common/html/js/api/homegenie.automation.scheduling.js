//
// namespace : HG.Automation.Scheduling namespace
// info      : -
//  
HG.Automation.Scheduling = HG.Automation.Scheduling || new function(){ var $$ = this;

    $$.Get = function (name, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Get/' + encodeURIComponent(name),
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.Update = function (name, expression, data, description, script, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Update/' + encodeURIComponent(name),
            type: 'POST',
            dataType: 'text',
            data: JSON.stringify({
                Name: name,
                CronExpression: expression,
                Data: data,
                Description: description,
                Script: script
            }),
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.UpdateItem = function (name, item, callback) {
        item.Name = name; // not allowing rename
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Update/' + encodeURIComponent(name),
            type: 'POST',
            dataType: 'text',
            data: JSON.stringify(item),
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.Delete = function (name, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Delete/' + encodeURIComponent(name),
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.Enable = function (name, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Enable/' + encodeURIComponent(name),
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.Disable = function (name, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Disable/' + encodeURIComponent(name),
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

    $$.List = function (callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.List/',
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            }
        });
    };

};