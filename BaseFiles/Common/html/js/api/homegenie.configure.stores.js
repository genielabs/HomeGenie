//
// namespace : HG.Configure.Stores namespace
// info      : -
//  
HG.Configure.Stores = HG.Configure.Stores || new function(){ var $$ = this;

    $$.ItemGet = function (domain, address, store, item, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemGet/' + domain + '/' + address + '/' + store + '/' + item + '/',
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined' && callback != null) callback(data);
            }
        });
    };

    $$.ItemSet = function (domain, address, store, item, value, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemSet/' + domain + '/' + address + '/' + store + '/' + item + '/',
            type: 'POST',
            data: value,
            success: function (data) {
                if (typeof callback != 'undefined' && callback != null) callback(data);
            }
        });
    };

    $$.ItemDelete = function (domain, address, store, item, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemDelete/' + domain + '/' + address + '/' + store + '/' + item + '/',
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined' && callback != null) callback(data);
            }
        });
    };

    $$.ItemList = function (domain, address, store, callback) {
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemList/' + domain + '/' + address + '/' + store + '/',
            type: 'GET',
            success: function (data) {
                if (typeof callback != 'undefined' && callback != null) callback(data);
            }
        });
    };

};