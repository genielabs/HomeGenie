//
// namespace : HG.Automation.Groups namespace
// info      : -
//
HG.Automation = HG.Automation || {};
//
HG.Automation.Groups = HG.Automation.Groups || {};
HG.Automation.Groups.LightsOff = function (group) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Groups.LightsOff/" + group + "/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text"
    });
};
HG.Automation.Groups.LightsOn = function (group) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Groups.LightsOn/" + group + "/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text"
    });
};
//

//
// namespace : HG.Automation.Macro namespace
// info      : -
//
HG.Automation.Macro = HG.Automation.Macro || {};
HG.Automation.Macro.Record = function () {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Macro.Record/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text"
    });
};
//
HG.Automation.Macro.Save = function (mode, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Macro.Save/" + mode + "/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(data);
        }
    });
};
//
HG.Automation.Macro.Discard = function () {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Macro.Discard/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text"
    });
};
//
HG.Automation.Macro.SetDelay = function (type, args) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Macro.SetDelay/" + type + "/" + args,
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text"
    });
};

HG.Automation.Macro.GetDelay = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + "/Automation/Macro.GetDelay/",
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            var value = eval(data)[0];
            callback(value);
        }
    });
};


//
// namespace : HG.Automation.Programs namespace
// info      : -
//
HG.Automation.Programs = HG.Automation.Programs || {};
HG.Automation.Programs.List = function (callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.List/' + (new Date().getTime()), function (data) {
        HG.WebApp.Data.Programs = eval(arguments[2].responseText);
        callback();
    });
};
HG.Automation.Programs.AddProgram = function (group, program, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Add/' + group + '/' + (new Date().getTime()),
        data: program,
        dataType: "text",
        success: function (data) {
            var value = eval(data);
            if (value == 'undefined') {
                value = data;
            }
            else {
                try {
                    value = value[0].ResponseValue;
                } catch (e) { value = data; }
            }
            callback(value);
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};
HG.Automation.Programs.DeleteProgram = function (program, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Delete/' + program + '/' + (new Date().getTime()),
        data: "{ dummy: 'dummy' }",
        success: function (response) {
            callback();
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};

HG.Automation.Programs.Run = function (pid, options, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Run/' + pid + '/' + options,
        data: "{ dummy: 'dummy' }",
        success: function (response) {
            if (callback != null) callback(response);
        },
        error: function (a, b, c) {
            if (callback != null) callback(null);
        }
    });
};


//
// namespace : HG.Automation.Scheduling namespace
// info      : -
//	
HG.Automation.Scheduling = HG.Automation.Scheduling || {};
HG.Automation.Scheduling.Update = function (name, expression, pid, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Update/' + name + '/' + expression.replace(/\//g, '|') + '/' + pid,
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(data);
        }
    });
};
HG.Automation.Scheduling.Delete = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Delete/' + name,
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(data);
        }
    });
};
HG.Automation.Scheduling.Enable = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Enable/' + name,
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(data);
        }
    });
};
HG.Automation.Scheduling.Disable = function (name, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.Disable/' + name,
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(data);
        }
    });
};
HG.Automation.Scheduling.List = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Scheduling.List/',
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            callback(eval(arguments[2].responseText));
        }
    });
};