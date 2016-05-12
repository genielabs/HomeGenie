//
// namespace : HG.Configure namespace
// info      : -
//
HG.Configure = HG.Configure || new function(){ var $$ = this;

    $$.LoadData = function(callback) {
        $$.Modules.List(function (data) {
            try {
                HG.WebApp.Data.Modules = eval(data);
            } catch (e) { }
            //
            HG.Automation.Programs.List(function () {
                $$.Groups.List('Control', function () {

                    if (callback != null) callback();

                });
            });
        });
    };

};