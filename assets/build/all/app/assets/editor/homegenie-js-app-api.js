const $$ = new ProgramApi();

class ProgramApi {
  /**
   * Modules Helper.
   */
  modules: ModulesManager;
  // ProgramHelperBase
  program: ProgramHelper;
  // SettingsHelper
  settings: any;
  // NetHelper
  net: any;
  // SerialPortHelper
  serial: SerialPortHelper;
  // TcpClientHelper
  tcp: any;
  // UdpClientHelper
  udp: any;
  // MqttClientHelper
  mqtt: any;
  // KnxClientHelper
  knx: any;
  // SchedulerHelper
  scheduler: SchedulerHelper;
  // Events helper (hg.When)
  on: EventsHelper;
  // API Helper
  api: ApiHelper;
  // Miscellaneous functions
  /**
   * Pause for the given amount of seconds.
   * @param seconds
   */
  pause(seconds: number);
  /**
   * Delay for the given amount of seconds. (alias of `pause(..)`)
   * @param seconds
   */
  delay(seconds: number); // alias of 'pause'
}

class EventsHelper {
  // TODO: ...
}

class ApiHelper {
  handle(apiCall: string, handler: function);

  /**
   * Parses the given (api call) string as a `MigInterfaceCommand` object.
   * @returns  The mig command.
   * @param apiCall Api Command (eg. "HomeAutomation.X10/A5/Control.Level/50").
   */
  parse(apiCall: string): MigInterfaceCommand;

  /**
   * Invoke an API command and get the result.
   * @returns  The API command response.
   * @param apiCommand Any MIG/APP API command without the `/api/` prefix.
   * @param data Data object.
   */
  call(apiCommand: string, data?: any): any;
}

class ProgramHelper {

  /**
   * Execute a setup function when the program is enabled. It is meant to be used in the "Startup Code" to execute only once
   * the instructions contained in the passed function. It is mainly used for setting program configuration fields, parameters and features.
   * @param functionBlock Function name or inline delegate.
   * @remarks
   * @example
   * Example:
   * @code
   * Program.Setup(()=>
   * {
   * Program.AddOption(
   * "MaxLevel",
   * "40",
   * "Keep level below the following value",
   * "slider:10:80");
   * Program.AddFeature(
   * "Dimmer",
   * "EnergyManagement.EnergySavingMode",
   * "Energy Saving Mode enabled light",
   * "checkbox");
   * }, 0);
   */
  setup(functionBlock: Function);

  /**
   * Playbacks a synthesized voice message from speaker.
   * @param sentence Message to output.
   * @param locale Language locale string (eg. "en-US", "it-IT", "en-GB", "nl-NL",...).
   * @param goAsync If true, the command will be executed asyncronously.
   * @remarks
   * @example
   * Example:
   * @code
   * Program.Say("The garage door has been opened", "en-US");
   */
  say(sentence: string, locale: string = null, goAsync = false): ProgramHelper;

  /**
   * Playbacks a wave file.
   * @param waveUrl URL of the audio wave file to play.
   */
  play(waveUrl: string): ProgramHelper;

  /**
   * Parses the given (api call) string as a `MigInterfaceCommand` object.
   * @returns  The mig command.
   * @param apiCall Api Command (eg. "HomeAutomation.X10/A5/Control.Level/50").
   * @deprecated Use $$.api.parse(..) instead
   */
  parseApiCall(apiCall: string): MigInterfaceCommand;

  /**
   * Invoke an API command and get the result.
   * @returns  The API command response.
   * @param apiCommand Any MIG/APP API command without the `/api/` prefix.
   * @param data Data object.
   * @deprecated Use $$.api.call(..) instead
   */
  apiCall(apiCommand: string, data?: any): any;

  /**
   * Executes a function asynchronously.
   * @returns
   * The Thread object of this asynchronous task.
   * <param name='functionBlock'>
   * Function name or inline delegate.
   */
  runAsyncTask(functionBlock: () => {}): Thread;

