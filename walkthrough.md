---
title: HomeGenie Walkthrough
layout: main
published: true
---
<script type="text/javascript" src="http://www.youtube.com/player_api"></script>
<script type="text/javascript">
function onYouTubeIframeAPIReady() {
     var videos = document.getElementsByTagName('iframe'), // the iframes elements
         players = [], // an array where we stock each videos youtube instances class
         playingID = null; // stock the current playing video
     for (var i = 0; i < videos.length; i++) // for each iframes
     {
         var currentIframeID = videos[i].id; // we get the iframe ID
         players[currentIframeID] = new YT.Player(currentIframeID); // we stock in the array the instance
         // note, the key of each array element will be the iframe ID
         
         videos[i].onmouseover = function(e) { // assigning a callback for this event
             var currentHoveredElement = e.target;
             if (playingID) // if a video is currently played
             {
                 players[playingID].pauseVideo();
             }
             players[currentHoveredElement.id].playVideo();
             playingID = currentHoveredElement.id;
         };
     }
    
 }
</script>

# Walkthrough

## Initial setup and configuration

After [HomeGenie installation](install.html), you can access it's **administration** site by entering  into your web browser the address (IP or hostname) of the computer where HG was installed:

{: .center}
![Administration page url]({{site.baseurl}}/images/docs/admin_page_url.png)

The start page is the *Dashboard* containing some **Widgets** such as *Weather*, *Alarm System*, *Energy Monitor*, *Thermostat*, a couple of sensors and lights.

This preset configuration is also a playable demo, useful for starting to learn some *HomeGenie* basics before you proceed to integrate your smart devices into it.

{: .center}
![Dashboard]({{site.baseurl}}/images/docs/dashboard_page_01.png)

### Configuring Widgets

Pressing the *gear button* in the upper right corner of a widget, it will popup a dialog for customizing the various widget options.
Let's now see how to configure each of them.

{: .center}
![Weather configuration dialog]({{site.baseurl}}/images/docs/weather_options_01.png)

### Weather Forecast Widget

HomeGenie can integrate also external services and make the data available to the system so that this can be used for automating tasks.

The Weather widget itself is based on [Weather Underground](http://www.wunderground.com) service. 
It requires to register as a user in order to obtain a *service key* that is needed to make the widget
functional (see options shown in the picture above).
Once configured with a correct API key and restarted, the weather widget will retrieve updated forecast and other useful data for your location.

This data can then be used to automate various taks. For example having motorized window shades to open or close on behalf of the sun position and the weather conditions (cloudy or not) or turning off the garden irrigation system if it's raining outside.

You will learn later from this guide how to use widgets' data in a Wizard Script or Automation Program to automate tasks.

### Alarm System

If your home automation setup includes devices such as motion/smoke/co2/etc.. detectors, door/window sensors, then you can use these to create a basic security alarm system.

In the picture below, you can see options for the *Security Alarm Widget*. It can be configured to send e-mails when the alarm is triggered and/or to run an automation program when the system is armed/disarmed/triggered.

![Alarm System Options]({{site.baseurl}}/images/docs/alarm_system_01.png)

The following animation shows how to setup alarms and sensors for the *Security Alarm System*. Then, by pressing the *Motion Detected* button in the dashboard, it will simulate a motion event and so, when the system is armed, it will trigger the alarm.

{: .center}
<iframe id="player" width="680" height="400" src="http://www.youtube.com/embed/1Hesj-jEtFs?rel=0&wmode=Opaque&enablejsapi=1;showinfo=0;controls=0" frameborder="0" allowfullscreen></iframe>


### Lights, Appliances and Sensors

Also lights, appliances, sensors and other widgets have configurable options. Some options are related to the integration with other services/widgets (such as the alarm system), some other options are used to configure the behavior of the underlying device.

#### Energy Saving Mode

If enabled, this option will limit the level of a dimmer to a given value (20% by default).

#### Level Memory

If enabled, the device will restore last dimmer level when turned on. This is useful when the device itself does not implement this feature as it happens for some old X10 dimmer modules.

#### Smart Lights

If enabled, the device will react to any of the following events:

- *Motion Detection*
  <i class="fa fa-icon-arrow-right"></i>
  it will turn on
- *Luminance*
  <i class="fa fa-icon-arrow-right"></i>
  it will turn off if below the configured value
- *Timeout*
  <i class="fa fa-icon-arrow-right"></i>
  it will turn off if idle for more than the configured value

#### IR Remote Control



### Thermostat

...

{: .center}
``` Work In Progress... ```


In the meantime see the old [User's Guide](http://www.homegenie.it/docs/index.php) on HomeGenie presentation site.
