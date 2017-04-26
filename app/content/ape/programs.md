## Automation Programs

*Automation Programs* can be used either for simple tasks like creating scenarios, or more complex tasks like integrating
new devices and services into *HG* and extending system functionalities.<br/> 

<div class="media-container">
    <img self="size-medium" src="https://lh3.googleusercontent.com/i_DtXr1NEVuA1J4CLpC1_GHsY9zUHFMUCM2CQUoN4vn028zP6vQ3kCwAfMdX7ULKOA_LuKb9oiwqaq6xk3ROyNrv0h88iDieR5bMV96oAExtyF2kOIBih0qhEwkX3e-vJaOZxgyanc_vNiZZUvTFews0L94AQcSVQxUsLBQxuZcK9FuE03-tkDhNQFxX-Hxv4CNQgeJ5W-H_9huh8WzyA9W6GlNgkJ8DaAdn1Bw1VJwj7w-9lo-evUYrNHcWDw1oWwkzYdpO5HJEtpHM0-Dl56R2GeO62ZKNAW0HOvopCWF0yKQtuzJ3DOecHuV8_hoZghnh6PGBERtZwk9_bVbfIrO7pUJW9F8WeUU9txWfRiMgerMqGc9lMZqgt7st3vq_NAnzadMg3dMDjx2LfzfREEjRwm6Oi-wPuAsSc9ZFHseW6qPiyfhKhMJmscZhxWFzCyywo_69tB5LaGXS_1z9ZB9CxefqXynheq9JDiaXQo88_ppVWulXSKMujmAKQcIFbgRjx8DxZlkMxspGhp49cY0cSBQy2eRin-AZVCbDGV4=w1172-h783-no" />
</div>


## Program Editor

Program Editor can be accessed from the *Automation* section of the **Configure** menu. There, automation programs are
conveniently organized into groups. New automation programs can be created by choosing a group and then selecting the
*Add program* option from the *Action* menu located in the down-right corner.
An automation program can be coded using one of the following programming languages:

- C#
- Javascript
- Python
- Ruby

all languages can use the same set of [Helper Classes](api/ape/annotated.html) to access *HG* resources or external services
in the same way.

### Example - Turning off lights in a given group

The following example is using the [ModulesManager](api/ape/a00006.html) helper class to turn off all lights in the *Porch* group:

```javascript
// C#
var lights = Modules.InGroup("Porch");
porchLights.Off();

// Javascript
lights = hg.Modules.InGroup('Porch');
lights.Off();
// Javascript with camelCase
lights = hg.modules.inGroup('Porch');
lights.off();

// Python
lights = hg.Modules.InGroup('Porch')
lights.Off()

// Ruby
lights = hg.Modules.InGroup("Porch")
lights.Off()
```

so all of them are very similar, just using own language specific syntax. A few remarks:

- **hg.** prefix can be omitted when using C#, but must be used for all other languages
- **camelCase** coding practice can be adopted for *Javascript* programs