  /**
   * Run the program as soon as the "Setup Code" exits. This command is meant to be used in the "Setup Code".
   * @param willRun If set to @c  true will run.
   */
  run(willRun?: boolean): void;

  /**
   * Wait until the given program is not running.
   * @returns  ProgramHelper.
   * @param programId Program address or name.
   */
  waitFor(programId: string): ProgramHelper;

  /**
   * Returns a reference to the ProgramHelper of a program.
   * @returns  ProgramHelper.
   * @param programAddress Program address (id).
   */
  withAddress(programAddress: number): ProgramHelper;

  /**
   * Returns a reference to the ProgramHelper of a program.
   * @returns  ProgramHelper.
   * @param programName Program name.
   */
  withName(programName: string): ProgramHelper;

  /**
   * Pause for the given amount of seconds.
   * @param seconds
   */
  pause(seconds: number);

  /**
   * Delay for the given amount of seconds. (alias of `pause(..)`)
   * @param seconds
   */
  delay(seconds: number); // alias of 'pause'

  /**
   * This command is usually put at the end of the "Program Code". It is the equivalent of an infinite noop loop.
   */
  goBackground(): void;

  /**
   * Set widget to display for this program.
   * @param widgetId
   */
  useWidget(widgetId: string);

  /**
   * Adds a configuration option for the program. The option field will be displayed in the program options dialog using the UI widget specified by the "type" field.
   * @returns
   * ProgramHelper
   * <param name='field'>
   * Name of this input field
   * <param name='defaultValue'>
   * Default value for this input field
   * <param name='description'>
   * Description for this input field
   * <param name='type'>
   * The type of this option (eg. "text", "password", "cron.text", ...). Each type can have different initialization options
   * that are specified as ":" separated items. See html/ui/widgets folder for a list of possible types/options.
   */
  addOption(field: string, defaultValue: string, description: string, type: string): ProgramHelper;

  /**
   * Gets the value of a program option field.
   * @returns
   * The option field.
   * <param name='field'>
   * Name of the option field to get.
   * @remarks
   * @example
   * Example:
   * @code
   * // ....
   * var delay = Program.Option("OffDelay").DecimalValue;
   * Pause( delay );
   * Modules.WithFeature("Timer.TurnOffDelay").Off();
   */
  option(field: string): ModuleParameter;

  /**
   * Adds a "feature" field to modules matching the specified domain/type.
   * Feature fields are used by automation programs to create own handled module parameters.
   * This command should only appear inside a Program.Setup delegate.
   * @returns
   * ProgramHelper.
   * <param name='forDomains'>
   * Expression based on module domain names, used to select what modules will be showing this feature field. This can be either a valid Regular Expression enclosed by '/' and '/' delimiters or a simple comma (or '|') separated list of domain names (prepend '!' to a name to exclude it).
   * <param name='forModuleTypes'>
   * Expression based on module types and parameters names, used to select what modules will be showing this feature field. This must be in the format `&lt;types_expr&gt;[:&lt;parameters_expr&gt;]`. &lt;types_expr&gt; and &lt;parameters_expr&gt; can be either a valid Regular Expression enclosed by '/' and '/' delimiters or a simple comma (or '|') separated list of names (prepend '!' to a name to exclude it).
   * <param name='parameterName'>
   * Name of the module parameter bound to this feature field.
   * <param name='description'>
   * Description for this input field.
   * <param name='type'>
   * The type of this feature field (eg. "text", "password", "cron.text", ...). Each type can have different initialization options
   * that are specified as ":" separated items. See html/ui/widgets folder for a list of possible types/options.
   */
  addFeature(
    forDomains: string,
    forModuleTypes: string,
    parameterName: string,
    description: string,
    type: string
  ): ProgramHelper;

  /**
   * Return the feature field associated to the specified module parameter.
   * @param parameterName Parameter name.
   */
  feature(parameterName: string): ProgramFeature;

