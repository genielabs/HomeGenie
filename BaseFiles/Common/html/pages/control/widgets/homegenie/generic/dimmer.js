$$.widget = {
  name: 'Generic Switch/Dimmer',
  version: '1.2',
  author: 'Generoso Martello',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/dimmer_off.png'
};

// widget class methods

$$.start = function() {
  // Settings button click
  $$.field('settings').on('click', function () {
    $$.ui.EditModule($$.module);
  });
  // On button click
  $$.field('on').on('click', function () {
    $$.module.command('Control.On');
  });
  // Off button click
  $$.field('off').on('click', function () {
    $$.module.command('Control.Off');
  });
  // Toggle button click
  $$.field('toggle').on('click', function () {
    $$.module.command('Control.Toggle');
  });          
  // Level slider release
  $$.field('level').on('slidestop', function () {
    $$.module.command('Control.Level', $(this).val());
  });          
}

$$.refresh = function () {
  // Hide dimmer level if device type is switch
  if ($$.module.DeviceType == 'Switch' || $$.module.DeviceType == 'Light') {
    $$.field('level-div').hide();
  } else {
    $$.field('level-div').show();
  }

  $$.field('name').html($$.module.Name);
  var description = ($$.module.Domain.substring($$.module.Domain.lastIndexOf('.') + 1)) + ' ' + $$.module.Address;
  $$.field('description').html(description);

  $$.refreshStatus();
  $$.refreshMeter();
}

$$.update = function(parameter, value) {
  switch(parameter) {
    case 'Status.Level':
      $$.refreshStatus();
      $$.signalActity('name');
      break;
    case 'Meter.Watts':
      $$.refreshMeter();
      $$.signalActity('status');
      break;
    case 'Status.Error':
      if (value != '' && $$.field('led').length)
        $$.field('led').attr('src', 'images/common/led_red.png');
      break;
    default:
      $$.signalActity();
  }
}

$$.stop = function() {
  // TODO: ...
}


// custom methods

$$.refreshStatus = function() {
  var displayTime = '';
  // Get module level prop for status text
  var level = $$.module.prop('Status.Level');
  if (level != null) {
    var updatetime = level.UpdateTime;
    if (typeof updatetime != 'undefined') {
      updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
      var d = new Date(updatetime);
      displayTime = $$.util.FormatDate(d) + ' ' + $$.util.FormatDateTime(d); //$$.util.GetElapsedTimeText(d);
    }
    // Set status led color
    level = level.Value.replace(',', '.') * 100;
    if (level > 0) {
      $$.field('statusLed').attr('src', 'images/common/led_green.png');
    }
    else {
      $$.field('statusLed').attr('src', 'images/common/led_black.png');
    }
  } else level = 0;

  HG.Ui.GetModuleIcon($$.module, function(i,e){
    $$.field(e).attr('src', i);
  }, 'icon');
  $$.field('level').val(level).slider('refresh');
  $$.field('updatetime').html(displayTime);
}

$$.refreshMeter = function() {
  // Get module watts property
  var watts = $$.module.prop('Meter.Watts');
  if (watts != null) {
    var w = parseFloat(watts.Value.replace(',', '.')).toFixed(1);
    if (w > 0) {
      watts = w + '<span style="opacity:0.65">W</span>';
    } else watts = '';
  } else watts = '';
  $$.field('status').html(watts);
}