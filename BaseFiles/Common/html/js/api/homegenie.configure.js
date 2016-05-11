//
// namespace : HG.Configure.Groups namespace
// info      : -
//
HG.Configure = HG.Configure || {};

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