  /**
   * Adds a new module to the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param address Address.
   * @param type Type (Generic, Program, Switch, Light, Dimmer, Sensor, Temperature, Siren, Fan, Thermostat, Shutter, DoorWindow, MediaTransmitter, MediaReceiver).
   * @param widget Empty string or the path of the widget to be associated to the module. (optional)
   * @param implementFeatures Allow only features explicitly declared in this list. (optional)
   */
  addModule(domain: string, address: string, type: string, widget?: string, implementFeatures?: string[]): ProgramHelper;

  /**
   * Remove a module from the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param address Address.
   */
  removeModule(domain: string, address: string): ProgramHelper;

  /**
   * Adds a new set of modules to the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param startAddress Start address (numeric).
   * @param endAddress End address (numeric).
   * @param type Type.
   * @param widget? Widget to display these modules with. (optional)
   * @param implementFeatures Allow only features explicitly declared in this list. (optional)
   */
  addModules(
    domain: string,
    startAddress: number,
    endAddress: number,
    type: string,
    widget?: string,
    implementFeatures?: string[]
  ): ProgramHelper;

  /**
   * Adds a new virtual module to the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param address Address.
   * @param type Type (Generic, Program, Switch, Light, Dimmer, Sensor, Temperature, Siren, Fan, Thermostat, Shutter, DoorWindow, MediaTransmitter, MediaReceiver).
   * @param widget Widget to display this module with. (optional)
   * @deprecated Use {@link addModule `addModule(..)`} instead.
   */
  addVirtualModule(domain: string, address: string, type: string, widget: string): ProgramHelper;

  /**
   * Remove a virtual module from the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param address Address.
   * @deprecated Use {@link removeModule `removeModule(..)`} instead.
   */
  removeVirtualModule(domain: string, address: string): ProgramHelper;

  /**
   * Adds a new set of virtual modules to the system.
   * @returns  ProgramHelper.
   * @param domain Domain.
   * @param type Type.
   * @param widget Empty string or the path of the widget to be associated to the virtual module.
   * @param startAddress Start address (numeric).
   * @param endAddress End address (numeric).
   * @deprecated Use {@link addModules `addModules(..)`} instead.
   */
  addVirtualModules(
    domain: string,
    type: string,
    widget: string,
    startAddress: string,
    endAddress: string
  ): ProgramHelper;

  /**
   * Display UI notification message using the name of the program as default title for the notification.
   * @param message Message (formatting options allowed).
   * @param paramList Optional format parameters list.
   * @remarks
   * @example
   * Example:
   * @code
   * Program.Notify("Hello {0} {1}!", [firstName, lastName]);
   */
  notify(message: string, paramList?: any[]): ProgramHelper;

  /**
   * Display UI notification message from current program.
   * @param title Title.
   * @param message Message (formatting options allowed).
   * @param paramList Optional format parameters list.
   * @remarks
   * @example
   * Example:
   * @code
   * Program.Notify("Test Program", "Hello world!");
   */
  notify(title: string, message: string, paramList?: any[]): ProgramHelper;

  /**
   * Emits a new parameter value.
   * @returns  ModuleHelper.
   * @param parameter Parameter name.
   * @param value The new parameter value to set.
   * @param description Event description. (optional)
   */
  emit(parameter: string, value: any, description?: string): ModuleHelper;

  /**
   * Raise a parameter event and set the parameter with the specified value.
   * @returns  ProgramHelper.
   * @param parameter Parameter name.
   * @param value The new parameter value to set.
   * @param description Event description.
   * @deprecated Use {@link emit `emit(..)`} instead.
   */
  raiseEvent(parameter: string, value: any, description: string): ProgramHelper;

  /**
   * Gets or sets a program parameter.
   * @param parameterName Parameter name.
   * @remarks
   * @example
   * Example:
   * @code
   * // sets an example program parameter
   * Program.Parameter("MyProgram.ExampleParameter").Value = "Just testing...";
   * /// // read "Sensor.Temperature" parameter of the program named "DHT-11"
   * if (Program.WithName("DHT-11").Parameter("Sensor.Temperature").DecimalValue &lt; 20
   * && Modules.WithName("Heater").IsOff)
   * {
   * Modules.WithName("Heater").On();
   * }
   */
  parameter(parameterName: string): ModuleParameter;

