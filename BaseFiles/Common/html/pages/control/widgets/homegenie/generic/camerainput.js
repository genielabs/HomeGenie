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
    var requestPopupOpen = false;
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
      var _this = this;
      //
      controlpopup.on('popupbeforeposition', function () { 
        popupisopen = true; 
        requestPopupOpen = false;
        var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
        popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?' + (new Date().getTime()));
      });
      controlpopup.on('popupafterclose', function () { popupisopen = false; $.mobile.loading('hide'); });
      controlpopup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?' + (new Date().getTime())).load(function () {
        var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
        if (requestPopupOpen) {
          popup.popup('open');
        } else if (popupisopen) {
          $.mobile.loading('hide');
          setTimeout(function () {
            popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?' + (new Date().getTime()));
          }, 100);
        }
      }).error(function () {
        var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
        if (popupisopen) {
          $.mobile.loading('show', { text: 'Error connecting to camera', textVisible: true });
          setTimeout(function () {
            popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?' + (new Date().getTime()));
          }, 2000);
        }
      });
      //
      // initialization stuff here
      //
      // when widget is clicked control popup is shown
      widget.find('[data-ui-field=camerapicturepreview]').on('click', function () {
        if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp')) {
          var popup = $(cuid).find('[data-ui-field=widget]').data('ControlPopUp');
          $.mobile.loading('show', { text: 'Connecting to camera', textVisible: true });
          requestPopupOpen = true;
          popup.find('[data-ui-field=camerapicture]').attr('src', imageurl + '?' + (new Date().getTime()));
        }
      });
      widget.find('[data-ui-field=camerapicturepreview]').load(function () {
        if (_this.Widget.is(':visible')) {
          setTimeout(function () {
            _this.Widget.find('[data-ui-field=camerapicturepreview]').attr('src', imageurl + '?' + (new Date().getTime()));
          }, 2000);
        }
      }).error(function () {
        if (_this.Widget.is(':visible')) {
          setTimeout(function () {
            _this.Widget.find('[data-ui-field=camerapicturepreview]').attr('src', imageurl + '?' + (new Date().getTime()));
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
    //
    this.Widget.find('[data-ui-field=camerapicturepreview]').attr('src', imageurl + '?');
  }

}]