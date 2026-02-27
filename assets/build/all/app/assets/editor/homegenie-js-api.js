const $$ = new SchedulerApi();

class SchedulerApi {
  /**
   * Modules bound to this event.
   */
  get boundModules(): ModulesManager;
  /**
   * Modules Helper.
   */
  get modules(): ModulesManager;
  /**
   * API Helper class. Provides methods for handling and invoking HomeGenie's web service API calls.
   */
  api: ApiHelper;
  /**
   * Program Helper. Provides methods related to the current program or script execution context.
   */
  program: ProgramHelper;
  /**
   * Settings Helper. Provides methods to read and write HomeGenie's system or user settings.
   */
  settings: SettingsHelper;
  /**
   * Net Helper. Provides network-related utility functions, typically for DNS resolution or general network info.
   */
  net: NetHelper;
  /**
   * Serial Port Helper. Provides methods to communicate with devices via a local serial port.
   */
  serial: SerialPortHelper;
  /**
   * TCP Client Helper. Provides client-side functionality for TCP network communication.
   */
  tcp: TcpClientHelper;
  /**
   * UDP Client Helper. Provides client-side functionality for UDP network communication.
   */
  udp: UdpClientHelper;
  /**
   * MQTT Client Helper. Provides methods for interacting with an MQTT broker (publish/subscribe).
   */
  mqtt: MqttClientHelper;
  /**
   * KNX Client Helper. Provides methods for interacting with devices on a KNX bus.
   */
  knx: KnxClientHelper;
  /**
   * Scheduler Helper. Manages and interacts with scheduled tasks and timers within HomeGenie.
   */
  scheduler: SchedulerHelper;
  /**
   * The scheduler event item
   */
  event: SchedulerItem;
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
  /**
   * Returns `true` if this event will also occur on next minute, `false` otherwise.
   */
  onNext(): boolean;
  /**
   * Returns `true` if this event did also occur on previous minute, `false` otherwise,
   */
  onPrevious(): boolean;
  /**
   * Store data that can be read across the occurrences of the schedule.
   * @param key
   * @param value
   */
  data(key: string, value?: any);
  onUpdate(handler: (module: ModuleHelper, parameter: ModuleParameter) => any);
}

/**
 * @description API Helper class. Provides methods for handling and invoking HomeGenie's web service API calls.
 *              Class instance accessor: `hg.api`.
 */
class ApiHelper {
  /**
   * Invokes an API command and gets the result.
   * @param apiCommand Any MIG/APP API command without the `/api/` prefix (e.g., "HomeAutomation.X10/A5/Control.Level/50").
   * @param data Optional data object to pass with the command.
   * @returns The API command response object.
   */
  call(apiCommand: string, data?: any): any;
}

/**
 * @description Program Helper class. Provides methods related to the current program or script execution context,
 *              including setup, configuration, module creation, and state management.
 *              Class instance accessor: `hg.program`.
 */
class ProgramHelper {

  /**
   * Execute a setup function when the program is enabled. It is meant to be used in the "Setup Code" to execute only once
   * the instructions contained in the passed function. It is mainly used for setting program configuration fields, parameters and features.
   * @param functionBlock The function block (name or inline delegate) containing setup instructions.
   * @example
   * Program.Setup(()=>
   * {
   *   Program.AddOption(
   *     "MaxLevel",
   *     "40",
   *     "Keep level below the following value",
   *     "slider:10:80");
   *   // ...
   * });
   */
  setup(functionBlock: () => void): void;

  /**
   * Playbacks a synthesized voice message from speaker.
   * @param sentence Message to output.
   * @param locale Language locale string (e.g., "en-US", "it-IT", "en-GB", "nl-NL",...).
   * @param goAsync If true, the command will be executed asynchronously.
   * @example
   * Program.Say("The garage door has been opened", "en-US");
   * @returns The ProgramHelper instance for method chaining.
   */
  say(sentence: string, locale?: string, goAsync?: boolean): ProgramHelper;

  /**
   * Playbacks a wave file.
   * @param waveUrl URL of the audio wave file to play.
   * @returns The ProgramHelper instance for method chaining.
   */
  play(waveUrl: string): ProgramHelper;

  /**
   * Executes a function asynchronously.
   * @param functionBlock Function name or inline delegate.
   * @returns The Thread object of this asynchronous task.
   */
  runAsyncTask(functionBlock: () => any): Thread;

  /**
   * Runs the program code. This command is meant to be used in the "Setup Code" to trigger the main program logic.
   * @param willRun If set to `true`, the program will run as soon as the "Setup Code" exits. If omitted, assumes `true`.
   */
  run(willRun?: boolean): void;

  /**
   * Wait until the given program is not running.
   * @param programId Program address or name.
   * @returns The ProgramHelper instance for method chaining.
   */
  waitFor(programId: string): ProgramHelper;

  /**
   * Returns a reference to the ProgramHelper of a program by its address.
   * @param programAddress Program address (id).
   * @returns The ProgramHelper instance for the specified program.
   */
  withAddress(programAddress: number): ProgramHelper;

  /**
   * Returns a reference to the ProgramHelper of a program by its name.
   * @param programName Program name.
   * @returns The ProgramHelper instance for the specified program.
   */
  withName(programName: string): ProgramHelper;

  /**
   * Pause the current program execution for the given amount of seconds.
   * @param seconds The duration of the pause in seconds.
   */
  pause(seconds: number): void;

  /**
   * Delay the current program execution for the given amount of seconds. (Alias of `pause(...)`).
   * @param seconds The duration of the delay in seconds.
   */
  delay(seconds: number): void;

  /**
   * This command is usually put at the end of the "Program Code". It is the equivalent of an infinite no-op loop, keeping the program alive in the background.
   */
  goBackground(): void;

  /**
   * Set the widget that will be used for displaying this program data in the UI Control page.
   * @param widgetId The widget path (e.g., 'Widgets/Program.Generic').
   * @returns The ProgramHelper instance for method chaining.
   */
  useWidget(widgetId: string): ProgramHelper;

  /**
   * Adds a configuration option for the program. The option field will be displayed in the program options dialog.
   * This command should only appear inside a `Program.Setup` delegate.
   * @param field Name of this input field.
   * @param defaultValue Default value for this input field.
   * @param description Description for this input field.
   * @param type The type of this option (e.g., "text", "password", "cron.text", "slider:10:80").
   * @returns The ProgramHelper instance for method chaining.
   */
  addOption(field: string, defaultValue: string, description: string, type: string): ProgramHelper;

  /**
   * Gets the value of a program option field.
   * @param field Name of the option field to get.
   * @returns The option field as a ModuleParameter object.
   * @example
   * var delay = Program.Option("OffDelay").DecimalValue;
   */
  option(field: string): ModuleParameter;

  /**
   * Adds a "feature" field to modules matching the specified domain/type. Feature fields are used by automation programs to create own handled module parameters.
   * This command should only appear inside a `Program.Setup` delegate.
   * @param forDomains Expression based on module domain names (Regex or comma-separated list).
   * @param forModuleTypes Expression based on module types and parameters names (`<types_expr>[:<parameters_expr>]`).
   * @param parameterName Name of the module parameter bound to this feature field.
   * @param description Description for this input field.
   * @param type The type of this feature field (e.g., "text", "checkbox").
   * @returns The ProgramHelper instance for method chaining.
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
   * @returns The ProgramFeature object.
   */
  feature(parameterName: string): ProgramFeature;