  /**
   * Gets or creates a persistent data Store for this program.
   * @param storeName Store name.
   */
  store(storeName: string): StoreHelper;

  /**
   * Gets a value indicating whether the program is running.
   * @value
   * @c  true if this program is running; otherwise, @c  false.
   */
  isRunning: boolean;

  /**
   * Gets a value indicating whether the program is enabled.
   * @value
   * @c  true if this program is enabled; otherwise, @c  false.
   */
  isEnabled: boolean;

  /**
   * Get a reference to the Module associated to the program.
   * @value
   * Module object associated to the program.
   */
  Module: Module;

}

class SerialPortHelper {
  /**
   * Selects the serial port with the specified name.
   * @returns  SerialPortHelper.
   * @param port Port name.
   */
  withName(port) { return this; }

  /**
   * Connect the serial port at the specified speed.
   * @param baudRate Baud rate.
   * @param stopBits Stop Bits.
   * @param parity Parity.
   *
   */
  connect(baudRate, stopBits, parity): boolean;

  /**Disconnects the serial port. */
  disconnect(): SerialPortHelper;

  /**
   * Sends a raw data message.
   * @param message Message.
   */
  sendMessage(message): void;

  /**
   * Sets the function to call when a new string message is received.
   * @param receivedAction Function or inline delegate.
   */
  onStringReceived(receivedAction: (message: string) => void): SerialPortHelper;

  /**
   * Sets the function to call when a new raw message is received.
   * @param receivedAction Function or inline delegate.
   */
  onMessageReceived(receivedAction: (message: number[]) => void): SerialPortHelper;

  /**
   * Sets the function to call when the status of the serial connection changes.
   * @param statusChangeAction Function or inline delegate.
   */
  onStatusChanged(statusChangeAction: (status: any) => void): SerialPortHelper;

  /**
   * Gets a value indicating whether the serial port is connected.
   * @value  @c  true if connected; otherwise, @c  false.
   */
  get isConnected(): boolean;

  /**
   * Gets or sets the end of line delimiter used in text messaging.
   * @value  The end of line.
   */
  endOfLine: string;
}

const ModuleTypes = {
  generic: -1, // 0xFFFFFFFF
  program: 0,
  switch: 1,
  light: 2,
  dimmer: 3,
  sensor: 4,
  temperature: 5,
  siren: 6,
  fan: 7,
  thermostat: 8,
  shutter: 9,
  doorWindow: 10, // 0x0000000A
  doorLock: 11, // 0x0000000B
  mediaTransmitter: 12, // 0x0000000C
  mediaReceiver: 13, // 0x0000000D
}

/**Module instance. */
class Module {
  /**
   * Gets or sets the name.
   * @value  The name.
   */
  name: string;

  /**
   * Gets or sets the description.
   * @value  The description.
   */
  description: string;

  /**
   * Gets or sets the type of the device.
   * @value  The type of the device.
   */
  deviceType: ModuleTypes;

  /**
   * Gets or sets the domain.
   * @value  The domain.
   */
  domain: string;

  /**
   * Gets or sets the address.
   * @value  The address.
   */
  address: string;

  /**
   * Gets the properties.
   * @value  The properties.
   */
  properties: Array<ModuleParameter>;

  // TODO: deprecate 'Stores' field!!! (DataHelper/LiteDb can be used now to store data for a module)
  stores: Array<Store>;
}

/**
 * Module Helper class.\n
 * This class is a module instance wrapper and it is used as return value of ModulesManager.Get() method.
 */
class ModuleHelper extends ModulesManager {

  /**
   * Determines whether this module has the given name.
   * @returns  @c  true if this module has the given name; otherwise, @c  false.
   * @param name Name.
   */
  is(name: string): boolean;

  /**
   * Gets a value indicating whether this @see HomeGenie.Automation.Scripting.ModuleHelper  has a valid module instance.
   * @value  @c  true if module instance is valid; otherwise, @c  false.
   */
  exists: boolean;

