# HomeGenie scripting API reference index

Generated from Doxygen XML. These are backend automation APIs, not widget JavaScript APIs or a device-command inventory.

Load only the required topic with the HomeGenie prompt composer. Links alone do not load a reference; dynamic model-driven retrieval is not implemented.

Keep the shared automation.md rules loaded. Use the host-provided Program, Modules, Events, Scheduler, Data, Settings, Net and Api objects; do not construct helpers or ProgramHelperBase.

- [Module selection and values](api-modules.md): ModulesManager, ModuleHelper, Module, ModuleParameter, ValueStatistics, StatValue.
  Include: `#use-kb homegenie-core/references/api-modules.md`
- [Program lifecycle and configuration](api-programs.md): ProgramHelper, ProgramHelperBase.
  Include: `#use-kb homegenie-core/references/api-programs.md`
- [Events and scheduler](api-events.md): EventsHelper, SchedulerHelper.
  Include: `#use-kb homegenie-core/references/api-events.md`
- [Data, settings and store](api-storage.md): DataHelper, SettingsHelper, StoreHelper.
  Include: `#use-kb homegenie-core/references/api-storage.md`
- [HTTP and local API calls](api-network.md): NetHelper, ApiHelper.
  Include: `#use-kb homegenie-core/references/api-network.md`
- [MQTT, KNX, TCP, UDP and serial](api-protocols.md): MqttClientHelper, KnxClientHelper, TcpClientHelper, UdpClientHelper, SerialPortHelper.
  Include: `#use-kb homegenie-core/references/api-protocols.md`