  /**
   * Adds a new module to the system.
   * @param domain Domain (e.g., 'HomeAutomation.ZWave').
   * @param address Address string.
   * @param type Module Type (e.g., 'Switch', 'Light', 'Sensor').
   * @param widget Empty string or the path of the widget to be associated to the module (optional).
   * @param implementFeatures Allow only features explicitly declared in this list (optional).
   * @returns The ProgramHelper instance for method chaining.
   */
  addModule(domain: string, address: string, type: string, widget?: string, implementFeatures?: string[]): ProgramHelper;

  /**
   * Remove a module from the system.
   * @param domain Domain.
   * @param address Address.
   * @returns The ProgramHelper instance for method chaining.
   */
  removeModule(domain: string, address: string): ProgramHelper;

  /**
   * Adds a new set of modules to the system.
   * @param domain Domain.
   * @param startAddress Start address (numeric).
   * @param endAddress End address (numeric).
   * @param type Module Type.
   * @param widget Widget to display these modules with (optional).
   * @param implementFeatures Allow only features explicitly declared in this list (optional).
   * @returns The ProgramHelper instance for method chaining.
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
   * Display UI notification message using the name of the program as default title for the notification.
   * @param message The message (formatting options allowed).
   * @param paramList Optional format parameters list.
   * @returns The ProgramHelper instance for method chaining.
   * @example
   * Program.Notify("Hello {0} {1}!", [firstName, lastName]);
   */
  notify(message: string, paramList?: any[]): ProgramHelper;

  /**
   * Display UI notification message from current program with a custom title.
   * @param title The notification title.
   * @param message The message (formatting options allowed).
   * @param paramList Optional format parameters list.
   * @returns The ProgramHelper instance for method chaining.
   * @example
   * Program.Notify("Test Program", "Hello world!");
   */
  notify(title: string, message: string, paramList?: any[]): ProgramHelper;

  /**
   * Emits a new parameter value for the program's module.
   * This triggers the event propagation system (ModuleParameterChanged).
   * @param parameter Parameter name.
   * @param value The new parameter value to set.
   * @param description Event description (optional).
   * @returns The ModuleHelper instance (implied by the original C# method return in `ModuleHelperBase`).
   */
  emit(parameter: string, value: any, description?: string): ModuleHelper;

  /**
   * Gets or sets a program parameter.
   * @param parameterName Parameter name.
   * @returns The ModuleParameter object for the specified parameter.
   */
  parameter(parameterName: string): ModuleParameter;

  /**
   * Gets or creates a persistent data Store for this program.
   * @param storeName Store name.
   * @returns The StoreHelper object.
   */
  store(storeName: string): StoreHelper;

  /**
   * Gets a value indicating whether the program is running.
   * @returns `true` if this program is running; otherwise, `false`.
   */
  isRunning: boolean;

  /**
   * Gets a value indicating whether the program is enabled.
   * @returns `true` if this program is enabled; otherwise, `false`.
   */
  isEnabled: boolean;

  /**
   * Get a reference to the Module associated to the program.
   * @returns Module object associated to the program.
   */
  module: Module;

  /**
   * Force update module database with current data. This is typically used after dynamic module additions/removals.
   * @returns `true` on successful update, `false` otherwise.
   */
  updateModuleDatabase(): boolean;

  /**
   * Resets the program's module, removing all options and features added by the program.
   * @returns The ProgramHelper instance for method chaining.
   */
  reset(): ProgramHelper;

  /**
   * Restarts the current program.
   * @returns The ProgramHelperBase instance.
   */
  restart(): ProgramHelperBase;
}

/**
 * @description Net Helper class. Provides comprehensive methods for network communication,
 *              including web service calls (HTTP), email (SMTP/IMAP), and network diagnostics (Ping).
 *              Class instance accessor: `hg.net`.
 */
class NetHelper {

  /**
   * Sets the web service URL to call for subsequent HTTP requests.
   * @param serviceUrl The full URL of the web service.
   * @returns The NetHelper instance for method chaining.
   * @example
   * var iplocation = hg.net.webService("http://freegeoip.net/json/").getData();
   */
  webService(serviceUrl: string): NetHelper;

  /**
   * Sets the WebClient connection timeout for subsequent HTTP requests.
   * @param seconds The timeout duration in seconds (default is 10 seconds).
   * @returns The NetHelper instance for method chaining.
   */
  withTimeout(seconds: number): NetHelper;

  /**
   * Sends the specified string data using the HTTP PUT method.
   * @param data The string data to send in the PUT request body.
   * @returns The NetHelper instance for method chaining.
   */
  put(data: string): NetHelper;

  /**
   * Sends the specified string data using the HTTP POST method.
   * @param data String containing post data fields and values in the form `field1=value1&field2=value2&...`.
   * @returns The NetHelper instance for method chaining.
   */
  post(data: string): NetHelper;

  /**
   * Adds the specified HTTP header to the HTTP request.
   * @param name Header name.
   * @param value Header value.
   * @returns The NetHelper instance for method chaining.
   */
  addHeader(name: string, value: string): NetHelper;

  /**
   * Calls the web service URL using the configured method (GET, PUT, POST) and returns the raw server response as a string.
   * @returns A string containing the server response.
   */
  call(): string;

  /**
   * Calls the web service URL using the configured method (GET, PUT, POST) and returns the server response,
   * attempting to map the result from JSON/XML into a dynamic object.
   * @returns A dynamic object containing all fields mapped from the JSON/XML response, or a simple string.
   */
  getData(): any;

  /**
   * Calls the web service URL using the configured method (GET, PUT, POST) and returns the server response as binary data.
   * @returns A byte array (represented as `number[]` in JS) containing the raw server response.
   */
  getBytes(): number[];

  /**
   * Ping the specified remote host.
   * @param remoteAddress The IP or DNS address of the remote host.
   * @returns `true` if the ping was successful; otherwise, `false`.
   */
  ping(remoteAddress: string): boolean;

  /**
   * Uses provided username and password for authentication (e.g., Basic Authentication).
   * @param user Username.
   * @param pass Password.
   * @returns The NetHelper instance for method chaining.
   */
  withCredentials(user: string, pass: string): NetHelper;

  /**
   * Uses the default network credentials of the system when connecting.
   * @returns The NetHelper instance for method chaining.
   */
  withDefaultCredentials(): NetHelper;

  /**
   * Sets the SMTP server address for sending emails.
   * @param smtpServer The SMTP server address.
   * @returns The NetHelper instance for method chaining.
   */
  mailService(smtpServer: string): NetHelper;

  /**
   * Sets the SMTP server address and connection details for sending emails.
   * @param smtpServer The SMTP server address.
   * @param port The SMTP server port.
   * @param useSsl If `true`, uses SSL for the connection.
   * @returns The NetHelper instance for method chaining.
   */
  mailService(smtpServer: string, port: number, useSsl: boolean): NetHelper;

  /**
   * Adds an attachment to the message to be sent. Can be called multiple times.
   * @param name File name (without path).
   * @param data Binary data of the file to attach (as a byte array/`number[]`).
   * @returns The NetHelper instance for method chaining.
   */
  addAttachment(name: string, data: number[]): NetHelper;

  /**
   * Sends an E-Mail using the configured mail service.
   * @param recipients Comma-separated list of message recipients.
   * @param subject Message subject.
   * @param messageText Message body text.
   * @returns `true` if the message was sent successfully; otherwise, `false`.
   */
  sendMessage(recipients: string, subject: string, messageText: string): boolean;