  /**
   * Determines whether this module belongs to the specified domain.
   * @returns  @c  true if this module belongs to the specified domain; otherwise, @c  false.
   * @param domain Domain.
   */
  isInDomain(domain: string): boolean;

  /**
   * Gets the underlying module instance.
   * @value  The instance.
   */
  instance: Module;

  /**
   * Determines whether this module is in the specified groupList.
   * @returns  @c  true if this instance is the specified groupList; otherwise, @c  false.
   * @param groupList Comma separated group names.
   */
  isInGroup(groupList: string): boolean;

  /**
   * Determines whether this module is of one of the types specified in typeList.
   * @returns  @c  true if this module is of one of device types specified in typeList; otherwise, @c  false.
   * @param typeList Comma seprated type list.
   */
  isOfDeviceType(typeList: string): boolean;

  /**
   * Determines whether this module has the specified feature active.
   * @returns  @c  true if this module has the specified feature active; otherwise, @c  false.
   * @param feature Feature.
   */
  hasFeature(feature: string): boolean;

  /**
   * Determines whether this module has the specified parameter.
   * @returns  @c  true if this module has the specified parameter; otherwise, @c  false.
   * @param parameter Parameter.
   */
  hasParameter(parameter: string): boolean;

  /**
   * Gets the specified module parameter.
   * @param parameter Parameter.
   */
  parameter(parameter: string): ModuleParameter;

  /**
   * Gets or creates a persistent data Store for this module.
   * @param storeName Store name.
   */
  store(storeName: string): StoreHelper;

  /**
   * Emits a new parameter value.
   * @returns  ModuleHelper.
   * @param parameter Parameter name.
   * @param value The new parameter value to set.
   * @param description Event description. (optional)
   */
  emit(parameter: string, value: any, description?: string): ModuleHelper;

  /**
   * Raise a module parameter event and set the parameter with the specified value.
   * @returns  ModuleHelper.
   * @param parameter Parameter name.
   * @param value The new parameter value to set.
   * @param description Event description.
   * @deprecated Use {@link emit `emit(..)`} instead.
   */
  raiseEvent(parameter: string, value: any, description: string): ModuleHelper;

}


class ModulesManager {

  /**
   * Select modules belonging to specified domains.
   * @returns  ModulesManager
   * @param domains A string containing comma seperated domain names.
   * @remarks
   * @example
   * Example:
   * @code
   * // turn off all z-wave lights
   * Modules
   * .InDomain("HomeAutomation.ZWave")
   * .OfDeviceType("Light,Dimmer")
   * .Off();
   */
  inDomain(domains: string): ModulesManager;

  /**
   * Select modules with specified address.
   * @returns  ModulesManager
   * @param addresses A string containing comma seperated address values.
   * @remarks
   * @example
   * Example:
   * @code
   * // turn on X10 units A2 and B5
   * Modules.WithAddress("A2,B5").On();
   */
  withAddress(addresses: string): ModulesManager;

  /**
   * Select modules matching specified names.
   * @returns  ModulesManager
   * @param moduleNames A string containing comma seperated module names.
   * @remarks
   * @example
   * Example:
   * @code
   * // turn off ceiling light
   * Modules.WithName("Ceiling Light").Off();
   */
  withName(moduleNames: string): ModulesManager;

  /**
   * Select modules of specified device types.
   * @returns  ModulesManager
   * @param deviceTypes A string containing comma seperated type names.
   * @remarks
   * @example
   * Example:
   * @code
   * // turn on all lights and appliances
   * Modules.OfDeviceType("Light,Dimmer,Switch").On();
   */
  ofDeviceType(deviceTypes: string): ModulesManager;

  /**
   * Select modules included in specified groups.
   * @returns  ModulesManager
   * @param groups A string containing comma seperated group names.
   * @remarks
   * @example
   * Example:
   * @code
   * Modules.InGroup("Living Room,Kitchen").Off();
   */
  inGroup(groups: string): ModulesManager;

