LICENSE INFORMATION

HomeGenie Project and all of its components (eg. precompiled distributables,
home site, documentation), exception made only and solely for its
source code, are licensed under the terms and conditions of the 
"Creative Commons Attribution-NonCommercial 3.0 Generic License".
You should have received a copy of the CC-BY-NC License along with this program. 
If not, see <http://creativecommons.org/licenses/by-nc/3.0/>.

HomeGenie Source Code is licensed under the terms and conditions of the 
"GNU General Public License GPLv3".
You should have received a copy of the GNU General Public License along with 
this program.  If not, see <http://www.gnu.org/licenses/>.

Please referer to the github repository for source code and development contribution:
http://github.com/genielabs/HomeGenie



DISCLAIMER

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.



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
- IR learn and send
- Native mobile apps (iOS, Android, Windows Phone)
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
* Open Source
* Customizable widgets and features
  create new features and widgets that interact with your devices and internet services
* Programmable Automation Engine
  with web based Program Editor and compiler, the power of C# language for the Internet Of Things
* Dynamic Web Service API and Helper Classes
  make your devices and services talk each other using a common language over the net
* Direct access to Raspberry Pi hardware
  GPIO, SPI, I2C programming using an event driven model
* Virtual modules
* Speech recognition / Voice Control (using Web Speech API)
* Speech synthesys (cloud based)
* Programmable InfraRed receiver
  use a common IR remote to control your automation system
* Speech Synthesys
* Video4Linux camera video driver
  use a webcam as a remote monitoring device

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



HOMEPAGE AND DOCUMENTATION

http://homegenie.it



DEVELOPMENT WIKI

https://sourceforge.net/p/homegenie/wiki/Home/



RELEASE HISTORY

26/02/2014 1.00 beta rev 343
	- ZWaveLib: added fallback to Generic.Sensor handler when GenericType is not directly supported
	- Scheduler: replaced ',' (OR operator) with ':' symbol due to conflicts with regular cron expression
	- Updated "FibaroRGBW" plugin
	- Updated Weeco4m GPIO module

25/02/2014 1.00 beta rev 341
	- just some code cleanup and reformatting

24/02/2014 1.00 beta rev 340
	- Added Weeco4m GPIO module (http://www.weecoboard.eu) contributed by L.Neri
	- Added Scheduler.TimeEvent to Wizard Script conditions
	- Update Newtonsoft.Json.dll to latest version

23/02/2014 1.00 beta rev 339
	- Deprecated "Sunrise/Sunset Lights Control" in flavour of new "Scheduled ON/OFF" APP
	- Added SchedulerHelper class 
	- Added HG Scheduler supporting crontab expressions
	- Fixed cleanup of program features on APP re-compile
	- Disabled module Status.Level reset on restart

20/02/2014 1.00 beta rev 336
	- ZWaveLib: fixed bug in Scene Activation command class
	- Main source code refactoring

19/02/2014 1.00 beta rev 334
	- ZWaveLib: deprecated automatic manufacturer get; it was bugging serial i/o performance expecially with nodes supporting WakeUp
	- HGService: fixed password protection enabling/disabling after configuration restore
	- HGService: added logging to file, configurable from maintenance menu: last two days logs are stores in log folder
	- Automation: minor fixes/improvement to some APPs

18/02/2014 1.00 beta rev 333
	- HGService: added automatic cleanup of orphan/deleted Z-Wave modules
	- ZWaveLib: fixed major bug in node adding / removing
	- Automation: minor changes to Z-Wave MultiChannel and Smart Lights automation programs

16/02/2014 1.00 beta rev 332
	- APE: improved events capturing in Wizard Script; now supporting IR codes capture and more device types (fan, shutter, siren, ...)
	- MIG: LibLirc, symplified codes syntax for receiving and sending: <remote_name>/<remote_key>. Old codes syntax is deprecated.
	- MIG: added IR send capability to LircRemote handler
	- ZWaveLib: fixed Sensor Multilevel report that was interpreted as Sensor Binary

13/02/2014 1.00 beta rev 330
	- ZWaveLib: added support for Aeon Labs Aeotec Z-Wave Multi-Sensor
	- MIG: revised webservice code for better performances

09/02/2014 1.00 beta rev 329
	- APE: fixed AddControlWidget bug 
	- NetHelper: added support for ssl and custom port number for mail service (dummy certificate validation - be warned =))
	- XTenLib: added transceived house codes settings for CM15 interface
	- MIG: added preliminary code for supporting HTTPS in WebServiceGateway (work in progress)
	- MIG: replaced synchronous web requests processing in flavour of asynchronous ones; this should fix occasional webservice hang issue 
	- added fibaro rgbw app and widget (contributed by DaniMail)

