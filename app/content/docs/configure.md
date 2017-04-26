## Configuring groups

Smart devices and services (also simply called *modules*) can be organized into groups.
From the **<i class="material-icons">settings</i>Configure** menu 
select the **<i class="material-icons">dashboard</i>Groups** option to
add and manage groups.
Click the **<i class="material-icons">add</i>Add Group** button located
in the *bottom bar* to add a new group.

To edit a group select it from the list. The group configuration page will be opened.
From this page we can add/sort *modules* in the group, change the wallpaper, rename or
delete it. 

<div class="media-container">
    <img self="size-medium" src="images/docs/configure_menu.png">
    <img self="size-medium" src="images/docs/groups_add_group.png">
    <img self="size-medium" src="images/docs/groups_add_module.png">
</div>


## Configuring widgets

We call *widget*, that part of the user interface through which a module is presented
and through which we can interact with it.

Depending on the type, a different widget will be used to display each *module*.
In every case we can configure a *module* by pressing the **<i class="material-icons">settings</i>**
button in the upper right corner of its widget. This will popup a dialog for configuring its options.

Let's now see how to configure each of them.


### Weather forecast

HomeGenie can integrate also external services and make the data available to the system
so that it can be displayed and/or used for automating tasks.

A good example is the Weather widget that is based on [Weather Underground](http://www.wunderground.com) service. 

To enable this widget it is required to enter a *service API key* that can be obtained by registering to the service as
described in its option popup (see picture below).

<div class="media-container">
    <img self="size-medium" alt="Weather configuration dialog" src="images/docs/weather_options_01.png">
</div>

After entering the location name and a valid API key, press the **<i class="material-icons">autorenew</i>Restart** button.
The widget will then retrieve updated forecast and other useful data.

This data can then be used to automate various tasks.
For example to open or close motorized window shades on behalf of the sun position
and the weather conditions (cloudy or not), or turning off the garden irrigation system
if it's raining outside.


### Alarm system

If your home automation setup includes devices such as motion/smoke/co2/etc.. detectors, 
door/window sensors, then you can use them to create a basic security alarm system.

<!--
In the picture below, you can see options for the *Security Alarm Widget*. It can be configured to send e-mails when the alarm is triggered and/or to run an automation program when the system is armed/disarmed/triggered.
-->

<!--div class="media-container">
    <img alt="Alarm System Options" src="images/docs/alarm_system_01.png">
</div-->

<div class="content-margin" align="center">
    <iframe self="size-medium" height="440" src="https://www.youtube.com/embed/jsL_fAJ5-5w?rel=0" frameborder="0" allowfullscreen></iframe>
</div>

The above video shows how to setup alarms and sensors for the *Security Alarm System*. Then, by pressing the *Motion Detected* button in the dashboard, it will simulate a motion event and so, when the system is armed, it will trigger the alarm.


### Lights, appliances and sensors

Also lights, appliances, sensors and other widgets have configurable options. Some options are related to the integration with other services/widgets (such as the alarm system), some other options are used to configure the behavior of the underlying device.

<div class="media-container">
    <img self="size-medium" alt="Widget Features/Options" src="images/docs/widget_options_01.gif">
</div>


#### Energy saving mode

If enabled, this option will limit the level of a dimmer to a given value (20% by default).


#### Level memory

If enabled, the device will restore last dimmer level when turned on. This is useful when the device itself does not implement this feature as it happens for some old X10 dimmer modules.


#### Smart lights

If enabled, the device will react to any of the following events:

- *Motion Detection*
  <i class="fa fa-long-arrow-right"></i>
  it will turn **on** if motion is detected from the configured sensor
- *Luminance*
  <i class="fa fa-long-arrow-right"></i>
  it will turn **on** if luminance from the configured sensor is below the given value
- *Timeout*
  <i class="fa fa-long-arrow-right"></i>
  it will turn **off** if idle for more than the configured value


### Thermostats

...

<div class="content-margin" align="center">
    `Work In Progress...`
</div>