  /**
   * Select all modules having specified parameters.
   * @returns  ModulesManager
   * @param parameters A string containing comma seperated parameter names.
   * @remarks
   * @example
   * Example:
   * @code
   * // select all modules with Sensor.Temperature parameter and get the average temperature value
   * var averagetemperature = Modules.WithParameter("Sensor.Temperature").Temperature;
   * Program.Notify("Average Temperature", averagetemperature);
   */
  withParameter(parameters: string): ModulesManager;

  /**
   * Select all modules having specified features.
   * @returns  ModulesManager
   * @param features A string containing comma separated feature names.
   * @remarks
   * @example
   * Example:
   * @code
   * // Turn on all Security System sirens
   * Modules.WithFeature("HomeGenie.SecurityAlarm").On();
   */
  withFeature(features: string): ModulesManager;

  /**
   * Select all modules NOT having specified features.
   * @returns  ModulesManager
   * @param features A string containing comma seperated feature names.
   * @remarks
   * @example
   * Example:
   * @code
   * // Turn off all modules not having the "EnergySavingMode" feature
   * Modules.WithoutFeature("EnergyManagement.EnergySavingMode").Off();
   */
  withoutFeature(features: string): ModulesManager;

  /**
   * Iterate through each module in the current selection and pass it to the specified `callback`.
   * To break the iteration, the callback must return *true*, otherwise *false*.
   * @param callback Callback function to call for each iteration.
   * @returns  ModulesManager
   * @remarks
   * @example
   * Example:
   * @code
   * var total_watts_load = 0D;
   * Modules.WithParameter("Meter.Watts").Each( (module) => {
   * total_watts_load += module.Parameter("Meter.Watts").DecimalValue;
   * return false; // continue iterating
   * });
   * Program.Notify("Current power load", total_watts_load + " watts");
   */
  each(callback: (module: ModuleHelper) => boolean): ModulesManager;

  /**
   * Returns the module in the current selection.
   * If the current selection contains more than one element, the first element will be returned.
   * @returns  ModuleHelper
   * @remarks
   * @example
   * Example:
   * @code
   * var strobeAlarm = Modules.WithName("Strobe Alarm").Get();
   */
  get(): ModuleHelper;

  fromInstance(module: Module): ModuleHelper;

  /// <summary>
  /// Gets the complete modules list.
  /// </summary>
  /// <value>The modules.</value>

  /**
   * Gets the complete modules list.
   * @returns Array of modules
   */
  get modules(): Array<Module>;

  /**
   * Return the list of selected modules.
   * @returns Array of modules
   */
  get selectedModules(): Array<Module>;

  /**
   * Return the list of control groups.
   * @returns  List&lt;string&gt;
   */
  get groups(): Array<string>;

  /**
   * Select an API command to be executed for selected modules. To perform the selected command, Execute or Set method must be invoked.
   * @returns  ModulesManager
   * @param command API command to be performed.
   * @remarks
   * @example
   * Example:
   * @code
   * // turn on all modues of "Light" type
   * Modules.OfDeviceType("Light").Command("Control.On").Execute();
   * // set all dimmers to 50%
   * Modules.OfDeviceType("Dimmer").Command("Control.Level").Set("50");
   */
  command(command: string): ModulesManager;

  /**
   * Used before a command (*Set*, *Execute*, *On*, *Off*, *Toggle*, ...), it will put a pause after performing the command for each module in the current selection.
   * @returns  ModulesManager
   * @param delaySeconds Delay seconds.
   * @remarks
   * @example
   * Example:
   * @code
   * // Set the level of all dimmer type modules to 40%,
   * // putting a 100ms delay between each command
   * Modules
   * .OfDeviceType("Dimmer")
   * .Command("Control.Level")
   * .IterationDelay(0.1)
   * .Set(40);
   */
  iterationDelay(delaySeconds: number): ModulesManager;

  /**
   * Execute current command on first selected module and return the response value.
   * @param options Options.
   */
  getValue(options: string): string;