  /**
   * Sends an E-Mail with a custom 'From' address using the configured mail service.
   * @param from Message sender's email address.
   * @param recipients Comma-separated list of message recipients.
   * @param subject Message subject.
   * @param messageText Message body text.
   * @returns `true` if the message was sent successfully; otherwise, `false`.
   */
  sendMessage(from: string, recipients: string, subject: string, messageText: string): boolean;

  /**
   * Creates and initializes an IMAP mail client helper for receiving emails.
   * @param host IMAP host address.
   * @param port IMAP port number.
   * @param useSsl If `true`, uses SSL for the connection.
   * @returns The ImapClient object.
   */
  imapClient(host: string, port: number, useSsl: boolean): any; // ImapClient;

  /**
   * Resets all current settings (URL, method, timeout, headers, attachments) on the NetHelper instance to their default values.
   */
  reset(): void;
}

/**
 * @description Serial port helper. Provides methods for establishing, managing, and communicating over a serial port connection.
 *              Class instance accessor: `hg.serial`.
 */
class SerialPortHelper {
  /**
   * Selects the serial port with the specified name.
   * @param port Port name (e.g., 'COM3' or '/dev/ttyS0').
   * @returns The SerialPortHelper instance for method chaining.
   */
  withName(port: string): SerialPortHelper;

  /**
   * Connects to the selected serial port at the default speed of 115200bps.
   * @returns `true` if connection succeeded; otherwise, `false`.
   */
  connect(): boolean;

  /**
   * Connects to the serial port at the specified speed and communication settings.
   * @param baudRate Baud rate (e.g., 9600, 115200).
   * @param stopBits Stop Bits (e.g., typically an enumeration or string like 'One', 'Two', etc.).
   * @param parity Parity (e.g., typically an enumeration or string like 'None', 'Even', 'Odd').
   * @returns `true` if connection succeeded; otherwise, `false`.
   */
  connect(baudRate: number, stopBits: any, parity: any): boolean;

  /**
   * Disconnects the serial port.
   * @returns The SerialPortHelper instance for method chaining.
   */
  disconnect(): SerialPortHelper;

  /**
   * Sends a string message. The message will be appended with the current `endOfLine` delimiter.
   * @param message The string message to send.
   */
  sendMessage(message: string): void;

  /**
   * Sends a raw data message.
   * @param message The raw data message as a byte array (represented as `number[]` in JS).
   */
  sendMessage(message: number[]): void;

  /**
   * Sets the function to call when a new string message is received.
   * The message is delimited by the current `endOfLine` property.
   * @param receivedAction Function or inline delegate that receives the message string.
   * @returns The SerialPortHelper instance for method chaining.
   */
  onStringReceived(receivedAction: (message: string) => void): SerialPortHelper;

  /**
   * Sets the function to call when a new raw message is received.
   * @param receivedAction Function or inline delegate that receives the raw message as a byte array (`number[]`).
   * @returns The SerialPortHelper instance for method chaining.
   */
  onMessageReceived(receivedAction: (message: number[]) => void): SerialPortHelper;

  /**
   * Sets the function to call when the status of the serial connection changes.
   * @param statusChangeAction Function or inline delegate that receives the new connection status (typically a boolean or status object).
   * @returns The SerialPortHelper instance for method chaining.
   */
  onStatusChanged(statusChangeAction: (status: any) => void): SerialPortHelper;

  /**
   * Disconnects the serial port and clears all registered event handlers.
   */
  reset(): void;

  /**
   * Gets a value indicating whether the serial port is currently connected.
   * @returns `true` if connected; otherwise, `false`.
   */
  get isConnected(): boolean;

  /**
   * Gets or sets the end of line delimiter used in text messaging.
   * @returns The end of line delimiter string.
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
  motor: 10, // 0x0000000A
  doorWindow: 11, // 0x0000000B
  doorLock: 12, // 0x0000000C
  mediaTransmitter: 13, // 0x0000000D
  mediaReceiver: 14 // 0x0000000E
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



/**
 * @description Modules Manager Helper class. Offers methods for filtering, selecting, and operating on a group of home automation modules.
 *              Class instance accessor: `hg.modules`.
 */
class ModulesManager {

  /**
   * Select modules belonging to specified domains.
   * @param domains A string containing comma separated domain names (e.g., 'HomeAutomation.ZWave,HomeAutomation.X10').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // turn off all z-wave lights
   * Modules.InDomain("HomeAutomation.ZWave").OfDeviceType("Light,Dimmer").Off();
   */
  inDomain(domains: string): ModulesManager;

  /**
   * Select modules with specified address.
   * @param addresses A string containing comma separated address values (e.g., 'A2,B5').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // turn on X10 units A2 and B5
   * Modules.WithAddress("A2,B5").On();
   */
  withAddress(addresses: string): ModulesManager;

  /**
   * Select modules matching specified names.
   * @param moduleNames A string containing comma separated module names (e.g., 'Ceiling Light,Kitchen Lamp').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // turn off ceiling light
   * Modules.WithName("Ceiling Light").Off();
   */
  withName(moduleNames: string): ModulesManager;

  /**
   * Select modules of specified device types.
   * @param deviceTypes A string containing comma separated type names (e.g., 'Light,Dimmer,Switch').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // turn on all lights and appliances
   * Modules.OfDeviceType("Light,Dimmer,Switch").On();
   */
  ofDeviceType(deviceTypes: string): ModulesManager;

  /**
   * Select modules included in specified groups.
   * @param groups A string containing comma separated group names (e.g., 'Living Room,Kitchen').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * Modules.InGroup("Living Room,Kitchen").Off();
   */
  inGroup(groups: string): ModulesManager;

  /**
   * Select all modules having specified parameters.
   * @param parameters A string containing comma separated parameter names (e.g., 'Sensor.Temperature,Meter.Watts').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // select all modules with Sensor.Temperature parameter and get the average temperature value
   * var averagetemperature = Modules.WithParameter("Sensor.Temperature").Temperature;
   */
  withParameter(parameters: string): ModulesManager;

  /**
   * Select all modules having specified features.
   * @param features A string containing comma separated feature names (e.g., 'HomeGenie.SecurityAlarm').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // Turn on all Security System sirens
   * Modules.WithFeature("HomeGenie.SecurityAlarm").On();
   */
  withFeature(features: string): ModulesManager;

  /**
   * Select all modules NOT having specified features.
   * @param features A string containing comma separated feature names (e.g., 'EnergyManagement.EnergySavingMode').
   * @returns The ModulesManager instance for method chaining.
   * @example
   * // Turn off all modules not having the "EnergySavingMode" feature
   * Modules.WithoutFeature("EnergyManagement.EnergySavingMode").Off();
   */
  withoutFeature(features: string): ModulesManager;

  /**
   * Iterate through each module in the current selection and pass it to the specified `callback`.
   * To continue the iteration, the callback must return `false`, otherwise `true` to break the loop.
   * @param callback Callback function to call for each iteration. The function should return a boolean to control the loop.
   * @returns The ModulesManager instance for method chaining.
   * @example
   * Modules.WithParameter("Meter.Watts").Each( (module) => {
   *   // ... do something with module ...
   *   return false; // continue iterating
   * });
   */
  each(callback: (module: ModuleHelper) => boolean): ModulesManager;

  /**
   * Returns the module in the current selection as a `ModuleHelper`.
   * If the current selection contains more than one module, the first one will be returned.
   * @returns The ModuleHelper instance for the first selected module.
   * @example
   * var strobeAlarm = Modules.WithName("Strobe Alarm").Get();
   */
  get(): ModuleHelper;

