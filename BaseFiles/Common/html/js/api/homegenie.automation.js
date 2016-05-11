//
// namespace : HG.Automation.Groups namespace
// info      : -
//
HG.Automation = HG.Automation || {};
//
HG.Automation.Groups = HG.Automation.Groups || {};
HG.Automation.Groups.LightsOff = function (group) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Groups.LightsOff/' + group + '/',
        type: 'GET'
    });
};
HG.Automation.Groups.LightsOn = function (group) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Groups.LightsOn/' + group + '/',
        type: 'GET'
    });
};