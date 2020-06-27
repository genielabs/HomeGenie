[{
    Name: "UI Link",
    Author: "Generoso Martello",
    Version: "2013-03-31",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/link.png',
    StatusText: '',
    Description: '',
    Initialized: false,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        var link = HG.WebApp.Utility.GetModulePropertyByName(module, "FavouritesLink.Url");
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        if (!this.Initialized) {
            this.Initialized = true;
            //
            var groupname = this.GroupName;
            widget.on('click', function () {
                //HG.Automation.Programs.Run(module.Address, groupname, null);
                if (link != '') {
                    window.open(link.Value);
                }
            });
        }
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name);
    }
}]