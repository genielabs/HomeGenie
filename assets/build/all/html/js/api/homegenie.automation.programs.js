//
// namespace : HG.Automation.Programs namespace
// info      : -
//
HG.Automation.Programs = HG.Automation.Programs || new function(){ var $$ = this;

    $$.List = function (callback) {
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.List/', function (data) {
            HG.WebApp.Data.Programs = eval(arguments[2].responseText);
            callback();
        });
    };

    $$.AddProgram = function (group, program, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Add/' + group + '/',
            type: 'POST',
            data: program,
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                callback(data);
            },
            error: function (a, b, c) {
                alert('A problem ocurred');
            }
        });
    };

    $$.DeleteProgram = function (program, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Delete/' + program + '/',
            type: 'GET',
            success: function (response) {
                callback();
            },
            error: function (a, b, c) {
                alert('A problem ocurred');
            }
        });
    };

    $$.Run = function (pid, options, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Run/' + pid + '/' + options,
            type: 'GET',
            success: function (response) {
                if (typeof callback != 'undefined') callback(response);
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };
    
    $$.HasConfigurationOptions = function(programAddress) {
        var hasOptions = false;
        var module = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation', programAddress);
        if (module != null) {
            for (var p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name.substring(0, 17) === 'ConfigureOptions.') {
                    hasOptions = true;
                    break;
                }
            }
        }
        return hasOptions;
    };

    $$.Toggle = function (pid, options, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Toggle/' + pid + '/' + options,
            type: 'GET',
            success: function (response) {
                if (typeof callback != 'undefined') callback(response);
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

    $$.ArduinoFileLoad = function (pid, filename, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Arduino.FileLoad/' + pid + '/' + filename,
            type: 'GET',
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                if (typeof callback != 'undefined') callback(unescape(data));
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

    $$.ArduinoFileAdd = function (pid, filename, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Arduino.FileAdd/' + pid + '/' + filename,
            type: 'GET',
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                if (typeof callback != 'undefined') callback(unescape(data));
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

    $$.ArduinoFileDelete = function (pid, filename, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Arduino.FileDelete/' + pid + '/' + filename,
            type: 'GET',
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                if (typeof callback != 'undefined') callback(unescape(data));
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

    $$.ArduinoFileSave = function (pid, filename, srctext, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Arduino.FileSave/' + pid + '/' + filename,
            type: 'POST',
            data: srctext,
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                if (typeof callback != 'undefined') callback(unescape(data));
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

    $$.ArduinoFileList = function (pid, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Arduino.FileList/' + pid,
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined')
                    callback(data);
            },
            error: function (a, b, c) {
                if (typeof callback != 'undefined') callback(null);
            }
        });
    };

};
