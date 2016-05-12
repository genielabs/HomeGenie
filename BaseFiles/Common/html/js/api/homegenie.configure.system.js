//
// namespace : HG.Configure.System namespace
// info      : -
//  
HG.Configure.System = HG.Configure.System || new function(){ var $$ = this;

    $$.ServiceCall = function (systemfn, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/System.Configure/' + systemfn + '/',
            type: 'GET',
            success: function (data) {
                if (typeof data.ResponseValue != 'undefined')
                    data = data.ResponseValue;
                callback(data);
            }
        });
    };

};