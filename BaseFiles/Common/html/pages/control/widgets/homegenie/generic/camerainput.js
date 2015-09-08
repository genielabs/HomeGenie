[{
    Name: "Generic Camera Input",
    Author: "Generoso Martello",
    Version: "2013-06-08",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/camera.png',
    StatusText: '',
    Description: '',
    Widget: null,

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = this.Widget = container.find('[data-ui-field=widget]');
        var controlpopup = widget.data('ControlPopUp');
        var popupisopen = false;
        this.Description = 'Camera ' + module.Address;
        //
        var imageurl = HG.WebApp.Utility.GetModulePropertyByName(module, 'Image.URL');
        if (imageurl != null) imageurl = imageurl.Value;
        else imageurl = '/api/' + module.Domain + '/' + module.Address + '/Camera.GetPicture/';
        //
        // create and store a local reference to control popup object
        //
        if (!controlpopup) {
            container.find('[data-ui-field=controlpopup]').trigger('create');
            controlpopup = container.find('[data-ui-field=controlpopup]').popup();
            widget.data('ControlPopUp', controlpopup);
            //
            controlpopup.on('popupbeforeposition', function () { popupisopen = true; });
            controlpopup.on('popupafterclose', function () { popupisopen = false; });
            //
            controlpopup.find('[data-ui-field=camerapicture]').load(function () {
                if (popupisopen) {
                    setTimeout(function () {
                        var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
                        popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?');
                    }, 100);
                }
            });
            //
            // initialization stuff here
            //
            var _this = this;
            // when widget is clicked control popup is shown
            widget.find('[data-ui-field=camerapicturepreview]').on('click', function () {
                if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
                    var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
                    popup.popup('open');
                    popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?');
                }
            });
            widget.find('[data-ui-field=camerapicturepreview]').load(function () {
                if (_this.Widget.is(':visible')) {
                    setTimeout(function () {
                        _this.Widget.find('[data-ui-field=camerapicturepreview]').attr('src', imageurl + '?');
                    }, 2000);
                }
            });
            //
            // ui events handlers
            //
            // settings button
            widget.find('[data-ui-field=settings]').on('click', function () {
                HG.WebApp.Control.EditModule(module);
            });
            //
        }
        //
        // read some context data
        //
        this.GroupName = container.attr('data-context-group');
        //
        // render widget
        //
        widget.find('[data-ui-field=name]').html(module.Name);
        widget.find('[data-ui-field=description]').html(this.Description);
        //widget.find('[data-ui-field=status]').html(this.StatusText);
        //widget.find('[data-ui-field=icon]').attr('src', this.IconImage);
        //
        this.Widget.find('[data-ui-field=camerapicturepreview]').attr('src', imageurl + '?');
    }

}]