Some [Helper Classes](api/ape/annotated.html) functions may require a callback function as argument. It might be useful to
show how callback functions can be passed in the various language flavours.<br/>
The following example is using [SystemStarted](api/ape/a00001.html#afcf0d379d8dd2da00c2adf1c3c9996f3) helper function that
requires a callback as argument.

### Example - Using delegates in different language flavours

```javascript
// Output a speech message from the speaker after *HG* is started and ready.
// C#
When.SystemStarted(()=>{
    Program.Say("HomeGenie is now ready!");
    return true;
}); 

// Javascript
hg.when.systemStarted(function(){
    hg.program.say('HomeGenie is now ready!');
    return true;
});

// Python
def started_handler():
    hg.Program.Say('HomeGenie is now ready!')
    return True
hg.When.SystemStarted(started_handler)

// Ruby
started_fn = lambda {
  hg.Program.Say("HomeGenie is now ready!")
  return true
}
hg.When.SystemStarted(started_fn)
```

Further documentation about specific syntax of each language can be found on the following pages (or just googlin'):

- [C#](https://msdn.microsoft.com/en-us/library/aa645597%28v=vs.71%29.aspx)
- Javascript - [Jint](https://github.com/sebastienros/jint)
- Python - [IronPython](http://ironpython.net/documentation/)
- Ruby - [IronRuby](http://ironruby.net/documentation/)

## Program Code and Startup Code

An automation program is splitted into two piece of code. The *Program Code*, which is the main program code,
and the *Startup Code* (also called *Trigger Code* in previous HG release).
*Startup Code* is a piece of code that can be used to set various program options and also to tell *HG* when to run the
program by using the [Program.Run](api/ape/a00009.html#a34a937a2bc052615d27137c3663d10c6) instruction in it.
When the program is idle (not running), the *Startup Code* is evaluated every second or as soon as any event is raised
in the system.

### Example - Startup Code

```csharp
// Run the program every day at 7 am
if (Scheduler.IsScheduling("0 7 * * *"))
    Program.Run();
```

 Other instructions commonly used in the *Startup Code* are:

- [Program.Setup](api/ape/a00009.html#aba65b477efba06ac22a4f908881bbece)
- [Program.UseWidget](api/ape/a00009.html#ab4c0baea094fcf0f29c57cf1f157c68b)
- [Program.AddOption](api/ape/a00009.html#a77342214dc230ce4eab47ffc4c10bd7f)
- [Program.AddFeature](api/ape/a00009.html#a59e041d4aa2ea5fcd00d4a8b5efacc6b)

The use of these command is described later in this page.

**NOTE:** when [Program.Run](api/ape/a00009.html#a34a937a2bc052615d27137c3663d10c6) is called in the *Startup Code*,
the program's code will run only after that *Startup Code* reaches it's end. So it's a good practice not to put any
long-time consuming operation or infinite loop in the *Startup Code*.
<br/>
*Startup Code* and *Program Code* are not running in the same scope. Data between them that can be shared by using
[Modules parameters](api/ape/a00004.html#a016f9d7512c4407acbfc2d7c72d02a17) and other structures such as
[Program Store](api/ape/a00009.html#a111c3e247c9a22cf44e032bfb568f876) and [System Settings](api/ape/a00012.html).

<a name="modules"></a>


## Virtual Modules, Program Module and Widgets

Virtual Modules (or just modules) are used in *HG* as an abstraction for devices or services. So, each of them, represent
a particular device or service in the system and holds its data into *Parameter* fields. For example: a module for a
light switch device will have a parameter called *Status.Level* for indicating the current state of the light
(1 = turned on, 0 = turned off); a temperature sensor module will have a *Sensor.Temperature* parameter... and so on.<br/>

Each module is identified by **Domain**, **Address**, **Type** and **Widget**. The **Domain** is used as a group for same class
of modules. Example of domain names are: `HomeAutomation.ZWave`, `HomeAutomation.PhilipsHue`, `HomeAutomation.X10`.
The **Address**, that is usually a number, is used to identify uniquely each module belonging to the same domain. The **Type**
will define what kind of module is. Commonly used types are: `Program`, `Switch`, `Light`, `Dimmer`, `Sensor`,
`Temperature`, `Siren`, `Fan`, `Thermostat`, `Shutter`, `DoorWindow`, `DoorLock`,
`MediaTransmitter`, `MediaReceiver`. The **Widget** will determine how a module will be displayed in the User
Interface. There are already a bunch of widgets available in *HG*, but custom ones can also be designed using the 
integrated [Widget Editor](widgets.html).

Also automation programs have a module associated to each of them, so program's data can be displayed in the
UI using a widget. The standard widget that can be used for a program is a simple button that once clicked runs the
program.

### Example - Associating a widget to a program

```csharp
// This program will be displayed as a simple button
Program.UseWidget("homegenie/generic/program");
```

A program can also create and handle futher modules by using the following functions:

- [Program.AddVirtualModules](api/ape/a00009.html#a6ce0c82ab9edb50be6689919cf29c1ca)
- [Program.AddVirtualModule](api/ape/a00009.html#ad74a2a82101dd80e17244fd03eaf181f)
- [Program.RemoveVirtualModule](api/ape/a00009.html#ac1c5f107619fed38f6cb9d9224fd3506)

## Program Options

Automation programs can have an options dialog in the UI, so that the user may configure some aspect of it. As an example
we can look at the *Weather Underground* program. This program needs to know the city name so to display weather data of
the given location. 

### Example - Adding a field to the program options dialog

```csharp
// Add "city" text field to the program options UI dialog
Program.AddOption(
    "Location", // <-- identifier name of the option
    "autoip", // <-- default value
    "City name", // <-- description
    "text"); // <-- UI field type and parameters
// ...
```

### Example - Getting the entered value of a program option

```csharp
var location = Program.Option("Location").Value;
```

The [Program.AddOption](api/ape/a00009.html#a77342214dc230ce4eab47ffc4c10bd7f) instruction is meant to be used inside
the [Program.Setup](api/ape/a00009.html#aba65b477efba06ac22a4f908881bbece) delegate.

## Program Features

In a similar way as described for the program options dialog, module also have an UI options dialog where an automation
program can add its own options so to let the user configure different per-module values. As an example we can look
at the *Automatic turn-off* program. This program will add to the options dialog of modules a new field where the
user can set the turn-off delay for each module.

### Example - Adding a program feature to modules

```csharp
// This will display a slider for setting the timeout
// in the module options UI dialog
Program.AddFeature(
    "", // <-- affected domain name (empty "" string stands for "any domain")
    "Switch,Light,Dimmer", // <-- affected module types
    "HomeGenie.TurnOffDelay", // <-- identifier name for this feature
    "Automatic turn off delay (seconds)", // <-- description
    "slider:0:3600:1"); // <-- UI field type and parameters
Program.Run();
```

The last parameter of the [Program.AddFeature](api/ape/a00009.html#a59e041d4aa2ea5fcd00d4a8b5efacc6b) function will select the type of control that will be displayed in 
the UI module options dialog. The following are the currently implemented UI field types:

- `text`
<br/>a simple text field
- `checkbox`
<br/>a checkbox field
- `slider:<min>:<max>:<step>`
<br/>a slider control
- `password`
<br/>a password field
- `module.text:<match_domain>:<match_type>:<match_parameter>`
<br/>autocomplete field for selecting a module
- `capture:<parameter_to_capture>`
<br/>capture the next value coming from a given parameter event
- `cron.text`
<br/>field with wizard dialog for building scheduler cron expression
- `store.script`
- `store.script.list`

## Reacting to module events

Each time a module parameter is updated a new event is raised in the system. By using either the
[ModuleParameterIsChanging](api/ape/a00001.html#a2345d703592c2fe90284b13ce7ac2650) or the
[ModuleParameterChanged](api/ape/a00001.html#a7e82383574aeff32db8d09d4eb916718)
function, a program can listen to the system event stream and react in consequence of a module event.
The difference between *ModuleParameterIsChanging* and *ModuleParameterChanged*, is that the first one is called before the
latter one. In most situations *ModuleParameterChanged* will be used.

### Example - Apply turn off timeout when a module is switched on

#### CSharp
```csharp
// this function will be called each time a module parameter is updated
When.ModuleParameterChanged((module, parameter) => {
  // check if the module raising the event has the Turn Off Delay set
  if (module.HasFeature("HomeGenie.TurnOffDelay") &&
      module.Parameter("HomeGenie.TurnOffDelay").DecimalValue > 0)
  {
    // Check if the module has just been turned on
    if (parameter.Is("Status.Level") &&
        parameter.Statistics.Last.Value == 0 &&
        parameter.DecimalValue > 0)
    {
      // Run a background timer that will turn off the light
      var pauseDelay = module.Parameter("HomeGenie.TurnOffDelay").DecimalValue;
      Program.RunAsyncTask(()=>{
        Pause(pauseDelay);
        // Check if the light is still on (also module.IsOn could be used)
        if (parameter.DecimalValue > 0)
        {
          module.Off();
          Program.Notify("Turn Off Delay",
            module.Instance.Name + "<br>" + 
            module.Instance.Address + 
            " switched off.");
        }
      });
    }
  }
  return true;
});
// the program will be running in the background waiting for events
Program.GoBackground();
```

#### Javascript

```javascript
hg.when.moduleParameterChanged(function(module, parameter) {
    // put code here using "module" and "parameter" objects
    return true;
});
hg.program.goBackground();
```

#### Python
```python
def module_updated_fn(mod,par):
    # put code here using "mod" and "par" objects
    return True
hg.When.ModuleParameterChanged(module_updated_fn)
hg.Program.GoBackground()
```

#### Ruby
```ruby
module_updated_fn = lambda { |mod,par|
    # put code here using "mod" and "par" objects
    return true
}
hg.When.ModuleParameterChanged(module_updated_fn)
hg.Program.GoBackground()
```

Automation programs can also raise events, so the system (and other programs as well) will acknowledge when a module has been updated.
The function [Program.RaiseEvent](api/ape/a00009.html#af51db91ed13809da94337aac3c1053b7) is meant for that.

<a name="commands"></a>


## Reacting to commands

When the user click a button or any other control of a module widget, an [API](api/mig/overview.html) command request is
sent to *HG*. A standard HTTP API request, follows the syntax:<br/>
`/api/<module_domain>/<module_address>/<command>[/<option_1>/../<option_n>]`<br/>
For example when clicking **On** and **Off** buttons on the widget of a **Z-Wave** switch with node id **5**, the following
HTTP requests are made:

```
/api/HomeAutomation.ZWave/5/Control.On
/api/HomeAutomation.ZWave/5/Control.Off
```
So if an automation program creates a [virtual module](api/ape/a00009.html#ad74a2a82101dd80e17244fd03eaf181f) of switch type in the domain *MyProgram.Domain*

```csharp
Program.AddVirtualModule(
    "MyProgram.Domain",          // <-- domain name
    "1",                         // <-- module address
    "Switch",                    // <-- module type
    "homegenie/generic/switch"); // <-- widget used to display this module
```

it will be able to handle commands addressed to this module by listening to API calls going to the *MyProgram.Domain* domain.
The [When.WebServiceCallReceived](api/ape/a00001.html#a58515455945c35783cde47d21f844663) helper function is used for this purpose:

#### CSharp
```csharp
// handle requests of type http://<hg_address>/api/MyProgram.Domain/...
When.WebServiceCallReceived("MyProgram.Domain", (args) => {
    // All API requests going to the "MyProgram.Domain" 
    // will hit this piece of code.
    // e.g. for the "On" command, <args> will contain the string
    // "MyProgram.Domain/1/Control.On", so...
    string[] req = ((string)args).Split('/');
    string domain = req[0], address = req[1], command = req[2];
    // be optimistic
    var response = "{ \"ResponseValue\" : \"Ok\" }";
    // get a reference to the addressed module
    var module = Modules.InDomain(domain).WithAddress(address).Get();
    // process the command
    switch (command)
    {
        case "Control.On":
            // set the status of the module to on
            Program.RaiseEvent(module, "Status.Level", "1", "Switched on");
            break;
        case "Control.Off":
            // set the status of the module to off
            Program.RaiseEvent(module, "Status.Level", "0", "Switched off");
            break;
        default:
            response = "{ \"ResponseValue\" : \"Error\" }";
            Program.Notify("MyProgram", "Unrecognized command received");
            break;
    }
    return response;
});
Program.GoBackground();
```

So when the user will click the *On* and *Off* buttons of the virtual module widget, the code above will raise an event 
that will update the module *Status.Level* property with *1* or *0*. This event will be also received by the widget that will so
update the displayed data accordingly.

#### Javascript
```javascript
// handle requests of type http://<hg_address>/api/MyProgram.Domain/...
hg.when.webServiceCallReceived('MyProgram.Domain', function(args) {
    // handle the request here....
    return { ResponseValue : 'Ok' };
});
hg.program.goBackground();
```

#### Python
```python
# handle requests of type http://<hg_address>/api/MyProgram.Domain/...
def handle_request_fn(args):
    # handle the request here....
    return "Ok"
hg.When.WebServiceCallReceived("MyProgram.Domain", handle_request_fn)
hg.Program.GoBackground()
```

#### Ruby
```ruby
# handle requests of type http://<hg_address>/api/MyProgram.Domain/...
handle_request_fn = lambda { |args|
    # handle the request here....
    return "Ok"
}
hg.When.WebServiceCallReceived("MyProgram.Domain", handle_request_fn)
hg.Program.GoBackground()
```

## Common operations on modules

See the [ModulesManager](api/ape/a00006.html) helper class documentation to find out all available functions and properties.

### Example - Selecting modules

```csharp
// Selecting dimmer lights in the same group
var roomDimmers = Modules.InGroup("Living Room").OfDeviceType("Dimmer");
// Set the level of living room dimmers to 50%
roomDimmers.Level = 50;
// Say the average temperature of all sensors
var tempSensors = Modules
    .OfDeviceType("Sensors")
    .WithParameter("Sensor.Temperature");
Program.Say("The average temperature is " + tempSensors.Temperature);
```

### Example - Reading a module parameter

```csharp
// Getting a single module
var tempSensor = Modules.InDomain("HomeAutomation.ZWave").WithAddress("5").Get();
var temperature = tempSensor.Parameter("Sensor.Temperature");
// .Value property returns a string
Program.Say("Current temperature value is " + temperature.Value);
// .DecimalValue property returns a number (double)
if (temperature.DecimalValue > 24)
{
    Program.Say("Well... it's actually kinda hot day!");
}
```

### Example - Selecting/Testing modules having a given feature

```csharp
// Selecting all modules with the "turn off" feature
var mods = Modules.WithFeature("HomeGenie.TurnOffDelay");
// Testing if a module have the "turn off" feature
var porchLight = Modules.WithName("My Porch Light").Get();
if (porchLight.HasFeature("HomeGenie.TurnOffDelay"))
{
    var timeout = porchLight.Parameter("HomeGenie.TurnOffDelay").Value;
    Program.Say("The porch light has a turn off timeout of " + timeout);
}
```