02/02/2014 1.00 beta rev 327
	- fixed "_refresh_modules_misc" error
	- fixed UPnP GetPositionInfo json response formatting

24/01/2014 1.00 beta rev 326
	- added Fahrenheit temp value display to Weather Widget (contributed by lancebot http://sourceforge.net/p/homegenie/discussion/requestsandtodo/thread/c4752b10/#89b3)
	- removed AirPlay (replaced by new UPnP interface)
	- added Media Server and Media Receiver control widgets
	- added UPnP MIG Interface implementing UPnP ControlPoint for UPnP devices : Light, Media Server, Media Receiver
	- ZWaveLib: added Scene Activation cc support (routed as Sensor.Generic parameter)
	- ZWaveLib: fixed SwitchBinary and SwitchMultilevel reports, that are now being processed correctly
	- added TcpClient helper class

27/12/2013 1.00 beta rev 324
	- ZWaveLib: added checksum verification against received messages (fixes occasional bad readings of reported values from nodes)
	- fixed bug in MultiChannel switch widget that was not udating status for channels 1 and 2 
	- some minor changes/fixes in Level Memory, Energy Saving Mode and ZWave Wake Up Notify programs
	- some fixes in SC9000 security sensors program
	- ZWaveLib: fixed node inclusion/exclusion bug
	- Utility: fixed RunAsyncTask that was missing exceptions handling
	- ZWaveLib: fixed bad temperature values reported from HSM-100
	- ZWaveLib: fixed occasional "Control.Toggle" desync
	- ZWaveLib: added spin wait and ManualResetEvent for waiting callback ACK of last command before sending next one
	- added Program.Play(<wave_url>) method to ProgramHelper to play wave audio files

22/12/2013 1.00 beta rev 319
	- new X10 driver installer built with libusbK (Windows)

21/12/2013 1.00 beta rev 318
	- added "Smart Lights" app; improved DHT-11 app
	- fixed device type selection not working (Windows)
	- fixed CM15 driver bug (Windows)

20/12/2013 1.00 beta rev 316
	- set default condition type to 'OnTrue' when switching to csharp program type

20/12/2013 1.00 beta rev 315
	- added missing "Last-modified" header to WebServiceGateway
	- ZWaveLib: rewrote callback handling; added resend on CAN (0x18); major ZWavePort/Controller code rework

15/12/2013 1.00 beta rev 313
	- added automatic Manufacturer Specific and NIF requests when opening the node setup page
	- fixed ModuleParameter.DecimalValue bug in value conversion from string 

15/12/2013 1.00 beta rev 312
	- added "PowerMonitor.*" and "Statistics.*" parameters to statistics logger 

14/12/2013 1.00 beta rev 311
	- ZWaveLib: integrated code for HEM2 and generic POWER reports (https://sourceforge.net/p/homegenie/discussion/csharpscripting/thread/6d28b98d/#d298)

14/12/2013 1.00 beta rev 310
	- added z-wave configuration parameters descriptions from Pepper1 db

13/12/2013 1.00 beta rev 309
	- fixed timezone bug in statistics
	- added last 24hr graph to statistics; restyled stats
	- starting to integrate Pepper1 z-wave devices database into HG
	- deprecated manufacturer.xml

11/12/2013 1.00 beta rev 307
	- ZWaveLib: added MultiInstance Count Get
	- MIGInterface domain api commands can now be extended using hg automation programs (eg. adding new features to z-wave driver using an automation program)
	- added new automation program that associates a virtual module for each instance/channel of a multi instance/channel node (z-wave)
	- added ProgramHelper.AddVirtualModule / RemoveVirtualModule

09/12/2013 1.00 beta rev 306
	- added "Windows Phone Notification Push Service" app

07/12/2013 1.00 beta rev 305
	- added last 24 hours graph to statistics
	- fixed bug in Virtual Meter
	- added "Favourites links" widget and app

05/12/2013 1.00 beta rev 302
	- added UPnP service with presetation url to automatically discovery hg service
	- Windows HomeGenie Manager now using UPnP to discover HG service url

04/12/2013 1.00 beta rev 301
	- fixed bug in MIG.ZWave.SetPort that was not updating serial port name sometime
	- fixed switch widget on/off status text not updating in control popup
	- new statistics database and engine
	- added module filter in statistics page 

01/12/2013 1.00 beta rev 300
	- more reliable SerialPortLib (fixes Z-Wave ZWaveLib communication errors on smaller hosts such as RaspberryPi)
	- user defined programs have now Address >= ProgramEngine.USER_SPACE_PROGRAMS_START (=1000), all programs below this address are to be considered HG System programs
	- added System Programs replace/add handling in HG Update Manager

26/11/2013 1.00 beta rev 299
	- Fixed synchronization problems in SerialPortLib.SendMessage 

24/11/2013 1.00 beta rev 298
	- Fixed missing synchronization in SerialPortLib.SendMessage 
	- Fixed unhandled network error in UpdateChecker

20/11/2013 1.00 beta rev 296
	- Added Splines/Lines graph type selection to Analyze page
	- Added Fibaro products to manufacturer.xml file (http://sourceforge.net/p/homegenie/discussion/requestsandtodo/thread/013a33f4/#81a9)

18/11/2013 1.00 beta rev 295
	- Added "service restart" case to Automatic Software Update

18/11/2013 1.00 beta rev 294
	- Added Automatic Software Update functionality in Maintenance menu (http://sourceforge.net/p/homegenie/discussion/requestsandtodo/thread/8b29a412/#9a4d)

16/11/2013 1.00 beta rev 293
	- Improved Thread-Safe concurrent Collections data handling using SpinWait and ManualResetEvent

15/11/2013 1.00 beta rev 292
	- ZWaveLib: applied patch from http://sourceforge.net/p/homegenie/discussion/general/thread/3d98093f/#677f

09/11/2013 1.00 beta rev 290
	- minor fixes
	- added moment.js for UI date handling and formatting
	- switching to v 1.00 

26/10/2013 0.99 beta rev 288
	- added Modules Cleanup option in Maintenance section
	- added last trigger date info in programs list
	- fixed basic report issue in ZWaveLib 'Generic Sensor' 
	- added power report support in ZWaveLib

19/10/2013 0.99 beta rev 284
	- fixed ZWaveLib minor bug in COMMAND_CLASS_SWITCH_BINARY and COMMAND_CLASS_SWITCH_MULTILEVEL report handling
	- added new automation program that gives hg compatibility for using X10HAL a Windows Phone 7 home automation client
	- Net.SendMail now reads EMail program account data by default
	- added Modules.Groups property (List<string>) to Modules Helper class

14/10/2013 0.99 beta rev 283
	- improved generic widgets to display all fields of multi-sensors modules (eg. door/window + battery + temperature)
	- fixed control page background issue

13/10/2013 0.99 beta rev 282
	- fixed bug in ZWaveLib WakeUpInterval Set and Battery Get
	- improved generic sensor widget
	- improved statistics page

12/10/2013 0.99 beta rev 281
	- improved Wizard Scripts: the comparison value field of program conditions can now reference any module/program parameter 
	- improved statistics: now showing any logged parameter not just meter.watts
	- little css/theme rework (still work in progress)

06/10/2013 0.99 beta rev 280
	- fixed icon refresh issue in group modules
	- added program details tab to module's options popup
	- E-Mail and Security Program now using Settings helper class for reading/storing e-mail account data

04/10/2013 0.99 beta rev 279
	- added Settings helper class
	- all configuration files are now being values-encrypted

02/10/2013 0.99 beta rev 278
    - added SerialPort.OnStringReceived and SerialPort.SendMessage(string) for receiving/sending strings instead of raw bytes

27/09/2013 0.99 beta rev 277
    - applied patch with some hg fixes from Erik J.M.
    - new automation section "Messaging and Social" with E-Mail and Pushing Box programs
    - added "Presence Simulator" program
    - Security Alarm program is now reading e-mail configuration from E-Mail program

27/09/2013 0.99 beta rev 276
    - Program.Run can now execute Wizard programs also

26/09/2013 0.99 beta rev 275
    - added SerialPortHelper class for using within c# automation programs

24/09/2013 0.99 beta rev 274
    - added SerialPortLib library
    - adapted ZWaveLib.ZWavePort to work with SerialPortLib

22/09/2013 0.99 beta rev 273
    - improved Z-Wave module option page showing supported command classes, battery get, multiinstance commands
    - added arming delay and max sirens' trigger time to security program
    - z-wave wake up program now reads nodeinfo field for determining the type of query to perform on the module
    - added meter.watts info to generic sensor widget
    - added support for multichannel devices to generic switch widget

19/09/2013 0.99 beta rev 272
    - added ZWaveLib MultiInstance/Channel support (SwitchBinary, SwitchMultiLevel, SensorBinary, SensorMultiLevel)
    - added ZWaveLib Node Information query for getting supported command classes
    - fixed missing modules list update on z-wave discovery event

16/09/2013 0.99 beta rev 271
    - fixed bug in ZWaveLib Discovery and ManufactuerGet
    - added Modules.Each(...)
    - some optimization in hg core (modules list generation/update)

14/09/2013 0.99 beta rev 270
    - minor improvements to bs3 control page
    - added Sensor.Alarm property for generic alarm values (eg. tamper) 
    - fixed reading of COMMAND_CLASS_ALARM basic report value
    - added z-wave COMMAND_CLASS_SWITCH_MULTILEVEL report handling in generic dimmer driver
    - fixed interconnections / control of remote modules 
    - added Swedish locale and lingo files (thanks Adnan AKA "a." =))
    - added support for other sensor values (Flood, Smoke..) to Security Alarm program

11/09/2013 0.99 beta rev 269
    - BS3 control page access url http://<your_hg_address/hg/html/control/bs3/index.html
    - added generic sensor widget to the new Bootstrap 3 Control Page
    - implemented remote modules control
    - added Module.RoutingNode property

09/09/2013 0.99 beta rev 265
    - added HG.Configure.Groups.ModulesList webservice api call
    - minor improvements to bs3 control page

08/09/2013 0.99 beta rev 264
    - added new lightweight Control Page based on Bootstrap 3 (WIP)

07/09/2013 0.99 beta rev 263
    - better generic Sensors' devices support in ZWaveLib (10x Adnan for helping with this!)
    - added nl.lingo.json for speech recognition in Netherland (thanks Frank!)
    - Wizard program editor now hiding domains and modules that are not comparable/controllable
    - restyled Wizard condition and command popups
    - added conditions/commands Capture to Wizard program editor

06/09/2013 0.99 beta rev 262
    - wizard condition and command popups now list all types of modules/parameters/commands
    - added generic Alarm Sensor and Meter Pulse device type handlers to ZWaveLib
    - added description field in programs list
    - added "Turn Off Delay" automation program
    - added update interval parameter to Weather Underground automation program

30/08/2013 0.99 beta rev 261
    - maintenance->restore now merges automation groups with pre-existing ones
    - fixed Modules.WithoutParameter bug

28/08/2013 0.99 beta rev 260
    - fixed z-wave configuration variables get/set
    - fixed http server error (was not reading unexpected multipart post data)
    - maintenance/restore now prompts user for what programs to restore and does not delete current programs
    - improved "Generic IP Camera" APP, now using virtual modules and allow up to 10 cameras
    - added "Z-Wave WakeUp Handler" APP 

26/08/2013 0.99 beta rev 259
    - added AddFeatureTextInput method to ProgramHelper
    - added prop getter Program.Feature(<feature_name>) to ProgramHelper
    - added Modules.WithoutFeature(<feature_name>) selector
    - added missing file js/jquery-2.0.2.min.map
    - added Basic/Multilevel/Battery report buttons to z-wave module options page

24/08/2013 0.99 beta rev 258
    - more ZWaveLib optimization and code cleanup
    - associated Virtual Modules are now being deleted if a program is disabled

24/08/2013 0.99 beta rev 257
    - code cleanup/refactoring ZWaveLib
    - added temperature/luminance/humidity to generic sensor
    - added Net.AddAttachment method to NetHelper

21/08/2013 0.99 beta rev 256
    - fixed a bug in "Security Alarm System" APP

21/08/2013 0.99 beta rev 255
    - added Security Alarm widget

21/08/2013 0.99 beta rev 254
    - fixed bad image url in camerainput widget
    - added Program.RaiseEvent method to ProgramHelper
    - better garbage cleanup when stopping/disabling a program
    - rewrote most APPs for using new RaiseEvent and implement own web service 
    - added "Generic IP Camera" and "Security Alarm System" APP
    - added Net.SendMessageAsync method to NetHelper
    - Net.SendMessage now returns a bool value 

20/08/2013 0.99 beta rev 253
    - added GetBytes() method to NetHelper
    - modified generic camerainput widget to work with any module having "Image.URL" property set
    - added German locales, contributed by David

18/08/2013 0.99 beta rev 252
    - fixed: after saving a macro program type was not being set to "Wizard"
    - fixed control group renaming bug
    - fixed /dev/ttyAMA0 serial port selection issue on Raspberry Pi + RaZberry

15/08/2013 0.99 beta rev 251
    - added device type parsing to homegenie.voicecontrol.js

14/08/2013 0.99 beta rev 250
    - added preliminary Voice Control capability (works only in webkit browsers, eg. Chrome)
    - added locales/it.lingo.json and locales/en.lingo.json, italian and english data file for Voice Control
    - added js/homegenie.voicecontrol.js class

08/08/2013 0.99 beta rev 249
    - added field "Description" to automation programs
    - fixed "Invalid Date" label in control menu that was happening in IE and FF

04/08/2013 0.99 beta rev 248
    - fixed APPs config option reset, that was occurring on hg restart
    - added "Energy Saving Mode" APP
    - added When.ModuleParameterIsChanging helper method
    - added "X10 - Dimmer Bright 100% when switched on" APP
    - added "Control.Toggle" command to "Philips Hue Bridge" APP
    - fixed ZWaveLib COMMAND_CONFIG_REPORT and ConfigParameterSet that were not taking into consideration parameter size byte 
    - fixed ZWaveLib bug in generic sensor handler 
    - fixed modules features disabling (it was not actually disabling the unchecked option)

11/07/2013 0.99 beta rev 247
    - fixed error in HomeGenieService::_loadmodules that was causing problems to IR/RF events (lirc ir/x10 rf)

05/07/2013 0.99 beta rev 246
    - added option keyword "Short" to Modules.List ws call (eg. http://<hgaddr>/api/.../Modules.List/Short)

01/07/2013 0.99 beta rev 245
    - fixed concurrent write access issue when accessing HomeGenie.Modules and ProgramEngine.Programs

27/06/2013 0.99 beta rev 244
    - fixed program status update after .hgx import 
	- added display of all data fields in earth tools widget

26/06/2013 0.99 beta rev 243
    - fixed widgets caching issue
    - added Earth Tools widget
    - added DHT-11 temperature and humidity sensor widget
    - fixed HTTP authentication web server "hang" issue
    - added Net.Ping and Net.SignalModuleEvent
    - added "Interconnections" autoamation group with a couple of "Synapse" program examples
    - fixed some minor issues that were affecting the UI expecially when using IE

21/06/2013 0.99 beta rev 240
    - windows setup, raspberry sdcard image and platform agnostic bundles released
    - fixed major bug in _jsonSerializedModules 

16/06/2013 0.99 beta rev 239
    - some improvements and fixes in generic widgets
    - fixed ModuleParameter Value/LastValue logic
	- multiple lirc remotes
	- added theming 
    - ui restyle 
    - speeded up homegenie startup
    - optimized CM15 driver 
    - some minor fixes to ZWaveLib and consequent adaptations to HG
	- some UI fixes in automation section
	- fixed "Break" functionality in program editor

11/06/2013 0.99 beta rev 216
	- added Video4Linux camera driver and widget (x64 Ubuntu and Raspberry Pi)

05/06/2013 0.99 beta rev 213
    - fixed X10 CM15 driver bug (occasional readdata timeout)
    - fixed AddWidget bug

02/06/2013 0.99 beta rev 210
    - fixed Program AddInputField / AddFeature exception issue
    - other minor UI enhancements
	- added LIRC client interface driver (now HG can be controlled by any IR remote)
	- restyled footer bar to be more functional (hope)
	- added in control menu "Record Macro" functionality

24/05/2013 0.99 beta rev 199
	- added XML parsing to Net.WebService(svc).GetData()

20/05/2013 0.99 beta rev 198
	- fixed bug in z-wave nodes discovery
	- some refactoring in automation engine helper classes

14/05/2013 0.99 beta
	- added program import and export functionality
	- added Program.RegisterFeature and RegisterModuleChangedHandler

13/05/2013 0.99 alpha
	- added "Break" button in program editor to stop a running program 
	- program status now shows running,waiting,disabled states
	- fixed z-wave node discovery timeout
	- added Modules.IterationDelay( <seconds> )
	- added Program.RegisterDisplayModule and RegisterVirtualModules
	- implemented "Virtual Modules"
    - added Program.Setup(Action fn) and new program configuration/options popup
    - added Description property to ModuleParameter
    - fixed delayed UI refresh in some pages/dialogs
    - optimized generation of json modules list for having better performance on embedded syss
    - optimized program engine scheduler: condition evaluation is now going async with a result callback
	- added Program.RegisterApi command
    - added raspberry-sharp references to program engine, so automation programs can use it directly

02/05/2013 0.98 alpha
    - added program group delete button in automation programs list
	- fixed windows installer issue about sqlite.interop.dll
	- impl. maintenance menu with system backup/restore option
	- now Utility.Say has a third optional parameter "async" (default is false)
	- fixed associated module deletion when removing an automation program
	- added example program using sunset/sunrise parameters from Weather Underground widget
	- added Weather Underground widget with 

14/04/2013 0.97 alpha
	- implemented automation programs groups
	- other minor service and ui fixes
	- fixed installation issues under Windows (now we have a signle setup for any version of win OS from XP and up)
	- improved homegenie startup while loading automation program assemblies (ten times faster)
	- added Modules.WithParameter selector in program engine
	- implemented widget loading in control menu
	- added philips hue lights widget
	- added fortrezz strobe siren widget
	- added aeon labs sensor door widget
	- added homeseer hsm100 widget
	- added homegenie/generic type widgets
	- converted existing control menu code to suit new widgets structure
	- implemented customizable control widgets
	- last known zwave node device handler is now saved in modules.xml (and selected even if node is sleeping during nodes discovery phase)
	- completed aeon labs zwave door window sensor driver
	- added generic device type for door/window sensor
	- completed groups and modules sorting by drag'n'drop
	- added draft aeon labs door / window sensor driver
	- better support for hsm100 zwave motion sensor

22/03/2013 0.96 alpha
	- other minor ui fixes
	- added Utility.Say command (speech synthesys)
	- new black theme for ui
	- added c# automation program for recalling last dim level when switching on a light
	- fixed bug in hotplug feature
	- added W800RF32 interface options to system config (ui)
	- added driver for W800RF32 32bit RF Receiver
	- added editable modules parameters form
	- fixed sqlite issue in hg for win
	- added UpdateTime, LastValue, LastUpdateTime and ValueIncrement to Module data class
	- disabled gesture tap to show/hide header and footer 
	- added toggle buttons in control modules list
	- added new "Control.Toggle" api command
	- added condition type select for csharp programs as well

02/03/2013 0.95 alpha
	- added online help in automation programs
	- added basic http password protection and config security page
	- added Program.Run(<id_or_name>) to run a program from c# script 
	- added on off control popup for module type siren
	- limited statistics db file to 5Mb for now (take care of this in future)
	- fixed "Collection was modified" exception in ProgramEngine (ModulesManager)
	- fixed bug in x10 driver (byte data conversion/substring issue)
	- added fan and shutters types+icons
	- added codemirror for c# code syntax highlighting
	- generalized automation programs as hg modules. programs can now set custom persistent parameter (eg. WheaterService.Temperature)
	- added new c# scripting commands (Net.WebService and Net.MailService)
	- added Spanish translation locale file (thanks Luis!)
	- new systemconfig.xml format
	- added configuratin switches to enable/disable features/interfaces

04/02/2013 0.94 alpha
	- added NetPlay SlideShow app  
	- fixed redirect bug in webserver 
	- fixed cm15 x10 dim/bright issue 
	- added logs polling and realtime events popup in ui
	- added frech locale translation ( thanks cautor! =) )
	- added generic type Binary Sensor (0x20) handling in z-wave driver
	- added Automation States (c# scripting only for now)
	- some refactoring in main homegenie classes (old release automation programs must be saved again to work properly)
	- added X10 RF events handling (c# scripting)
	- added new commands to c# scripting engine (X10RfData, IsOn, IsOff, Level, Toggle, On, Off)

27/01/2013 0.93 alpha
	- fixed various bugs affecting c# scripting engine
	- added runtime error reporting for c# automation programs, script is disabled when runtime error occurs
	- first draft of AirPlay feature (homegenie as an airplay receiver...)
	- added couple of new commands to c# scripting engine (see support forum for specs)
	- added jQuery UI Touch Punch for better input handling on touch devices

24/01/2013 0.92 alpha
	- added experimental Raspberry Pi GPIO driver (eg. for controlling leds)
	- fixed c# scripting compilation error (missing metadata files) when running homegenie as a windows service
	- fixed issue in c# scripting: it was not reporting compiler errors, now it does
	- added fn IsInterfacePresent to MIGService (for device autoprobing)

22/01/2013 0.91 alpha
	- improved automation section
	- added c# scripting compile errors reporting and status
	- added c# scripting to automation programs
	- now automation programs can be associated to groups
	- some bug-fixing and refactoring

06/01/2013 0.90 alpha
	- improved z-wave option page 
	- completed it, en, nl locales
	- fixed CM11 driver
	- fixed bug in windows tray icon app

08/12/2012 0.86 alpha
	- added z-wave node add/remove functionality in "Configure"->"System and Interfaces"
	- added z-wave node add/remove commands to mig zwavelib
	- added z-wave webapi "Controller.NodeAdd" and "Controller.NodeRemove" commands

06/12/2012 0.85 alpha
	- fixed edit module save
	- added zwave manufacturer list as xml file 
	- more code cleanup in zwavelib
	- added app.manifest to HomeGenie Manager
	- added LogEntry.cs and Manufacturer.cs data classes

25/11/2012 0.84 alpha
	- updated windows setup for installing homegenie as a windows service
	- added homegenie tray icon manager app (windows) - VS2010
	- added homegenie service app (installs as a windows service w/ wcf) - VS2010
	- working on authentication and logging

18/11/2012 0.83 alpha
	- wrote example interface driver class in MIG\Interfaces\HomeAutomation\ExampleDriver.cs
	- major html/javascript code rework using namespacing and MVC design pattern

08/11/2012 0.82 alpha
	- added Visual Studio C# 2010 Express solution files
	- revisited UI of "groups and modules" section
	- added slide animation to collapsible list (control section)

05/11/2012 0.81 alpha
	- fixed bug: when changing X10 housecodes it was not regenerating consistent modules list

04/11/2012 0.80 alpha
	- added systemconfig.xml file for storing various system parameters settings
	- added configurable http port parameter
	- enabled xml formatting in config files generation
	- fixed synchronization issue in XTenManager
	- major 'Control' UI rework

28/10/2012 0.79 alpha
	- added "Repeat" statement to Program Engine
	- added date/time based properties to program conditions
	- fixed a bug in "switch true/false" conditions behavior
	- some ui fixes/improvements

27/10/2012 0.78 alpha
	- added enable/disable switch in program list
	- added "Run" button to program editor
	- fixed "Condition Type" selectmenu refresh issue
	- improved ProgramEngine task synchronization

21/10/2012 0.77 alpha
	- updated jquerymobile to the latest 1.2.0
	- updated automation and groups editor ui
	- major ui rework to fit on mobile phones

17/10/2012 0.76 alpha
	- Deprecated "Status" property in X10Module (MIG XTenLib)
	- Implemented property changed event propagation using ComponentModel (MIG XTenLib)
	- X10 RF commands now correctly update modules properties
	- Added automatic soft link to libusb for un*x systems

13/10/2012 0.75 alpha
	- Completed Automation/Programs list and editor with data persistence
	- Better organized html pages
	- MIG: added to web service gateway html preprocessor 'include' directive
	- Added ZWave motion detection sensor handling
	- Fixed desync issue in ProgramEngine

10/10/2012 0.73 alpha
	- Added Condition Type select to Program Editor (Configure/Automation) 
	- Fixed a couple of bugs in ProgramEngine

07/10/2012 0.72 alpha
	- Added X10 CM11/CM12 Driver
	- Fixed access error in X10 driver when closing / reopening the device
	- Added port selection for CM15 and CM11/CM12 in X10 interface options (frontend)
	
06/10/2012 0.7 alpha
	- Completed 'Virtual Metering', now has configurable watt for each module
	- Some progress in Automation / ProgramCondition
	- Minor bug fixes and improvement in frontend UI  

30/09/2012 0.6 alpha
	- Added preliminary draft of Automation/ProgramEngine
	- In Configure/Automation section is now possible to create and test an automation program (macro) 	
	- Fixed a bug in X10 Virtual Meter
	- Other minor fixes

17/09/2012 0.5 alpha 
	- first public release  




CREDITS AND RESOURCES

HomeGenie is using:
- jQuery Mobile for its UI			http://http://jquerymobile.com
- jQuery UI Touch Punch				http://touchpunch.furf.com
- Flot for statistics graphs        http://code.google.com/p/flot
- moment.js for date formatting		http://momentjs.com
- libusb for its CM15Pro Usb Driver http://www.libusb.org
- sqlite for statistics database	http://sqlite.org
- CodeMirror						http://codemirror.net
- LAME								http://lame.sourceforge.net
- Raphael Js						http://raphaeljs.com
- Raspberry#-IO						https://github.com/raspberry-sharp/raspberry-sharp-io
- LIRC								http://lirc.org
- Pepper One Z-Wave DB				http://www.pepper1.net/

Z-Wave driver originarly based on article "An introduction to Z-Wave programming in C#" (http://www.digiwave.dk/en/programming/an-introduction-to-z-wave-programming-in-c/).


