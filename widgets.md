---
title: Widgets
category: ape
layout: post
---
# Widgets

## Introduction

Widgets are reusable and composite user interface components used to display data and information of a [module](programs.html#modules)
and that may also contain UI controls (such as buttons, sliders, ...) for interacting with them. 
Widgets use [MVC](https://it.wikipedia.org/wiki/Model-View-Controller) design pattern, where the *View* is the HTML text
used for displaying the widget in the *UI*, the *Model* is small javascript code that access data of the bound module and
renders them to the view, and the *Controller* is the automation program that [receives commands](programs.html#commands)
from the View (upon user interaction) or other agents and implement the business logic of the module.

## Widget Editor

So, while the *Program Editor* can be used to implement the *Controller* part in the [MVC](https://it.wikipedia.org/wiki/Model-View-Controller),
the *Widget Editor* is used to implement the *View* and the *Model* part.
Widget Editor can be accessed from the *Automation* section of the **Configure** menu. New widget can be created selecting
the *Add widget* option from the *Action* menu located in the down-right corner.
So a widget is formed by two parts: the first, which represents the *View*, is HTML text; the second, which represents
the *Model*, is Javascript code.

The *Widget Editor* has a preview panel, just below the HTML editor, that will show a preview of the currently
inserted HTML text. In order for it to work, a **bound module** has to be selected from the *"bind to module"* selector.
The preview can then be updated by hitting ```CTRL+S``` keys or by clicking the *Preview* button.

## The View - HTML

When designing a widget's *View* a couple of guide-lines have to be considered:

- do not use the ```id``` attribute for referencing HTML elements; use a ```data-ui-field``` attribute instead
- prefer using **CSS** classes provided with *HG*, which are [jQuery Mobile CSS classes](https://api.jquerymobile.com/classes/) and the ones defined by
[*HG* CSS](https://github.com/genielabs/HomeGenie/blob/master/BaseFiles/Common/html/css/my.css#L206)
- since *HG UI* is based on [jQuery Mobile](http://jquerymobile.com/), prefer using this framework instead of plain HTML;
other frameworks/plugins can also be used next to [jQuery Mobile](http://jquerymobile.com/), these are listed later on this chapter

### Example - Basic Widget container
```html
<!-- main widget container -->
<div data-ui-field="widget" 
     class="ui-overlay-shadow ui-corner-all ui-body-inherit hg-widget-a">
     <!-- widget content begin -->
    <h1>Simple widget with a button</h1>
    <input data-ui-field="test-btn" type="button" class="ui-btn" />
    <br/>
    Module Name: <span data-ui-field="name">...</span>
     <!-- widget content end -->
</div>
```

## The Model - Javascript

The javascript code takes care of updating the data displayed in the widget and also of sending proper commands when
the user click buttons and other controls (if any).
This is implemented as a json object that is formatted as shown in the example below.

### Example - Minimal javascript code for a widget

```javascript
[{
  // use this field to assign a default image to the widget
  IconImage: 'pages/control/widgets/homegenie/generic/images/icons/robot.png',

  // this field is used for initializing the widget once loaded
  Initialized: false,

  // this method is called each time the module bound to this widget is updated
  // put here the code for displaying module's data
  RenderView: function (cuid, module) {
    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    var button = widget.find('[data-ui-field=test-btn]');
    var name = widget.find('[data-ui-field=name]');
    if (!this.Initialized) {
        this.Initialized = true;
        // register widget's event handlers
        button.on('click', ButtonClicked);
    }
    name.html(module.Name);
  },

  ButtonClicked: function() {
    // handler for the button click event here
    // this will make an API request in most cases
    $.get('/api/'+module.Domain+'/'+module.Address+'/Control.On', function(){
        // request completed.
    });
  }
}]
```

As shown in the *ButtonClicked* handler, in most cases, when the user click a control, an API request is made. The
end-point of the request will be usually an automation program that is [listening](programs.html#commands) to API calls
for that module domain.

## Frameworks and Plugins

The following is a list of framework/plugins that can be used in a widget.

### Base Frameworks
- [jQuery](https://jquery.com/)
- [jQuery Mobile](http://jquerymobile.com/)

### UI Controls
- [ColorWheel](http://jweir.github.io/colorwheel/)
- [jQuery Knob](http://anthonyterrien.com/knob/)
- [jQM Datebox](http://dev.jtsage.com/jQM-DateBox/)

### Notification / Tooltip
- [qTip](http://qtip2.com/)
- [jQuery UI Notify Widget](http://www.erichynds.com/examples/jquery-notify/)

### Graphics / Custom controls
- [Flot](http://www.flotcharts.org/)
- [RaphaelJs](http://raphaeljs.com/)

### Utility
- [Moment.js](http://momentjs.com/)
- [jStorage](http://www.jstorage.info/)
