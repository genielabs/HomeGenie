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

# Software Walkthrough

## Initial setup and configuration

After [HomeGenie installation](install.html), you can access it's **administration** site by entering  into your web browser the address (IP or hostname) of the computer where HG was installed:

{: .center}
![Administration page url]({{site.baseurl}}/images/docs/admin_page_url.png)

The start page is the *Dashboard* containing some **Widgets** such as *Weather*, *Alarm System*, *Energy Monitor*, *Thermostat*, a couple of sensors and lights.

This preset configuration is also a playable demo, useful for starting to learn some *HomeGenie* basics before you proceed to integrate your smart devices into it.

{: .center}
![Dashboard]({{site.baseurl}}/images/docs/dashboard_page_01.png)

### Configuring Widgets

When you see a *gear button* in the upper right corner of a widget, it means that the widget have options that can be configured. Pressing the button will popup a configuration dialog.

{: .center}
![Weather configuration dialog]({{site.baseurl}}/images/docs/weather_options_01.png)

### Weather Forecast Widget

HomeGenie can integrate also external services and make the data available to the system so that this can be used for automating tasks.

For example having motorized window shades to open or close on behalf of the sun position and the weather conditions (cloudy or not) or turning off the garden irrigation system if it's raining outside.

The Weather widget is based on [Weather Underground](http://www.wunderground.com) service and requires that the user create an account in order to obtain a *service key* that is needed to make the widget
functional (as shown in the picture above).

You will learn later from this guide how to use widgets data in a Wizard Script or Automation Program to automate tasks.

### Alarm System

If your home automation setup includes devices such as motion, smoke, co2 detectors, door/window sensors, then you can use them for a basic security alarm system.

In the picture below, you can see options for the *Security Alarm Widget*. It can be configured to send e-mails when the alarm is triggered and/or to run an automation program when the system is armed/disarmed/triggered.

![Alarm System Options]({{site.baseurl}}/images/docs/alarm_system_01.png)

To include a sensor in the alarm system press the options button of the sensor widget and configure the *Security Alarm System* options as shown below:

<iframe id="player" width="385" height="230" src="http://www.youtube.com/embed/1Hesj-jEtFs?rel=0&wmode=Opaque&enablejsapi=1;showinfo=0;controls=0" frameborder="0" allowfullscreen></iframe>


### Thermostat

...

### Lights, Appliances and Sensors

...

{: .center}
``` Work In Progress... ```


In the meantime see the old [User's Guide](http://www.homegenie.it/docs/index.php) on HomeGenie presentation site.
