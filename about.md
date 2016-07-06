---
title: HomeGenie Dev Docs - About
layout: main
published: true
---

# Developers documentation

## What is this all about?

We can see thousands of new connected devices appearing day by day, creating much enthropy and own protocols.
Having a well designed software, ready to handle and manage all of this, is such a big challenge.
This is the scenario where HomeGenie is growing up,
and its mission is to try to find a reasonable way of converging all these new on going technologies into one single
piece of software.

{: .center}
![Dashboard]({{site.baseurl}}/images/docs/dashboard_page_01.png)

## Integrated Editor for Automation Programs and UI Widgets

In order to achieve this mission, the key factor for a such kind of software is to offer an integrated enviroment and set of tools for adding new features and supporting new hardware or services with a little effort, and also let the end user do this on his own, so to have a easy way to integrate custom solutions as needed.<br/>
For this purpose, HomeGenie integrates [Automation Program](programs.html) and [Widget](widgets.html) editors, so that if you want to improve a widget or integrate a new device/service, you will be able to do it without leaving HomeGenie UI.

## Multi-protocol I/O Gateway

Formerly known as [MIG](https://github.com/genielabs/mig-service-dotnet), it is the base library top of which HG is built on.
Whenever the integrated tools are not providing a good way of interfacing to something, [MIG](https://github.com/genielabs/mig-service-dotnet)
can offer a more flexible solution throught its [Interface Plugins](https://github.com/genielabs/mig-service-dotnet).<br/>
However, developing an [Interface Plugins](https://github.com/genielabs/mig-service-dotnet), will require to go outside
HomeGenie UI and use an external IDE such as *Xamarin Studio* or *Visual Studio* so to take advantage of the full power
of **.NET** framework and **C#** language.


## Software Diagram

<br />
<img src="https://raw.githubusercontent.com/genielabs/HomeGenie/master/HomeGenie_Diagram.png" />
