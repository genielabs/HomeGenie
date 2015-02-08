LICENCE

Please read the LICENCE.TXT file.


DISCLAIMER
==========

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


HOMEPAGE, DOCUMENTATION AND FORUM
=================================

http://www.homegenie.it
http://www.homegenie.it/docs
http://www.homegenie.it/forum


AUDIENCE
========

This project is mainly addressed to developers and tech enthusiasts, with the aim of becoming more and more friendly for everyone's use.
If you like this project and want to contribute, you can do this in many ways: sending feedback; reporting bugs; fixing bugs; improving/adding features; beta testing; suggesting new features; donating.


MISSION
=======

- Developing an easy to use and generic purpose Home Automation Server that will have highly extensible/programmable features
- Giving a concrete common language "chance" to all "connected" devices and standards, humans included =)
- Building a "Do It Yourself" HG server box, based on Raspberry Pi, with integrated display and also supporting direct plugging of HW add-ins (1-wire sensors and other easy to plug devices)
- Making HomeGenie boxes "talk" each other and interoperate

Some of these goals have been already accomplished (see CURRENT FEATURES).

Other expected features:
- Support for other multimedia wireless standards (eg. Bluetooth, AirPlay)
- Generic I/O Device Wireless Router and Framework (see MIG)


CURRENT FEATURES
================

* Modern, web based, responsive UI
  use it on every device, from desktop PCs to smart phones and tablets

* Integrated drivers for X10 and Z-Wave devices
  ready to use solution for your home automation

* Real and virtual energy metering with statistics
  energy consumption awareness for optimizing costs and usage

* UPnP / DLNA control point
  control media receiver, renderer and light/switch device types

* Wizard Scripting
  no need to be a programmer, create scenarios with your fingers using the "live macro recording" feature

* Automation Program Plugins (APPs)
  easily add/share new automation programs and features

* Localization
  English, Italian, Spanish, French, Netherlands. Other languages can by added by creating a simple JSON file.

* Password protected access

* Configuration backup/restore and factory reset

* Embeddable
  runs on low-energy and low-cost embedded systems such as Raspberry Pi

* Customizable widgets and features
  create new features and widgets that interact with your devices and internet services

* Programmable Automation Engine
  web based Program Editor and compiler with multi-language support: C#, Javascript, Phyton and Ruby, Arduino Sketch 

* Dynamic Web Service API and Helper Classes
  make your devices and services talk each other using a common language over the net

* Direct access to Raspberry Pi hardware
  GPIO, SPI, I2C programming using an event driven model

* Virtual modules

* Speech recognition / Voice Control (using Web Speech API)

* Speech synthesys (cloud based)

* Programmable InfraRed transceiver
  use a common IR remote to control your automation system

* Video4Linux camera video driver
  use a webcam as a remote monitoring device

* Mobile app clients (Android, Windows Phone)

* Open Source



BUILT-IN INTERFACES AND PROTOCOL DRIVERS
========================================

- X10      Marmitek CM15Pro USB interface
- X10	   Marmitek CM11/CM12 Serial interface
- X10	   W800RF32AE X10 RF receiver
- Z-Wave   Aeon Labs Z-Stick 2 and other Zensys API compatible controllers
- Z-Wave   RaZberry, Z-Wave daughter board for Raspberry Pi
- Insteon  Insteon PLM 2413S/2413U
- LIRC	   Any LIRC compatible IR transceiver
- GPIO     Raspberry Pi GPIO 1Wire/SPI/I2C
- Arduino  Integrated Arduino(TM) development
- V4L      Video4Linux camera video driver
- Serial   Generic serial port interface
- TCP      Generic TCP interface
- UDP      Generic UDP interface


BUILT-IN X10 FEATURES
=====================

- PLC: All Light On, All Units Off, On, Off, Dim, Bright
- RF: raw data receive and decoding


BUILT-IN Z-WAVE FEATURES
========================

- Nodes Discovery
- Node Inclusion/Exclusion
- Manufacturer Specific Get and Node Information (NIF)
- Basic Get/Set/Report
- Wake Up Get/Set
- Association Get/Set/Remove
- Association Group Get/Set/Remove
- Configuration Variable Get/Set
- MultiInstance/Channel Get/Set/Report (SwitchBinary, SwitchMultiLevel, SensorBinary, SensorMultiLevel)
- Metering Report
- Thermostat Mode/OperatingState/SetPoint/FanMode/FanState/Heating/SetBack

BUILT-IN Z-WAVE MODULE TYPES
============================

- Switch
- Dimmer Light (MultiLevel Switch)
- Siren
- MultiChannel Switch / Dimmer
- Motion Sensor
- Temperature Sensor
- Luminance Sensor
- Door/Window Sensor
- Flood Sensor
- Smoke Sensor
- Heat Sensor
- CarbonDioxide Sensor
- CarbonMonoxide Sensor
- Thermostat


AUTOMATION PROGRAM PLUGINS (APPs)
=================================

Lights
- Level Memory
- Smart Ligths

Energy Management
- Energy Monitor
- Energy Saving Mode
- Turn Off Delay

Scenes
- Group Lights ON/OFF
- Sunrise Color Scenario

Weather and Enviroment
- Earth Tools
- Generic Thermostat
- jkUtils OpenWeatherData
- jkUtils Solar Altitude
- Weather Undergroud

Messaging and Social
- Alcatel One Touch Y800Z SMS Notify
- E-Mail Account
- Pushing Box
- Windows Phone Push Notification Service

Devices and Things
- Favourites Links
- Generic IP Camera
- One-Wire Devices
- Philips Hue Bridge
- Serial Port I/O example

X10
- SC9000 RF Virtual Modules
- Set to 100% when switched on
- X10 RF Virtual Modules Mapper

Z-Wave
- Fibaro RGBW
- Level Poll
- Multi Instance/Channel Virtual Modules
- Query on Wake Up

Security
- Ping Me at Home
- Presence Simulator
- Security Alarm System

Interconnections
- IR/RF remote control events forwarding
- Meter.Watts events forwarding
- Sensor.* events forwarding
- Status.Level events forwarding
- MQTT Network

Scheduling
- Timetable (for scheduling lights, shutters, thermostats, appliances)
- Scheduled ON/OFF

Raspberry Pi
- DHT-11 Temperature/Humidity sensor
- DHT-22 Temperature/Humidity sensor
- GPIO Modules
- Grove - Chainable RGB Led
- Grove - Led Bar
- HCSR04 - Ultrasonic Ranging Module
- MCP23017 GPIO Modules
- MCP3008 - Analog Input Modules
- Olimex - Nokia 3310 LCD display
- SmartIC - MCP23017 dual I/O expander

CubieTruck
- GPIO Modules


More program plugins available from contributors at Users' Forum board:
http://www.homegenie.it/forum/index.php?board=3.0




