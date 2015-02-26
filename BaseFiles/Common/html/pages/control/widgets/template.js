[{
  Name: 'New Widget',
  Author: 'Foo Bar',
  Version: '1.0',

  IconImage: 'pages/control/widgets/homegenie/generic/images/icons/robot.png',
  Initialized: false,

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    var _this = this;
    if (!this.Initialized)
    {
      this.Initialized = true;
      // store a reference to ui fields
      this.nameText = widget.find('[data-ui-field=name]');
      this.descriptionText = widget.find('[data-ui-field=description]');
      this.statusText = widget.find('[data-ui-field=status]');
      this.iconImage = widget.find('[data-ui-field=icon]');
      this.updateTime = widget.find('[data-ui-field=updatetime]');
      // handle ui elements events
      widget.find('[data-ui-field=on]').bind('click', function(){
        _this.OnClicked();
      });
      widget.find('[data-ui-field=off]').bind('click', function(){
        _this.OffClicked();
      });
    }
    // render widget
    this.nameText.html(module.Name + " (" + module.DeviceType + ")");
    this.descriptionText.html('Hello World');
    this.updateTime.html('default widget template');
  },

  OnClicked: function() {
    this.statusText.html('ON was clicked!');
    this.iconImage.attr('src', 'pages/control/widgets/homegenie/generic/images/light_on.png');
  },

  OffClicked: function() {
    this.statusText.html('OFF was clicked!');
    this.iconImage.attr('src', 'pages/control/widgets/homegenie/generic/images/light_off.png');
  }
}]