  /**
   * Creates a ModuleHelper instance from a direct Module instance.
   * @param module The Module instance.
   * @returns The ModuleHelper wrapper for the given module instance.
   */
  fromInstance(module: Module): ModuleHelper;

  /**
   * Gets the complete list of all home automation modules in the system.
   * @returns Array of all Module objects.
   */
  get modules(): Array<Module>;

  /**
   * Returns the list of modules currently selected by the filters.
   * @returns Array of selected Module objects.
   */
  get selectedModules(): Array<Module>;

  /**
   * Returns the list of all control groups defined in the system.
   * @returns Array of group names.
   */
  get groups(): Array<string>;

  /**
   * Select an API command to be executed for selected modules. To perform the selected command, a `Submit` or direct command method (`on()`, `off()`, etc.) must be invoked.
   * @param command The API command to be performed (e.g., 'Control.Level').
   * @returns The ModulesManager instance for method chaining.
   */
  command(command: string): ModulesManager;

  /**
   * Used before a command (`Submit`, `On`, `Off`, etc.), it will put a pause after performing the command for each module in the current selection.
   * @param delaySeconds The delay duration in seconds.
   * @returns The ModulesManager instance for method chaining.
   * @example
   * Modules.OfDeviceType("Dimmer").Command("Control.Level").IterationDelay(0.1).submit("40");
   */
  iterationDelay(delaySeconds: number): ModulesManager;

  /**
   * Execute current command on the first selected module and return the response value.
   * @param options Optional slash-separated options to be passed to the command.
   * @returns The command response value as a string.
   */
  getValue(options: string): string;

  /**
   * Submits the command previously specified with `command()` method.
   * @param callback Optional callback that will be called, for each module in the selection, with the result of the issued command.
   * @returns The ModulesManager instance for method chaining.
   */
  submit(callback?: (module: Module, result: any) => void): ModulesManager;

  /**
   * Submits the command previously specified with `command()` method, passing to it the options given by the `options` parameter.
   * @param options A string containing a slash separated list of options to be passed to the selected command.
   * @param callback Optional callback that will be called, for each module in the selection, with the result of the issued command.
   * @returns The ModulesManager instance for method chaining.
   */
  submit(options: string, callback?: (module: Module, result: any) => void): ModulesManager;

  /**
   * Turn on all selected modules (sends 'Control.On' command).
   * @returns The ModulesManager instance for method chaining.
   */
  on(): ModulesManager;

  /**
   * Turn off all selected modules (sends 'Control.Off' command).
   * @returns The ModulesManager instance for method chaining.
   */
  off(): ModulesManager;

  /**
   * Toggle the state of all selected modules (sends 'Control.Toggle' command).
   * @returns The ModulesManager instance for method chaining.
   */
  toggle(): ModulesManager;

  /**
   * Gets or sets the "Status.Level" parameter of selected modules. Setting the value sends the 'Control.Level' command.
   * When reading, the average level value is returned if more than one module is selected.
   * @returns The level (percentage value 0-100).
   * @example
   * Modules.WithFeature("EnergyManagement.EnergySavingMode").level = 40;
   */
  level: number;

  /**
   * Gets "on" status ("Status.Level" > 0).
   * @returns `true` if at least one module in the current selection is on; otherwise, `false`.
   */
  isOn: boolean;

  /**
   * Gets "off" status ("Status.Level" == 0).
   * @returns `true` if at least one module in the current selection is off; otherwise, `false`.
   */
  isOff: boolean;

  /**
   * Gets or sets the "Status.ColorHsb" parameter of selected modules. Setting the value sends the 'Control.ColorHsb' command.
   * When reading, the first module's color is returned.
   * @returns The HSB color string (e.g., "0.3130718,0.986,0.65").
   */
  colorHsb: string

  /**
   * Gets "alarm" status ("Sensor.Alarm" > 0).
   * @returns `true` if at least one module in the current selection is alarmed; otherwise, `false`.
   */
  alarmed: boolean;

  /**
   * Gets "motion detection" status ("Sensor.MotionDetect" > 0).
   * @returns `true` if at least one module in the current selection detected motion; otherwise, `false`.
   */
  motionDetected: boolean;

  /**
   * Gets temperature value ("Sensor.Temperature").
   * @returns The temperature parameter of the selected module (average value is returned when more than one module is selected).
   */
  temperature: number;

  /**
   * Gets luminance value ("Sensor.Luminance").
   * @returns The luminance parameter of the selected module (average value is returned when more than one module is selected).
   */
  luminance: number;

  /**
   * Gets humidity value ("Sensor.Humidity").
   * @returns The humidity parameter of the selected module (average value is returned when more than one module is selected).
   */
  humidity: number;

  /**
   * Creates a copy of the actual modules selection. The copy retains the current filters but is independent of future selections.
   * @returns A new ModulesManager instance with a copy of the current module selection.
   */
  copy(): ModulesManager;

  /**
   * Resets all selection filters, returning the manager to a state that includes all modules.
   * @returns The ModulesManager instance for method chaining.
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

/**
 * @description Scheduler helper. Provides methods for managing and interacting with scheduled tasks and CRON expressions.
 *              Class instance accessor: `hg.scheduler`.
 */
class SchedulerHelper {

  /**
   * Selects the schedule with the specified name for subsequent operations (e.g., get, setSchedule).
   * @param name The name of the schedule item.
   * @returns The SchedulerHelper instance for method chaining.
   */
  withName(name: string): SchedulerHelper;

  /**
   * Gets the selected schedule instance.
   * @returns The SchedulerItem object.
   */
  get(): SchedulerItem;

  /**
   * Adds or modifies the schedule with the previously selected name.
   * @param cronExpression The CRON expression defining the schedule (e.g., "0 7 * * *").
   * @returns The SchedulerHelper instance for method chaining.
   */
  setSchedule(cronExpression: string): SchedulerHelper;

  /**
   * Determines whether the selected schedule is matching at this very moment (current system time).
   * @returns `true` if the selected schedule is matching; otherwise, `false`.
   */
  isScheduling(): boolean;

  /**
   * Determines whether the given CRON expression is matching at this very moment (current system time).
   * @param cronExpression The CRON expression to check.
   * @returns `true` if the given CRON expression is matching; otherwise, `false`.
   */
  isScheduling(cronExpression: string): boolean;

  /**
   * Determines whether the given CRON expression is a matching occurrence at the specified date/time.
   * @param date The Date or string representing the date/time to check (e.g., '2023-10-27T10:00:00Z').
   * @param cronExpression The CRON expression to check.
   * @returns `true` if the given CRON expression is a matching occurrence at the specified date/time; otherwise, `false`.
   */
  isOccurrence(date: Date | string, cronExpression: string): boolean;

  /**
   * Solar Times data. Retrieves sunrise and sunset times for the current date based on HomeGenie's location settings.
   * @param date The Date or string representing the date for which to calculate solar times.
   * @returns The SolarTimes data object.
   */
  solarTimes(date: Date | string): SolarTimes;
}

/**
 * @description Represents a single scheduled task item retrieved from the HomeGenie Scheduler.
 *              Typically returned by `hg.scheduler.withName(...).get()`.
 */
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

/**
 * @description Represents a lightweight, structured reference to a single HomeGenie module or device.
 *              Used primarily in collections like `SchedulerItem.boundModules`.
 */
class ModuleReference {
  /**
   * The domain of the module (e.g., 'HomeAutomation.ZWave', 'Automation.Program').
   */
  domain: string;
  /**
   * The unique address or identifier of the module within its domain (e.g., '3', 'A5', 'MyProgramName').
   */
  address: string;
}


/**
 * @description Settings helper. Provides access to read and write HomeGenie's system or user settings.
 *              Settings are typically stored as ModuleParameters within a dedicated system module.
 *              Class instance accessor: `hg.settings`.
 */
class SettingsHelper {

