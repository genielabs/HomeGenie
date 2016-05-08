$$.widget = {
  name: 'Generic Color Light',
  version: '1.2',
  author: 'Generoso Martello',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/icons/colorbulbs.png'
};

var colorWheel = null;
var colorImage = null;

// widget class methods

$$.start = function() {
  // Settings button click
  $$.field('settings').on('click', function () {
    HG.WebApp.Control.EditModule($$.module);
  });

  // When options button is clicked control popup is shown
  $$.field('options').on('click', function () {
    $$.popup.popup('open');
  });
  // On button click
  $$.field('on').on('click', function () {
    $$.module.command('Control.On');
  });
  // Off button click
  $$.field('off').on('click', function () {
    $$.module.command('Control.Off');
  });
  // Toggle button action (open color wheel popup)
  $$.field('toggle').on('click', function () {
    $$.popup.popup('open');
  });          
  // Level slider release
  $$.field('level').on('slidestop', function () {
    $$.module.command('Control.Level', $(this).val());
  });          
  // Popup buttons click
  $$.popup.field('on').on('click', function () {
    $$.module.command('Control.On');
  });
  $$.popup.field('off').on('click', function () {
    $$.module.command('Control.Off');
  });
  // Setup color wheel
  colorImage = Raphael($$.field('colorball').get(0));
  colorWheel = Raphael.colorwheel($$.popup.field('colors'), 200, 80);
  colorWheel.ondrag(null, function (rgbcolor) {
    var color = Raphael.rgb2hsb(rgbcolor);
    var hue = color.h;
    var sat = color.s;
    var bri = color.b;
    var hsbcolor = hue + "," + sat + "," + bri;
    colorWheel.color(rgbcolor);
    color.b = 1;
    colorImage.clear();colorImage.ball(12, 12, 12, color);
    $$.module.command('Control.ColorHsb', hsbcolor);
  });
}

$$.refresh = function () {
  HG.Ui.GetModuleIcon($$.module, function(i,e){
    $$.field(e).attr('src', i);
  }, 'icon');
  $$.field('description').html(($$.module.Domain.substring($$.module.Domain.lastIndexOf('.') + 1)) + ' ' + $$.module.Address);
  $$.field('icon').attr('src', $$.widget.icon);

  // Control PopUp
  $$.popup.field('group').html(this.GroupName);
  $$.popup.field('name').html($$.module.Name);

  $$.refreshStatus();
  $$.refreshColor();
  $$.refreshMeter();
}

$$.update = function(parameter, value) {
  // TODO: ..
  switch(parameter) {
    case 'Status.Level':
      $$.refreshStatus();
      $$.refreshColor();
      $$.ui.blink('name');
      break;
    case 'Status.ColorHsb':
      $$.refreshStatus();
      $$.refreshColor();
      $$.ui.blink('name');
      break;
    case 'Meter.Watts':
      $$.refreshMeter();
      $$.ui.blink('status');
      break;
    case 'Status.Error':
      if (value != '' && $$.field('led').length)
        $$.field('led').attr('src', 'images/common/led_red.png');
    default:
      $$.ui.blink();
  }
}

$$.stop = function() {
  // TODO: ..
}


// custom methods

$$.refreshStatus = function() {
  // Get module level prop for status text
  var displayTime = '';
  var level = $$.module.prop('Status.Level');
  if (level != null) {
    var updatetime = level.UpdateTime;
    if (typeof updatetime != 'undefined') {
      updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
      var d = new Date(updatetime);
      displayTime = $$.util.FormatDate(d) + ' ' + $$.util.FormatDateTime(d); //$$.util.GetElapsedTimeText(d);
    }
    level = parseFloat(level.Value.replace(',', '.'))*100;
  } else level = '';
  $$.field('level').val(level).slider('refresh');
  $$.field('updatetime').html(displayTime);
}

$$.refreshColor = function() {
  var level = $$.module.prop('Status.Level');
  if (level != null) {
    level = parseFloat(level.Value.replace(',', '.'))*100;
  } else level = 0;
  // Set current light color
  var hsbcolor = $$.module.prop('Status.ColorHsb');
  if (hsbcolor != null && colorWheel != null) {
    var hexcolor = eval('Raphael.hsb('+hsbcolor.Value+')');
    var color = Raphael.rgb2hsb(hexcolor);
    colorWheel.color_hsb(color);
    if (level == 0) color.b = 0; else color.b = 1;
    colorImage.clear(); colorImage.ball(12, 12, 12, color);
  }
}

$$.refreshMeter = function() {
  // Get module watts prop
  var watts = $$.module.prop('Meter.Watts');
  if (watts != null) {
    var w = Math.round(watts.Value.replace(',', '.'));
    if (w > 0) {
      watts = w + '<span style="opacity:0.65">W</span>';
    } else watts = '';
  } else watts = '';
  $$.field('status').html('<span style="vertical-align:middle">' + watts + '</span>');
  $$.popup.field('status').html('<span style="vertical-align:middle">' + watts + '</span>');
}