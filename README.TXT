LICENCE

Please read the LICENCE.TXT file.


DISCLAIMER

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


HOMEPAGE AND DOCUMENTATION

http://www.homegenie.it


AUDIENCE

This project is mainly addressed to developers and tech enthusiasts, with the aim of becoming more and more friendly for everyone's use.
If you like this project and want to contribute, you can do this in many ways: sending feedback; reporting bugs; fixing bugs; improving/adding features; beta testing; suggesting new features; donating.


MISSION

* Developing an easy to use and generic purpose Home Automation Server that will have highly extensible/programmable features
* Giving a concrete common language "chance" to all "connected" devices and standards, humans included =)
* Building a "Do It Yourself" HG server box, based on Raspberry Pi, with integrated LCD, touch UI, user-selectable Display Widget and also supporting pluggable HW add-ins (1-wire sensors and other easy to plug devices) via a Common eXpansion Port
* Making HG boxes "talk" each other and interoperate

Some of these goals have been already accomplished (see CURRENT FEATURES).

Other expected features:
- Support for other multimedia wireless standards (eg. Bluetooth, DLNA, AirPlay)
- Generic I/O Device Wireless Router and Framework (see MIG)


CURRENT FEATURES

* Modern, web based, responsive UI
  use it on every device, from desktop PCs to smart phones and tablets

* Integrated drivers for X10 and Z-Wave devices
  ready to use solution for your home automation

* Real and virtual energy metering with statistics
  energy consumption awareness for optimizing costs and usage

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
  web based Program Editor and compiler with multi-language support: C#, Javascript, Phyton and Ruby

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

* Open Source (last but not least)


BUILT-IN INTERFACES AND PROTOCOLS DRIVERS

- X10      Marmitek CM15Pro USB interface
- X10	   Marmitek CM11/CM12 Serial interface
- X10	   W800RF32AE X10 RF receiver
- ZWave    Aeon Labs Z-Stick 2 and other Zensys API compatible controllers
- ZWave    RaZberry, Z-Wave daughter board for Raspberry Pi
- LIRC	   Any LIRC compatible IR receiver and remote 
- GPIO     Raspberry Pi GPIO 1Wire/SPI/I2C
- V4L      Video4Linux camera video driver

BUILT-IN X10 FEATURES

- PLC: All Light On, All Units Off, On, Off, Dim, Bright
- RF: raw data receive

BUILT-IN ZWAVE FEATURES

- Nodes Discovery
- Node Inclusion/Exclusion
- Manufacturer Specific Get and Node Information (NIF)
- Basic Get/Set
- Wake Up Get/Set
- Association Get/Set/Remove
- Association Group Get/Set/Remove
- Configuration Variable Get/Set
- MultiInstance/Channel Get/Set (SwitchBinary, SwitchMultiLevel, SensorBinary, SensorMultiLevel)
- Metering Report

BUILT-IN Z-WAVE MODULE TYPES

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


AUTOMATION PROGRAM PLUGINS (APPs):

* Dimmers Level Memory
* Z-Wave Light, Dimmer, Switch level poll
* Z-Wave Sensor values poll
* Generic IP Camera
* Security Alarm System
* X10 RF Virtual Modules (SC9000 Security Sensors)
* Weather Underground Widget
* Sunrise/Sunset lights control
* Philips Hue lights integration
* Interconnections (events routing between hg boxes)

RASPBERRY PI SPECIFIC APPs:

* HCSR04 Ultrasonic Distance Sensor
* MCP3008 Temperature and Luminance
* DHT-11 Temperature and Luminance
* HomeGenie Leds (activity and status leds)
* HomeGenie Buzzer
* MCP23017 Example