  /**
   * Gets the system settings parameter with the specified name.
   * This parameter can be read or set (if writable).
   * @param parameter The name of the settings parameter (e.g., 'System.Location.Latitude').
   * @returns The ModuleParameter object for the specified setting.
   */
  parameter(parameter: string): ModuleParameter;

}

/**
 * @description TCP client helper. Provides methods for establishing and managing TCP/IP socket connections.
 *              Class instance accessor: `hg.tcp`.
 */
class TcpClientHelper {

  /**
   * Sets the server address to connect to.
   * @param address Host DNS or IP address (e.g., '192.168.1.10' or 'example.com').
   * @returns The TcpClientHelper instance for method chaining.
   */
  service(address: string): TcpClientHelper;

  /**
   * Connects to the server using the specified port.
   * @param port The port number to connect to.
   * @returns `true` if the connection succeeded; otherwise, `false`.
   */
  connect(port: number): boolean;

  /**
   * Disconnects from the remote host.
   * @returns The TcpClientHelper instance for method chaining.
   */
  disconnect(): TcpClientHelper;

  /**
   * Sends a string message. The message will be appended with the current `endOfLine` delimiter.
   * @param message The string message to send.
   */
  sendMessage(message: string): void;

  /**
   * Sends a raw data message.
   * @param message The raw data message as a byte array (represented as `number[]` in JS).
   */
  sendMessage(message: number[]): void;

  /**
   * Sets the function to call when a new string message is received.
   * The message is delimited by the current `endOfLine` property.
   * @param receivedAction Function or inline delegate that receives the message string.
   * @returns The TcpClientHelper instance for method chaining.
   */
  onStringReceived(receivedAction: (message: string) => void): TcpClientHelper;

  /**
   * Sets the function to call when a new raw message is received.
   * @param receivedAction Function or inline delegate that receives the raw message as a byte array (`number[]`).
   * @returns The TcpClientHelper instance for method chaining.
   */
  onMessageReceived(receivedAction: (message: number[]) => void): TcpClientHelper;

  /**
   * Sets the function to call when the status of the TCP connection changes.
   * @param statusChangeAction Function or inline delegate that receives the new connection status (typically a boolean `true` for connected, `false` for disconnected).
   * @returns The TcpClientHelper instance for method chaining.
   */
  onStatusChanged(statusChangeAction: (status: boolean) => void): TcpClientHelper;

  /**
   * Disconnects and resets all registered event handlers.
   */
  reset(): void;

  /**
   * Gets a value indicating whether the connection to the service is established.
   * @returns `true` if connected; otherwise, `false`.
   */
  get isConnected(): boolean;

  /**
   * Gets or sets the end of line delimiter used in text messaging.
   * @returns The end of line delimiter string.
   */
  endOfLine: string;
}

/**
 * @description UDP client helper. Provides methods for establishing and managing UDP socket communication (sending and receiving).
 *              Class instance accessor: `hg.udp`.
 */
class UdpClientHelper {

  /**
   * Configures the client as a sender to the specified address and port.
   * Note that UDP is connectionless; this primarily sets the default remote endpoint for sending.
   * @param address Remote DNS or IP address (e.g., '192.168.1.10' or 'example.com').
   * @param port The remote port number to send to.
   * @returns The UdpClientHelper instance for method chaining.
   */
  sender(address: string, port: number): UdpClientHelper;

  /**
   * Connects the client as a receiver, binding to the specified local port to listen for incoming packets.
   * @param port The local port number to listen on.
   * @returns `true` if binding to the port succeeded; otherwise, `false`.
   */
  receiver(port: number): boolean;

  /**
   * Disconnects from the remote host and stops listening.
   * @returns The UdpClientHelper instance for method chaining.
   */
  disconnect(): UdpClientHelper;

  /**
   * Sends a string message to the configured remote endpoint.
   * The message will be appended with the current `endOfLine` delimiter.
   * @param message The string message to send.
   */
  sendMessage(message: string): void;

  /**
   * Sends a raw data message to the configured remote endpoint.
   * @param message The raw data message as a byte array (represented as `number[]` in JS).
   */
  sendMessage(message: number[]): void;

  /**
   * Sets the function to call when a new string message is received.
   * The message is delimited by the current `endOfLine` property.
   * @param receivedAction Function or inline delegate that receives the message string.
   * @returns The UdpClientHelper instance for method chaining.
   */
  onStringReceived(receivedAction: (message: string) => void): UdpClientHelper;

  /**
   * Sets the function to call when a new raw message is received.
   * @param receivedAction Function or inline delegate that receives the raw message as a byte array (`number[]`).
   * @returns The UdpClientHelper instance for method chaining.
   */
  onMessageReceived(receivedAction: (message: number[]) => void): UdpClientHelper;

  /**
   * Sets the function to call when the status of the UDP client changes (e.g., binding/unbinding to port).
   * @param statusChangeAction Function or inline delegate that receives the new connection status (typically a boolean `true` for listening, `false` otherwise).
   * @returns The UdpClientHelper instance for method chaining.
   */
  onStatusChanged(statusChangeAction: (status: boolean) => void): UdpClientHelper;

  /**
   * Disconnects the client and resets all registered event handlers.
   */
  reset(): void;

  /**
   * Gets a value indicating whether the client is currently listening for incoming data.
   * @returns `true` if connected/listening; otherwise, `false`.
   */
  get isConnected(): boolean;

  /**
   * Gets or sets the end of line delimiter used in text messaging.
   * @returns The end of line delimiter string.
   */
  endOfLine: string;
}



/**
 * @description MQTT client helper. Provides methods for connecting, subscribing, and publishing to an MQTT broker.
 *              Class instance accessor: `hg.mqtt`.
 */
class MqttClientHelper {

  /**
   * Sets the MQTT server address to use for the connection.
   * @param server The MQTT server address (e.g., 'broker.hivemq.com').
   * @returns The MqttClientHelper instance for method chaining.
   */
  service(server: string): MqttClientHelper;

  /**
   * Connects to the MQTT server using the default port (1883) and the specified client identifier.
   * @param clientId The unique client identifier for the connection.
   * @returns The MqttClientHelper instance for method chaining.
   */
  connect(clientId: string): MqttClientHelper;

  /**
   * Connects to the MQTT server using the specified port and client identifier.
   * @param port The MQTT server port (e.g., 1883 or 8883).
   * @param clientId The unique client identifier.
   * @returns The MqttClientHelper instance for method chaining.
   */
  connect(port: number, clientId: string): MqttClientHelper;

  /**
   * Connects to the MQTT server using the specified port, client identifier, and connection status callback.
   * @param port The MQTT server port.
   * @param clientId The unique client identifier.
   * @param callback Optional callback invoked when the connection status changed (true if connected, false otherwise).
   * @returns The MqttClientHelper instance for method chaining.
   */
  connect(port: number, clientId: string, callback: (connected: boolean) => void): MqttClientHelper;

  /**
   * Connects to the MQTT server allowing for advanced client options setup (e.g., Will Message, Keep Alive).
   * @param port The MQTT server port.
   * @param clientId The unique client identifier.
   * @param clientOptionsCallback Callback invoked before the connection is established to allow setting advanced connection options.
   * @param callback Optional callback invoked when the connection status changed.
   * @returns The MqttClientHelper instance for method chaining.
   */
  connect(port: number, clientId: string, clientOptionsCallback: (optionsBuilder: MqttClientOptionsBuilder) => void, callback?: (connected: boolean) => void): MqttClientHelper;

