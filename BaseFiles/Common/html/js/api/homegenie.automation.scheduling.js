//
// namespace : HG.Automation.Scheduling namespace
// info      : -
//  
HG.Automation.Scheduling = HG.Automation.Scheduling || {};
HG.Automation.Scheduling.Update = function (name, expression, pid, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Update/' + name + '/' + expression.replace(/\//g, '|') + '/' + pid,
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (typeof callback != 'undefined')
                callback(data);
        }
    });
};
HG.Automation.Scheduling.Delete = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Delete/' + name,
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (typeof callback != 'undefined')
                callback(data);
        }
    });
};
HG.Automation.Scheduling.Enable = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Enable/' + name,
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (typeof callback != 'undefined')
                callback(data);
        }
    });
};
HG.Automation.Scheduling.Disable = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Disable/' + name,
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            if (typeof callback != 'undefined')
                callback(data);
        }
    });
};
HG.Automation.Scheduling.List = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.List/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined')
                callback(data);
        }
    });
};
