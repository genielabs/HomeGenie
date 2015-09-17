---
title: Editor
category: ape
layout: post
---
# Widgets

## Introduction

Widgets are reusable and composite user interface components used to display data and information of a [module](programs.html#modules)
and that may also contain UI controls (such as buttons, sliders, ...) for interacting with them. 
Widgets use [MVC](https://it.wikipedia.org/wiki/Model-View-Controller) design pattern, where the *View* is the HTML code
used for displaying the widget in the *UI*, the *Model* is small javascript code that access data of the bound module and
renders them to the view, and the *Controller* is the automation program that receives commands from the View (upon user
interaction) or other agents and implement the business logic of the module.

## Widget Editor

So, while the [Program Editor](programs.html#commands) can be used to implement the *Controller* part in the [MVC](https://it.wikipedia.org/wiki/Model-View-Controller),
the Widget Editor is used to implement the *View* and the *Model* part.
Widget Editor can be accessed from the *Automation* section of the **Configure** menu. New widget can be created selecting
the *Add widget* option from the *Action* menu located in the down-right corner.
So as described above a widget is formed by two parts of code. The first, which represents the *View*, is HTML code.
The second, which represents the *Model*, is Javascript code.

## The View - HTML code

The widget's *View* is wrapped into an HTML container element (commonly a **div**) that must define the property
```data-ui-field="widget"```.

### Example - Basic Widget container
```html
<!-- main widget container -->
<div data-ui-field="widget" 
     class="ui-overlay-shadow ui-corner-all ui-body-inherit hg-widget-a">
     <!-- widget content begin -->
    <h1>Simple blank widget</h1>
    <p>Put the HTML widget body here</p>
     <!-- widget content end -->
</div>
```

**NOTE:** since *HG* UI is based on [jQuery Mobile](http://jquerymobile.com/), the *HTML* code of a widget can refer to
[jQuery Mobile CSS classes](https://api.jquerymobile.com/classes/) and the ones defined by
[*HG* CSS](https://github.com/genielabs/HomeGenie/blob/master/BaseFiles/Common/html/css/my.css#L206).
When designing a widget is preferable to use these CSS classes, in order to mantain a coherent style in the *UI*.

```TODO: to be continued... ```

## The Model - Javascript code

```TODO: to be continued... ```

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
