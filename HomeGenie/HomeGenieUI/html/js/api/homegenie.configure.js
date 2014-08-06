//
// namespace : HG.Configure.Groups namespace
// info      : -
//
HG.Configure = HG.Configure || {};
//
HG.Configure.LoadData = function (callback) {
    HG.Configure.Modules.List(function (data) {
        try {
            HG.WebApp.Data.Modules = eval(data);
        } catch (e) { }
        //
        HG.Automation.Programs.List(function () {
            HG.Configure.Groups.List('Control', function () {

                if (callback != null) callback();

            });
        });
    });
};
//
HG.Configure.Groups = HG.Configure.Groups || {};
HG.Configure.Groups.ModulesList = function (groupname, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.ModulesList/' + groupname + '/' + (new Date().getTime()), function (data) {
        callback(eval(arguments[2].responseText));
    });
};

HG.Configure.Groups.List = function (context, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.List/' + context + '/' + (new Date().getTime()), function (data) {
        if (context == 'Automation') {
            HG.WebApp.Data.AutomationGroups = eval(arguments[2].responseText);
        }
        else {
            HG.WebApp.Data.Groups = eval(arguments[2].responseText);
        }
        callback();
    });
};

HG.Configure.Groups.Sort = function (context, sortorder, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Sort/' + context + '/' + (new Date().getTime()),
        data: sortorder,
        success: function (response) {
            callback(response);
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};

HG.Configure.Groups.SortModules = function (context, groupname, sortorder, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.SortModules/' + context + '/' + groupname + '/' + (new Date().getTime()),
        data: sortorder,
        success: function (response) {
            callback(response);
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};


HG.Configure.Groups.RenameGroup = function (context, oldname, newname, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Rename/' + context + '/' + oldname + '/' + (new Date().getTime()),
        data: newname,
        success: function (response) {
            callback(response);
        },
        error: function (a, b, c) {
            alert('Error.\nThere is aready a group with this name.');
        }
    });
};


HG.Configure.Groups.AddGroup = function (context, group, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Add/' + context + '/' + (new Date().getTime()),
        data: group,
        success: function (response) {
            callback();
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};
HG.Configure.Groups.DeleteGroup = function (context, group, callback) {
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Delete/' + context + '/' + (new Date().getTime()),
        data: group,
        success: function (response) {
            callback();
        },
        error: function (a, b, c) {
            alert('A problem ocurred');
        }
    });
};

HG.Configure.Groups.GetGroupModules = function (groupname) {
    var groupmodules = { 'Index': 0, 'Name': groupname, 'Modules': Array() };
    //
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        if (HG.WebApp.Data.Groups[i].Name == groupname) {
            groupmodules.Index = i;
            for (var c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
                var found = false;
                //
                for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                    if (HG.WebApp.Data.Modules[m].Domain == HG.WebApp.Data.Groups[i].Modules[c].Domain && HG.WebApp.Data.Modules[m].Address == HG.WebApp.Data.Groups[i].Modules[c].Address) {
                        groupmodules.Modules.push(HG.WebApp.Data.Modules[m]);
                        found = true;
                        break;
                    }
                }
                //
                if (!found) {
                    // orphan module/program, it is not present in the modules list nor programs one
                    groupmodules.Modules.push(HG.WebApp.Data.Groups[i].Modules[c]);
                }
            }
            break;
        }
    }
    //
    return groupmodules;
};

//
// namespace : HG.Configure.Interfaces namespace
// info      : -
//
HG.Configure.Interfaces = HG.Configure.Interfaces || {};
HG.Configure.Interfaces.ServiceCall = function (ifacefn, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.Configure/' + ifacefn + '/',
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            var value = eval(data);
            if (typeof value == 'undefined') {
                value = data;
            }
            else if (typeof value[0] != 'undefined' && typeof value[0].ResponseValue != 'undefined') {
                try {
                    value = value[0].ResponseValue;
                } catch (e) { value = data; }
            }
            callback(value);
        }
    });
};

HG.Configure.MIG = HG.Configure.MIG || {};
HG.Configure.MIG.InterfaceCommand = function (domain, command, option1, option2, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/MIGService.Interfaces/' + domain + '/' + command + '/' + option1 + '/' + option2 + '/' + (new Date().getTime()), function (data) {
    	var res = '';
    	try {
    		res = eval(data)[0];
    		if (res.ResponseValue) res.ResponseValue = decodeURIComponent(res.ResponseValue);
    	} catch (e) { }
    	if (callback) callback(res);
    });
};
//
// namespace : HG.Configure.Modules namespace
// info      : -
//	
HG.Configure.Modules = HG.Configure.Modules || {};
HG.Configure.Modules.List = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.List/' + (new Date().getTime()),
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "json",
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.Get = function (domain, address, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Get/' + domain + "/" + address + "/" + (new Date().getTime()),
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.Delete = function (domain, address, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Delete/' + domain + "/" + address + "/" + (new Date().getTime()),
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Modules.RoutingReset = function (callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.RoutingReset/' + (new Date().getTime()),
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
//
// namespace : HG.Configure.System namespace
// info      : -
//	
HG.Configure.System = HG.Configure.System || {};
HG.Configure.System.ServiceCall = function (systemfn, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/' + systemfn + '/',
        type: "POST",
        data: "{ dummy: 'dummy' }",
        dataType: "text",
        success: function (data) {
            var value = eval(data);
            if (typeof value == 'undefined') {
                value = data;
            }
            else if (typeof value[0] != 'undefined' && typeof value[0].ResponseValue != 'undefined') {
                try {
                    value = value[0].ResponseValue;
                } catch (e) { value = data; }
            }
            callback(value);
        }
    });
};
