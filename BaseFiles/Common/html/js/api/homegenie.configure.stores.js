//
// namespace : HG.Configure.Stores namespace
// info      : -
//  
HG.Configure.Stores = HG.Configure.Stores || {};
HG.Configure.Stores.ItemGet = function (domain, address, store, item, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemGet/' + domain + '/' + address + '/' + store + '/' + item + '/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Stores.ItemSet = function (domain, address, store, item, value, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemSet/' + domain + '/' + address + '/' + store + '/' + item + '/',
        type: 'POST',
        data: value,
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Stores.ItemDelete = function (domain, address, store, item, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemDelete/' + domain + '/' + address + '/' + store + '/' + item + '/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};
HG.Configure.Stores.ItemList = function (domain, address, store, callback) {
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Stores.ItemList/' + domain + '/' + address + '/' + store + '/',
        type: 'GET',
        success: function (data) {
            if (typeof callback != 'undefined' && callback != null) callback(data);
        }
    });
};