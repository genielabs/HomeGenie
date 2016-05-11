//
// namespace : HG.Configure.MIG namespace
// info      : -
//
HG.Configure.MIG = HG.Configure.MIG || {};
HG.Configure.MIG.InterfaceCommand = function (domain, command, option1, option2, callback) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/MIGService.Interfaces/' + domain + '/' + command + '/' + option1 + '/' + option2 + '/', function (data) {
        if (callback) callback(data);
    });
};