  /**
   * Execute current command with specified options.
   * @param options A string containing options to be passed to the selected command.
   * @returns  ModulesManager
   * @deprecated Use {@link submit `submit(..)`} instead.
   */
  execute(options?: string): ModulesManager;

  /**
   * Alias for Execute(options)
   * @param options A string containing options to be passed to the selected command.
   * @returns  ModulesManager
   * @deprecated Use {@link submit `submit(..)`} instead.
   */
  set(options?: string): ModulesManager;

  /**
   * Submits the command previously specified with `Command` method.
   * @param callback Optional callback that will be called, for each module in the selection, with the result of the issued command.
   * @returns  ModulesManager
   */
  submit(callback?: (module: Module, result: any) => void);

  /**
   * Submits the command previously specified with `Command` method, passing to it the options given by the `options` parameter.
   * @param options A string containing a slash separated list of options to be passed to the selected command.
   * @param callback Optional callback that will be called, for each module in the selection, with the result of the issued command.
   * @returns  ModulesManager
   */
  submit(options: string, callback?: (module: Module, result: any) => void);

  /**
   * Turn on all selected modules.
   * @returns  ModulesManager
   */
  on(): ModulesManager;

  /**
   * Turn off all selected modules.
   * @returns  ModulesManager
   */
  off(): ModulesManager;

  /**
   * Toggle all selected modules.
   * @returns  ModulesManager
   */
  toggle(): ModulesManager;

  /**
   * Gets or sets "Status.Level" parameter of selected modules. If more than one module is selected, when reading this property the average level value is returned.
   * @value  The level (percentage value 0-100).
   * @remarks
   * @example
   * Example:
   * @code
   * // Set the level of all modules with EnergySavingMode flag enabled to 40%
   * Modules.WithFeature("EnergyManagement.EnergySavingMode").Level = 40;
   */
  level: number;

  /**
   * Gets "on" status ("Status.Level" > 0).
   * @value  @c  true if at least one module in the current selection is on; otherwise, @c  false.
   */
  isOn: boolean;

  /**
   * Gets "off" status ("Status.Level" == 0).
   * @value  @c  true if at least one module in the current selection is off; otherwise, @c  false.
   */
  isOff: boolean;

  /**
   * Gets or sets "Status.ColorHsb" parameter of selected modules. If more than one module is selected, when reading this property the first module color is returned.
   * @value @hsb The HSB color string (eg. "0.3130718,0.986,0.65").
   */
  colorHsb: string

  /**
   * Gets "alarm" status ("Sensor.Alarm" > 0).
   * @value  @c  true if at least one module in the current is alarmed; otherwise, @c  false.
   */
  alarmed: boolean;

  /**
   * Gets "motion detection" status ("Sensor.MotionDetect" > 0).
   * @value  @c  true if at least one module in the current detected motion; otherwise, @c  false.
   */
  motionDetected: boolean;

  /**
   * Gets temperature value ("Sensor.Temperature").
   * @value  The temperature parameter of selected module (average value is returned when more than one module is selected).
   */
  temperature: number;

  /**
   * Gets luminance value ("Sensor.Luminance").
   * @value  The luminance parameter of selected module (average value is returned when more than one module is selected).
   */
  luminance: number;

  /**
   * Gets humidity value ("Sensor.Humidity").
   * @value  The humidity parameter of selected module (average value is returned when more than one module is selected).
   */
  humidity: number;

  /**
   * Creates a copy of the actual modules selection.
   * @returns  ModulesManager
   */
  copy(): ModulesManager;

  /**
   * Resets all selection filters.
   * @returns  ModulesManager
   */
  reset(): ModulesManager;
}

class ModuleParameter {
  /**
   * Gets the statistics.
   * @value  The statistics.
   */
  public statistics: ValueStatistics;

  /**
   * Gets the name.
   * @value  The name.
   */
  public name: string;

  /**
   * Gets the data object.
   */
  public getData(): any;

  /**
   * Sets the data of this parameter.
   * @param dataObject
   */
  public setData(dataObject: any): void;

