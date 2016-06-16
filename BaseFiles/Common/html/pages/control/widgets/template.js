$$.widget = {
  name: 'New Widget',
  version: '1.0',
  author: 'HomeGenie User',
  release: '2016-05-06',
  icon: 'pages/control/widgets/homegenie/generic/images/robot.png'
};

/*
# Quick-Reference

 "$$" is the widget class instance object

 Widget class methods and properties:

 Get the jQuery element for a "data-ui" field
   $$.field('<widget_field_name>')

 Get the jQuery element in the main document
   $$.field('<document_tree_selector>', true)

 Call HG API Web Service 
   $$.apiCall('<api_method>', function(response){ ... })

 Get the bound module object
   $$.module

 Get a parameter of the bound module
   $$.module.prop('<param_name>')
   e.g.: $$.module.prop('Status.Level')

 Invoke a module command
   $$.module.command('<api_command>', '<command_options>', function(response) { ... })
   e.g.: $$.module.command('Control.Off')

 Shorthand for HG.Ui
   $$.ui

 Shorthand for HG.WebApp.Utility
   $$.util

 Shorthand for HG.WebApp.Locales
   $$.locales

 Blink a widget field and the status led image (if present)
   $$.signalActity('<widget_field_name>') 

 For a reference of HomeGenie Javascript API see:
   https://github.com/genielabs/HomeGenie/tree/master/BaseFiles/Common/html/js/api

*/

// Widget base class methods

// This method is called when the widget starts
$$.onStart = function() {
  // handle ui elements events
  $$.field('btn-on').bind('click', function(){
    btnOnClicked();
  });
  $$.field('btn-off').bind('click', function(){
    btnOffClicked();
  });
}

// This method is called when the widget UI is request to refresh its view
$$.onRefresh = function () {
  $$.field('lbl-name').html($$.module.Name + " (" + $$.module.DeviceType + ")");
  $$.field('lbl-description').html('Hello World');
  $$.field('lbl-info').html('default widget template');
}

// This method is called when the bound module raises a parameter event
// eg.: parameter = 'Status.Level', value = '1'
$$.onUpdate = function(parameter, value) {
}

// This method is called when the widget stops
$$.onStop = function() {
}


// user-defined methods implemented for this widget

btnOnClicked = function() {
  $$.field('lbl-status').html('ON was clicked!');
  $$.field('img-icon').attr('src', 'pages/control/widgets/homegenie/generic/images/light_on.png');
};

btnOffClicked = function() {
  $$.field('lbl-status').html('OFF was clicked!');
  $$.field('img-icon').attr('src', 'pages/control/widgets/homegenie/generic/images/light_off.png');
};