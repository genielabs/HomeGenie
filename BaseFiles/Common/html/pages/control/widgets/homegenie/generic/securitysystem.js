$$.widget = {
  name: 'Security Alarm System',
  version: '1.2',
  author: 'Generoso Martello',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/securitysystem.png'
};

// widget class methods

$$.onStart = function() {
  // Settings button click
  $$.field('settings').on('click', function () {
    $$.ui.ConfigureProgram($$.module);
  });
  // Arm-Home button click
  $$.field('btn_armhome').on('click', function () {
    $$.module.command('Control.ArmHome');
  });
  // Arm-Away button click
  $$.field('btn_armaway').on('click', function () {
    $$.module.command('Control.ArmAway');
  });
  // Disarm button click
  $$.field('btn_disarm').on('click', function () {
    $$.module.command('Control.Disarm');
  });
  //this.GroupName = container.attr('data-context-group');
}

$$.onRefresh = function () {
  if (!$$.module) return;
  $$.ui.GetModuleIcon($$.module, function(imgPath){
    $$.field('icon').attr('src', imgPath);
    $$.widget.icon = imgPath;
  });

  var title = $$.locales.GetProgramLocaleString($$.module.Address, 'Title', 'Alarm System');
  $$.field('name').html(title);

  $$.refreshArmedStatus();
  $$.refreshCurrentMode();
  $$.loadLogData();
}

$$.onUpdate = function(parameter, value) {
  switch(parameter) {
    case 'Status.Level':
      $$.refreshArmedStatus();
      $$.refreshCurrentMode();
      break;
    case 'HomeGenie.SecurityArmed':
    case 'HomeGenie.SecurityTriggered':
      $$.refreshCurrentMode();
      break;
    default:
      $$.loadLogData();
      //$$.signalActity();
  }
}

$$.onStop = function() {
  // TODO: ...
}


// private/custom methods

$$.refreshTimeout = null;

$$.refreshArmedStatus = function() {
  var armedstatus = $$.module.prop('Status.Level'); //$$.module.prop('HomeGenie.SecurityArmed');
  if (armedstatus != null && armedstatus.Value == '1') {
    $$.field('arm_group').hide();
    $$.field('disarm_group').show();
  } else {
    $$.field('arm_group').show();
    $$.field('disarm_group').hide();
  }
}

$$.refreshCurrentMode = function() {
  var blink = false;
  var statusDescription = '...';
  var armedlevel = $$.module.prop('Status.Level');
  var armedstatus = $$.module.prop('HomeGenie.SecurityArmed');
  var statuscolor = 'rgba(100, 255, 100, 0.35)';
  if (armedlevel != null && armedlevel.Value == '1') {
    if (armedstatus != null && armedstatus.Value != 'Disarmed') {
      statusDescription = 'Armed '+armedstatus.Value;
      if (armedstatus.Value == 'Away')
        statuscolor = 'rgba(255, 50, 50, 0.45)';
      else
        statuscolor = 'rgba(50, 50, 255, 0.45)';
    } else {
      statusDescription = 'Arming...';
      statuscolor = 'rgba(255, 255, 50, 0.45)';
      blink = true;
    }
  } else {
    statusDescription = 'Disarmed';
    statuscolor = 'rgba(50, 255, 50, 0.45)';
  }
  $$.field('status-container').css('background', statuscolor);
  var alarmstatus = $$.module.prop('HomeGenie.SecurityTriggered');
  if (alarmstatus != null && alarmstatus.Value == '1') {
    statusDescription = '! ALARM !';
    blink = true;
  }
  $$.field('description').html(statusDescription);
  if (blink)
    $$.field('status-container').addClass('blinking_alarm');
  else
    $$.field('status-container').removeClass('blinking_alarm');
}

$$.loadLogData = function() {
  if ($$.refreshTimeout != null)
    clearTimeout($$.refreshTimeout);
  $$.refreshTimeout = setTimeout(function(){
    $$.module.command('Events.List', '', function(logData){
      $$.refreshLog(logData);
    });
  }, 200);
}

$$.refreshLog = function(logData) {
  // check wether the widget is disposed
  if (typeof $$._widget == 'undefined') return;
  $$.field('activity-log').empty();
  var log = '';
  if (logData.length > 0) {
    for(var i = logData.length-1; i >= 0; i--) {
      var securityModule = $$.util.GetModuleByDomainAddress(logData[i].Domain,logData[i].Address);
      var name = $$.ui.GetModuleDisplayName(securityModule);
      var param = logData[i].Parameter;
      var val = logData[i].Value;
      var logDetail = $('<div class="ui-block-a" style="border-top:1px solid rgba(200,200,200,0.5)"><img data-ui-field="img'+i+'" align="left" width="24" height="24" style="margin-top:6px;margin-right:4px" />'+name+'<br/><strong>'+val+'</strong> '+param+'</div>');
      var date = moment(logData[i].Timestamp).locale($$.locales.GetUserLanguage()).fromNow();
      var logTime = $('<div class="ui-block-b" align="right" style="border-top:1px solid rgba(200,200,200,0.5)"><br/>'+date+'</div>');
      $$.field('activity-log').append(logDetail);
      $$.field('activity-log').append(logTime);
      $$.ui.GetModuleIcon(securityModule, function(imgPath, elid){
        $$.field('activity-log').find('[data-ui-field="'+elid+'"]').attr('src',imgPath);
      }, 'img'+i);
    }
  } else {
    log = '<div align="center" style="line-height: 148px">'+$$.locales.GetWidgetLocaleString($$._widget, 'securityalarm_noactivity', 'No recent activity')+'</div>';
    $$.field('activity-log').append(log);
  }
  // auto refresh every 60 seconds
  if ($.mobile.activePage.attr('id') == 'page_control') {
    if ($$.refreshTimeout != null)
      clearTimeout($$.refreshTimeout);
    $$.refreshTimeout = setTimeout($$.loadLogData, 60000);
  }
}