  /**
   * Disconnects from the MQTT server.
   * @returns The MqttClientHelper instance for method chaining.
   */
  disconnect(): MqttClientHelper;

  /**
   * Subscribes to the specified MQTT topic.
   * @param topic Topic name (can include wildcards).
   * @param callback Callback for receiving messages published to the subscribed topic.
   * @returns The MqttClientHelper instance for method chaining.
   */
  subscribe(topic: string, callback: (topic: string, message: number[]) => void): MqttClientHelper;

  /**
   * Unsubscribes from the specified MQTT topic.
   * @param topic Topic name.
   * @returns The MqttClientHelper instance for method chaining.
   */
  unsubscribe(topic: string): MqttClientHelper;

  /**
   * Publish a message (string) to the specified topic.
   * @param topic Topic name.
   * @param message Message text.
   * @returns The MqttClientHelper instance for method chaining.
   */
  publish(topic: string, message: string): MqttClientHelper;

  /**
   * Publish a message (raw data) to the specified topic.
   * @param topic Topic name.
   * @param message Message as a byte array (`number[]`).
   * @returns The MqttClientHelper instance for method chaining.
   */
  publish(topic: string, message: number[]): MqttClientHelper;

  /**
   * Publish a message using advanced options (e.g., QoS, Retain).
   * @param applicationMessage MqttApplicationMessage instance for advanced publishing configuration.
   * @returns The MqttClientHelper instance for method chaining.
   */
  publish(applicationMessage: MqttApplicationMessage): MqttClientHelper;

  /**
   * Configures the client to connect over WebSocket instead of standard TCP.
   * @param useWebSocket `true` to use WebSockets.
   * @returns The MqttClientHelper instance for method chaining.
   */
  usingWebSockets(useWebSocket: boolean): MqttClientHelper;

  /**
   * Sets the username and password for authentication when connecting.
   * @param user Username.
   * @param pass Password.
   * @returns The MqttClientHelper instance for method chaining.
   */
  withCredentials(user: string, pass: string): MqttClientHelper;

  /**
   * Sets whether to connect using TLS/SSL or not.
   * @param useTls `true` to use TLS/SSL.
   * @returns The MqttClientHelper instance for method chaining.
   */
  withTls(useTls: boolean): MqttClientHelper;

  /**
   * Resets all current settings (server, credentials, TLS, WebSockets) on the MqttClientHelper instance to their default values.
   */
  reset(): void;
}

/**
 * @description Builder class for creating and configuring MQTT client options.
 *              This is typically accessed via a callback parameter in the `hg.mqtt.connect(...)` overloads.
 */
class MqttClientOptionsBuilder {
  /**
   * Finalizes the configuration and returns the MqttClientOptions object.
   * @returns The fully configured MQTT client options.
   * @throws {Error} if a required channel (TCP or WebSocket) is not set.
   */
  build(): MqttClientOptions;

  /**
   * Sets the address family for TCP connections (e.g., AddressFamily.Unspecified, AddressFamily.InterNetwork).
   * @param addressFamily The AddressFamily value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withAddressFamily(addressFamily: any): MqttClientOptionsBuilder;

  /**
   * Clean session is used in MQTT versions below 5.0.0. It is the same as setting "CleanStart".
   * @param value If true, the session is cleared upon connection.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withCleanSession(value?: boolean): MqttClientOptionsBuilder;

  /**
   * Clean start is used in MQTT versions 5.0.0 and higher. It is the same as setting "CleanSession".
   * @param value If true, the session is cleared upon connection.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withCleanStart(value?: boolean): MqttClientOptionsBuilder;

  /**
   * Sets the client identifier used to connect to the MQTT broker.
   * @param value The client ID string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withClientId(value: string): MqttClientOptionsBuilder;

  /**
   * Configures connection options (TCP or WebSocket, with or without TLS) from a URI string.
   * @param uri The connection URI string or Uri object (e.g., 'mqtt://broker.example.com').
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withConnectionUri(uri: string | Uri): MqttClientOptionsBuilder;

  /**
   * Sets the credentials (username and password) for basic authentication.
   * @param username The username.
   * @param password The password string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withCredentials(username: string, password: string): MqttClientOptionsBuilder;

  /**
   * Sets the credentials (username and password) for basic authentication.
   * @param username The username.
   * @param password The password as a byte array (represented as `number[]` in JS).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withCredentials(username: string, password?: number[]): MqttClientOptionsBuilder;

  /**
   * Sets the credentials using a custom provider interface.
   * @param credentials The IMqttClientCredentialsProvider instance.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withCredentials(credentials: IMqttClientCredentialsProvider): MqttClientOptionsBuilder;

  /**
   * Sets the remote endpoint using a generic EndPoint object.
   * @param endPoint The EndPoint instance.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withEndPoint(endPoint: EndPoint): MqttClientOptionsBuilder;

  /**
   * Configures enhanced authentication (used in MQTT v5) with a method and optional data.
   * @param method The authentication method string.
   * @param data Optional authentication data as a byte array (`number[]`).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withEnhancedAuthentication(method: string, data?: number[]): MqttClientOptionsBuilder;

  /**
   * Sets a handler for enhanced authentication challenges.
   * @param handler The IMqttEnhancedAuthenticationHandler instance.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withEnhancedAuthenticationHandler(handler: IMqttEnhancedAuthenticationHandler): MqttClientOptionsBuilder;

  /**
   * Sets the Keep Alive period, during which the client sends PING requests if no other messages are exchanged.
   * @param value The TimeSpan for the period.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withKeepAlivePeriod(value: TimeSpan): MqttClientOptionsBuilder;

  /**
   * Sets the Maximum Packet Size property (used in MQTT v5).
   * @param maximumPacketSize The maximum size in bytes.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withMaximumPacketSize(maximumPacketSize: number): MqttClientOptionsBuilder;

  /**
   * Disables the Keep Alive mechanism (sets period to zero).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withNoKeepAlive(): MqttClientOptionsBuilder;

  /**
   * Disables MQTT packet fragmentation. This is required by some brokers (like AWS) that do not support fragmented packets.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withoutPacketFragmentation(): MqttClientOptionsBuilder;

  /**
   * Sets the protocol type for TCP connections (e.g., ProtocolType.Tcp).
   * @param protocolType The ProtocolType value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withProtocolType(protocolType: any): MqttClientOptionsBuilder;

  /**
   * Sets the MQTT protocol version to use.
   * @param value The MqttProtocolVersion enumeration value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   * @throws {Error} if the protocol version is MqttProtocolVersion.Unknown.
   */
  withProtocolVersion(value: MqttProtocolVersion): MqttClientOptionsBuilder;