  /**
   * Gets or sets the data of this parameter as string. If the value is a non-primitive object, set using the `setData` method, then the getter of `Value` will return the JSON serialized data.
   * @value  The string value.
   */
  public value: any;

  /**
   * Gets or sets the description.
   * @value  The description.
   */
  public description: string;

  /**
   * Gets or sets the type of the field.
   * @value  The type of the field.
   */
  public fieldType: string;

  /**
   * Gets the update time.
   * @value  The update time.
   */
  public updateTime: Date | string;

  /**
   * Gets the decimal value.
   * @value  The decimal value.
   */
  public decimalValue: number;

  /**
   * Determines whether this instance has the given name.
   * @returns  @c  true if this instance is name; otherwise, @c  false.
   * @param name Name.
   */
  public is(name: string): boolean;

  public requestUpdate(): void;

  /**
   * Waits until this parameter is updated.
   * @returns  @c  true, if it was updated, @c  false otherwise.
   * @param timeoutSeconds Timeout seconds.
   */
  public waitUpdate(timeoutSeconds: number): boolean;

  /**
   * Gets the idle time (time elapsed since last update).
   * @value  The idle time.
   */
  public idleTime: number;

}

class StatValue {
  /**
   * Gets the value.
   * @value  The value.
   */
  value: number;
  /**
   * Gets the timestamp.
   * @value  The timestamp.
   */
  timestamp: Date | string;

  /**
   * Gets the unix timestamp.
   * @value  The unix timestamp.
   */
  unixTimestamp: number;
}

class ValueStatistics {

  /**
   * Gets or sets the history limit.
   * @value  The history limit.
   */
  historyLimit: number;

  /**
   * Gets or sets the history limit.
   * @value  The history limit.
   */
  historyLimitSize: number;

  /**
   * Gets the history.
   * @value  The history.
   */
  history: Array<StatValue>;

  /**
   * Gets the current value.
   * @value  The current.
   */
  current: StatValue;

  /**
   * Gets the last value.
   * @value  The last.
   */
  last: StatValue;

  /**
   * Gets the last on value (value != 0).
   * @value  The last on.
   */
  lastOn: StatValue;

  /**
   * Gets the last off value (value == 0).
   * @value  The last off.
   */
  lastOff: StatValue;

  addValue(fieldName: string, value: number, timestamp: Date): void;

  isValidField(field: string): boolean;

}

class SchedulerHelper {

  /**
   * Select the schedule with the specified name.
   * @param name Name.
   */
  withName(name: string): SchedulerHelper;

  /**Get the selected schedule instance. */
  get(): SchedulerItem;

  /**
   * Add/Modify the schedule with the previously selected name.
   * @param cronExpression Cron expression.
   */
  setSchedule(cronExpression: string): SchedulerHelper;

  /**
   * Determines whether the selected schedule is matching in this very moment.
   * @returns  @c  true if the selected schedule is matching, otherwise, @c  false.
   */
  isScheduling(): boolean;

  /**
   * Determines whether the given cron expression is matching at this very moment.
   * @returns  @c  true if the given cron expression is matching; otherwise, @c  false.
   * @param cronExpression Cron expression.
   */
  isScheduling(cronExpression: string): boolean;

  /**
   * Determines whether the given cron expression is a matching occurrence at the given date/time.
   * @returns  @c  true if the given cron expression is matching; otherwise, @c  false.
   * @param date Date.
   * @param cronExpression Cron expression.
   */
  isOccurrence(date: Date | string, cronExpression: string): boolean;

  /**
   * Solar Times data.
   * @returns  SolarTime data.
   * @param date Date.
   */
  solarTimes(date: Date | string): SolarTimes;
}

class SchedulerItem {
  name: string;
  cronExpression: string;
  description: string;
  data: string;
  isEnabled: boolean;
  script: string;
  boundDevices: string[];
  boundModules: ModuleReference[];
  lastOccurrence: string;
}
class ModuleReference {
  domain: string;
  address: string;
}
