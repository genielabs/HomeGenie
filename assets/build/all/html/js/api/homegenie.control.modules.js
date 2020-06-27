//
// namespace : HG.Control.Modules namespace
// info      : -
//
HG.Control.Modules = HG.Control.Modules || new function(){ var $$ = this;

    $$.ApiCall = function (domain, address, command, options, callback) {
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + domain + '/' + address + '/' + command + '/' + options + '/', function (data) {
            if (callback != null && typeof callback != 'undefined') {
                callback(data);
            }
        }).fail(function () {
            if (typeof (callback) != 'undefined') callback('');
        });
    };

    // this is left for HG 1.0 compatibility
    $$.ServiceCall = function (fn, domain, nodeid, fnopt, callback) {
        $.get('/' + HG.WebApp.Data.ServiceKey + '/' + domain + '/' + nodeid + '/' + fn + '/' + fnopt + '/', function (data) {
            if (callback != null && typeof callback != 'undefined') {
                callback(data);
            }
        }).fail(function () {
            if (typeof (callback) != 'undefined') callback('');
        });
    };

};