  /**
   * Sets the Receive Maximum property (used in MQTT v5).
   * @param receiveMaximum The maximum number of QoS 1 or QoS 2 publications the client is willing to process concurrently.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withReceiveMaximum(receiveMaximum: number): MqttClientOptionsBuilder;

  /**
   * Sets whether the client requests Problem Information from the broker (used in MQTT v5).
   * @param requestProblemInformation If true, requests problem information.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withRequestProblemInformation(requestProblemInformation?: boolean): MqttClientOptionsBuilder;

  /**
   * Sets whether the client requests Response Information from the broker (used in MQTT v5).
   * @param requestResponseInformation If true, requests response information.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withRequestResponseInformation(requestResponseInformation?: boolean): MqttClientOptionsBuilder;

  /**
   * Sets the Session Expiry Interval in seconds (used in MQTT v5).
   * @param sessionExpiryInterval The interval in seconds.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withSessionExpiryInterval(sessionExpiryInterval: number): MqttClientOptionsBuilder;

  /**
   * Configures a standard TCP connection to a server by host name and port.
   * @param host The host name or IP address.
   * @param port The port number. If null or 0, the default MQTT/MQTTS port is used based on TLS settings.
   * @param addressFamily The AddressFamily (e.g., AddressFamily.Unspecified).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTcpServer(host: string, port?: number, addressFamily?: any): MqttClientOptionsBuilder;

  /**
   * Configures a TCP connection using a custom options builder callback.
   * @param optionsBuilder The action to configure MqttClientTcpOptions.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTcpServer(optionsBuilder: (options: MqttClientTcpOptions) => void): MqttClientOptionsBuilder;

  /**
   * Sets the general timeout for socket-level and internal operations.
   * @param value The TimeSpan for the timeout.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTimeout(value: TimeSpan): MqttClientOptionsBuilder;

  /**
   * Sets the TLS/SSL options for the connection.
   * @param tlsOptions The MqttClientTlsOptions object.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTlsOptions(tlsOptions: MqttClientTlsOptions): MqttClientOptionsBuilder;

  /**
   * Configures the TLS/SSL options using a custom builder callback.
   * @param configure The action to configure MqttClientTlsOptionsBuilder.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTlsOptions(configure: (builder: MqttClientTlsOptionsBuilder) => void): MqttClientOptionsBuilder;

  /**
   * Sets the Topic Alias Maximum property (used in MQTT v5).
   * @param topicAliasMaximum The maximum topic alias.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTopicAliasMaximum(topicAliasMaximum: number): MqttClientOptionsBuilder;

  /**
   * If set to true, the client attempts to indicate to the remote broker that it is a bridge.
   * This may help with loop detection and retained message propagation on supporting brokers.
   * @param value If true, sets the Try Private flag.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withTryPrivate(value?: boolean): MqttClientOptionsBuilder;

  /**
   * Adds a user property to the CONNECT packet (used in MQTT v5).
   * @param name The property name.
   * @param value The property value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withUserProperty(name: string, value: string): MqttClientOptionsBuilder;

  /**
   * Configures a WebSocket connection using a custom options builder callback.
   * @param configure The action to configure MqttClientWebSocketOptionsBuilder.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWebSocketServer(configure: (builder: MqttClientWebSocketOptionsBuilder) => void): MqttClientOptionsBuilder;

  /**
   * Sets the Content Type of the Will Message (used in MQTT v5).
   * @param willContentType The content type string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillContentType(willContentType: string): MqttClientOptionsBuilder;

  /**
   * Sets the Correlation Data of the Will Message (used in MQTT v5).
   * @param willCorrelationData The correlation data as a byte array (`number[]`).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillCorrelationData(willCorrelationData: number[]): MqttClientOptionsBuilder;

  /**
   * Sets the Will Delay Interval in seconds (used in MQTT v5).
   * @param willDelayInterval The delay interval in seconds.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillDelayInterval(willDelayInterval: number): MqttClientOptionsBuilder;

  /**
   * Sets the Message Expiry Interval of the Will Message in seconds (used in MQTT v5).
   * @param willMessageExpiryInterval The expiry interval in seconds.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillMessageExpiryInterval(willMessageExpiryInterval: number): MqttClientOptionsBuilder;

  /**
   * Sets the payload of the Will Message.
   * @param willPayload The payload as a byte array (`number[]`).
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillPayload(willPayload: number[]): MqttClientOptionsBuilder;

  /**
   * Sets the payload of the Will Message from a string (encoded as UTF-8).
   * @param willPayload The payload string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillPayload(willPayload: string): MqttClientOptionsBuilder;

  /**
   * Sets the Payload Format Indicator of the Will Message (used in MQTT v5).
   * @param willPayloadFormatIndicator The MqttPayloadFormatIndicator enumeration value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillPayloadFormatIndicator(willPayloadFormatIndicator: any): MqttClientOptionsBuilder;

  /**
   * Sets the Quality of Service level of the Will Message.
   * @param willQualityOfServiceLevel The MqttQualityOfServiceLevel enumeration value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillQualityOfServiceLevel(willQualityOfServiceLevel: any): MqttClientOptionsBuilder;

  /**
   * Sets the Response Topic of the Will Message (used in MQTT v5).
   * @param willResponseTopic The response topic string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillResponseTopic(willResponseTopic: string): MqttClientOptionsBuilder;

  /**
   * Sets the Retain flag of the Will Message.
   * @param willRetain If true, the Will Message is retained by the broker.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillRetain(willRetain?: boolean): MqttClientOptionsBuilder;

  /**
   * Sets the topic of the Will Message.
   * @param willTopic The Will Message topic string.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillTopic(willTopic: string): MqttClientOptionsBuilder;

  /**
   * Adds a user property to the Will Message (used in MQTT v5).
   * @param name The property name.
   * @param value The property value.
   * @returns The MqttClientOptionsBuilder instance for method chaining.
   */
  withWillUserProperty(name: string, value: string): MqttClientOptionsBuilder;
}
/**
 * @description Represents an MQTT application message containing a topic, payload, and optional control properties.
 *              This class is used for publishing messages to the broker.
 */
class MqttApplicationMessage {
  /**
   * Gets or sets the content type.
   * The content type must be a UTF-8 encoded string (MQTT 5 feature).
   */
  contentType: string;

  /**
   * Gets or sets the correlation data (MQTT 5 feature).
   * Used for implementing request/response patterns.
   * Represented as a byte array (`number[]` in JS).
   */
  correlationData: number[];

  /**
   * If the DUP flag is set to `true`, it indicates that this might be a re-delivery of an earlier attempt to send the packet.
   */
  dup: boolean;

  /**
   * Gets or sets the message expiry interval in seconds (MQTT 5 feature).
   * This interval defines the period of time that the broker stores the message for any matching offline subscribers.
   */
  messageExpiryInterval: number;

  /**
   * Gets or sets the message payload.
   * Represented as a byte array (`number[]` in JS).
   */
  payload: number[];

  /**
   * Gets or sets the payload format indicator (MQTT 5 feature).
   * A value of 0 indicates an “unspecified byte stream”; a value of 1 indicates a "UTF-8 encoded payload".
   */
  payloadFormatIndicator: any;

  /**
   * Gets or sets the Quality of Service (QoS) level.
   * Defines the guarantee of delivery (0: At most once, 1: At least once, 2: Exactly once).
   */
  qualityOfServiceLevel: any;

  /**
   * Gets or sets the response topic (MQTT 5 feature).
   * Allows the implementation of a request/response pattern between clients.
   */
  responseTopic: string;

  /**
   * Gets or sets a value indicating whether the message should be retained by the broker or not.
   * The broker stores the last retained message for that topic.
   */
  retain: boolean;

  /**
   * Gets or sets the list of subscription identifiers (MQTT 5 feature).
   * The broker returns this identifier with the PUBLISH packet to the client.
   */
  subscriptionIdentifiers: number[];

  /**
   * Gets or sets the MQTT topic.
   * The topic consists of one or more levels separated by a forward slash.
   */
  topic: string;

  /**
   * Gets or sets the topic alias (MQTT 5 feature).
   * A mechanism for reducing the size of published packets by reducing the size of the topic field.
   * Represented as an unsigned 16-bit integer (`number` in JS).
   */
  topicAlias: number;

