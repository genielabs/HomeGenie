[{
    Name: "UI Group Label/Separator",
    Author: "Generoso Martello",
    Version: "2014-07-03",

    GroupName: '',
    IconImage: '',
    StatusText: '',
    Description: '',

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = container.find('[data-ui-field=widget]');
        //
        // render widget
        //
        if (module.Address.length > 0 && module.Address[0] != ':') {
            widget.find('[data-ui-field=name]').html(module.Address);
        }
        else {
            widget.find('[data-ui-field=name]').parent().html('').parent().parent().css('height', '0');
        }
    }

}]