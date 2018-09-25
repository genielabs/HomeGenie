$$.widget = {
  name: 'Generic Camera Input',
  version: '1.2',
  author: 'Generoso Martello',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/camera.png'
};

var popupisopen = false;
var requestPopupOpen = false;
var imageurl = $$.widget.icon;

$$.onStart = function() {
  // Settings button click
  $$.field('settings').on('click', function() {
    $$.ui.EditModule($$.module);
  });

  // Initialize camera live popup
  $$.popup.on('popupbeforeposition', function() {
    popupisopen = true;
    requestPopupOpen = false;
    $$.popup.field('camerapicture').attr('src', imageurl + '?' + (new Date().getTime()));
  });
  $$.popup.on('popupafterclose', function() { popupisopen = false; $.mobile.loading('hide'); });
  $$.popup.field('camerapicture').attr('src', imageurl + '?' + (new Date().getTime())).load(function () {
    if (requestPopupOpen) {
      $$.popup.field('camerapicture').css({ width: 'auto', height: 'auto'});
      $$.popup.popup('open');
      if (requestResize) {
        requestResize = false;
        var w = $$.popup.field('camerapicture').width()+200;
        var h = $$.popup.field('camerapicture').height()+200;
        var availW = $(window).width()-160;
        var availH = availW * (h / w);
        if (w/h >= 1 && $(window).width() / $(window).height() >= 1) {
          availH = $(window).height()-160;
          availW = availH * (w / h);
          if ($(window).width() / $(window).height() >= 1)
            $$.popup.field('camerapicture').css({ width: availW+'px', height: availH/(availW/w)+'px'});
          else
            $$.popup.field('camerapicture').css({ width: availW*(availH/h)+'px', height: availH+'px'});
        } else {
          if ($(window).width() / $(window).height() >= 1)
            $$.popup.field('camerapicture').css({ width: availW+'px', height: availH/(availW/w)+'px'});
          else
            $$.popup.field('camerapicture').css({ width: availH/(h/w)+'px', height: availH+'px'});
        }
        setTimeout(function(){
          $$.popup.popup('reposition', { positionTo: 'window', animate: 'true' });
        }, 500);
      }
    } else if (popupisopen) {
      $.mobile.loading('hide');
      setTimeout(function () {
        $$.popup.field('camerapicture').attr('src', imageurl + '?' + (new Date().getTime()));
      }, 80);
    }
  }).error(function () {
    if (popupisopen) {
      $.mobile.loading('show', { text: 'Error connecting to camera', textVisible: true });
      setTimeout(function () {
        $$.popup.field('camerapicture').attr('src', imageurl + '?' + (new Date().getTime()));
      }, 2000);
    }
  });

  // When widget is clicked control popup is shown
  $$.field('camerapicturepreview').on('click', function() {
    $.mobile.loading('show', { text: 'Connecting to camera', textVisible: true });
    requestPopupOpen = true;
    requestResize = true;
    $$.popup.field('camerapicture').attr('src', imageurl + '?' + (new Date().getTime()));
  });
  $$.field('camerapicturepreview').load(function() {
    if ($$.container.is(':visible')) {
      setTimeout(function () {
        $$.field('camerapicturepreview').attr('src', imageurl + '?' + (new Date().getTime()));
      }, 500);
    }
  }).error(function () {
    if ($$.container.is(':visible')) {
      setTimeout(function () {
        $$.field('camerapicturepreview').attr('src', imageurl + '?' + (new Date().getTime()));
      }, 500);
    }
  });
  $$.field('sizetoggle').on('click', function(){
    if ($$._widget.data('enlarged') === true) {
      $$._widget.css('width', '');
      $$._widget.css('height', '');
      $$.field('camerapicturepreview').css('height', '172');
      $$._widget.data('enlarged', false);
    } else {
      $$._widget.css('width', '430');
      $$._widget.css('height', '290');
      $$.field('camerapicturepreview').css('height', '282');
      $$._widget.data('enlarged', true);
    }
    // force content relayout
    $$.container.parent().parent().isotope('layout')
  });

  // Set the camera image URL
  var cameraUrl = $$.module.prop('IpCamera.ImageURL');
  if (cameraUrl != null) imageurl = cameraUrl.Value;
  else imageurl = '/api/' + $$.module.Domain + '/' + $$.module.Address + '/Camera.GetPicture/';
  if (imageurl.indexOf('?') > 0) imageurl += '&';
}

$$.onRefresh = function() {
  $$.field('camerapicturepreview').attr('src', imageurl + '?');
  $$.field('name').html($$.module.Name);
  $$.popup.field('name').html($$.module.Name);
  $$.field('description').html('Camera ' + $$.module.Address);
}

$$.onUpdate = function(parameter, value) {
  // TODO: ..
  $$.signalActity();
}

$$.onStop = function() {
  // TODO: ..
}