  /**
   * Gets or sets the list of user properties (MQTT 5 feature).
   * Basic UTF-8 string key-value pairs that can be appended to the packet (similar to HTTP headers).
   */
  userProperties: any[];
}

/**
 * @description Represents the network configuration for a KNX connection, typically used for tunneling.
 */
class KnxEndPoint {
  /**
   * The local IP address to use for the connection.
   */
  localIp: string;
  /**
   * The local port number.
   */
  localPort: number;
  /**
   * The remote IP address of the KNX IP interface/router.
   */
  remoteIp: string;
  /**
   * The remote port number (e.g., 3671 for KNXnet/IP).
   */
  remotePort: number;
}

/**
 * @description KNX client helper. Provides methods for establishing KNX/IP connections (Tunneling or Routing)
 *              and sending/receiving data to KNX group addresses.
 *              Class instance accessor: `hg.knx`.
 */
class KnxClientHelper {

  /**
   * Sets the Action Message code for filtering received messages (optional).
   * @param actionMessageCode The message code string.
   * @returns The KnxClientHelper instance for method chaining.
   */
  actionMessageCode(actionMessageCode: string): KnxClientHelper;

  // Overload 1: Multicast (Host only - Routing)
  /**
   * Sets the remote endpoint to connect to using Multicast (KNXnet/IP Routing).
   * @param host The remote IP address (often the multicast address 224.0.23.12 or the interface IP).
   * @returns The KnxClientHelper instance for method chaining.
   */
  endPoint(host: string): KnxClientHelper;

  // Overload 2: Multicast (Port only)
  /**
   * Sets the remote endpoint port to connect to (KNXnet/IP Routing/Tunneling).
   * @param port The endpoint port (e.g., 3671).
   * @returns The KnxClientHelper instance for method chaining.
   */
  endPoint(port: number): KnxClientHelper;

  // Overload 3: Multicast (Host + Port)
  /**
   * Sets the remote endpoint to connect to (Host and Port).
   * @param host The remote IP address.
   * @param port The remote port.
   * @returns The KnxClientHelper instance for method chaining.
   */
  endPoint(host: string, port: number): KnxClientHelper;

  // Overload 4: Tunneling (Local IP/Port + Remote IP/Port)
  /**
   * Sets the endpoint to connect to using Tunneling, requiring both local and remote addresses.
   * @param localIp The local IP address.
   * @param localPort The local port.
   * @param remoteIp The remote IP address of the KNX IP interface.
   * @param remotePort The remote port.
   * @returns The KnxClientHelper instance for method chaining.
   */
  endPoint(localIp: string, localPort: number, remoteIp: string, remotePort: number): KnxClientHelper;

  /**
   * Establishes the connection to the KNX/IP interface using the previously set endpoint.
   * @returns The KnxClientHelper instance for method chaining.
   */
  connect(): KnxClientHelper;

  /**
   * Disconnects from the remote host.
   * @returns The KnxClientHelper instance for method chaining.
   */
  disconnect(): KnxClientHelper;

  // Overload 1: Action (Boolean)
  /**
   * Sends a boolean action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The boolean action value (`true` or `false`).
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: boolean): KnxClientHelper;

  // Overload 2: Action (Integer)
  /**
   * Sends an integer action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The integer action value.
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: number): KnxClientHelper;

  // Overload 3: Action (Byte / number)
  /**
   * Sends a byte (number 0-255) action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The byte action value.
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: number): KnxClientHelper;

  // Overload 4: Action (Byte Array / number[])
  /**
   * Sends a raw byte array action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The byte array action value (`number[]` in JS).
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: number[]): KnxClientHelper;

  // Overload 5: Action (String)
  /**
   * Sends a string action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The string action value.
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: string): KnxClientHelper;

  // Overload 6: Action (Generic Object)
  /**
   * Sends a generic object action value to the specified KNX group address.
   * @param address The KNX group address (e.g., '1/1/1').
   * @param data The generic object action value.
   * @returns The KnxClientHelper instance for method chaining.
   */
  action(address: string, data: any): KnxClientHelper;

  /**
   * Requests the current status of a KNX group address (GroupValueRead).
   * @param address The KNX group address (e.g., '1/1/1').
   * @returns The KnxClientHelper instance for method chaining.
   */
  requestStatus(address: string): KnxClientHelper;

  /**
   * Converts a value to a KNX Data Point Type (DPT) byte array.
   * @param type The DPT type string (e.g., 'DPT-1', 'DPT-9.001').
   * @param data The input value (e.g., boolean, number, string).
   * @returns The KNX DPT byte array (`number[]`).
   */
  convertToDpt(type: string, data: any): number[];

  /**
   * Converts a KNX Data Point Type (DPT) byte array to a JavaScript object type (e.g., boolean, number, string).
   * @param type The DPT type string (e.g., 'DPT-1', 'DPT-9.001').
   * @param data The DPT byte array (`number[]` or an object containing the data).
   * @returns The converted object value.
   */
  convertFromDpt(type: string, data: any): any;

  /**
   * Sets the function to call when the connection status changes.
   * @param statusChangeAction Function or inline delegate that receives the new connection status.
   * @returns The KnxClientHelper instance for method chaining.
   */
  onStatusChanged(statusChangeAction: (status: boolean) => void): KnxClientHelper;

  /**
   * Sets the function to call when a new group address event (GroupValueWrite/Response) is received.
   * @param eventAction Function or inline delegate that receives the group address and the state value (as a string).
   * @returns The KnxClientHelper instance for method chaining.
   */
  onEventReceived(eventAction: (address: string, state: string) => void): KnxClientHelper;

  /**
   * Sets the function to call when a new status value is received for a group address (in response to a RequestStatus).
   * @param statusAction Function or inline delegate that receives the group address and the status value (as a string).
   * @returns The KnxClientHelper instance for method chaining.
   */
  onStatusReceived(statusAction: (address: string, state: string) => void): KnxClientHelper;

  /**
   * Resets the KNX client helper, disconnecting and clearing all event handlers and endpoint settings.
   */
  reset(): void;
}


/**
 * @description Data and database Helper class. Provides methods for accessing program-specific data storage,
 *              using the LiteDB embedded database, and managing system backups.
 *              Class instance accessor: `hg.data`.
 */
class DataHelper {

  /**
   * Gets the file system path of the program's data folder.
   * @param fixedName Optional: if provided, gets a shareable folder with the given fixed name instead of the program's unique folder.
   * @returns The absolute path of the data folder as a string.
   */
  getFolder(fixedName?: string): string;

  /**
   * Opens and gets a LiteDatabase instance for the specified file name.
   * LiteDB is a simple, fast NoSQL embedded database.
   * @param fileName The database file name (will be stored in the program's data folder).
   * @returns The LiteDatabase instance. See https://www.litedb.org for documentation.
   * @example
   * var db = hg.data.liteDb("my_data.db");
   */
  liteDb(fileName: string): any; // LiteDatabase;

  /**
   * Sets an additional file or folder to be included in the system backup file.
   * This ensures that program-specific data persists across system restores.
   * @param path The relative or absolute path of the file/folder to add.
   * @returns `true` if the path was successfully added; otherwise, `false`.
   */
  addToSystemBackup(path: string): boolean;

  /**
   * Removes an additional file or folder from the system backup file list.
   * @param path The path of the file/folder to remove.
   * @returns `true` if the path was successfully removed; otherwise, `false`.
   */
  removeFromSystemBackup(path: string): boolean;
}
