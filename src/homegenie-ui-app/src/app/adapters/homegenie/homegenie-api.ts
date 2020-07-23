export class Group {
  Name: string;
  Wallpaper: string;
  Modules: Array<{ Domain: string; Address: string }>;
}

export class Module {
  Domain: string;
  Address: string;
  Name: string;
  DeviceType: string;
  Description: string;
  Properties: Array<ModuleParameter>;
}

export class ModuleParameter {
  Name: string;
  Value: any;
  UpdateTime: number;
}

export class Program {
  // TODO: ...
  Address: string;
  Name: string;
  Description: string;
  IsEnabled: boolean;
  Features: ProgramFeature[];
}
export class ProgramFeature {
  Description: string;
  FieldType: string;
  ForDomains: string;
  ForTypes: string;
  Property: string;
}

export class HomegenieApi {
  static Config = {
    Groups: {
      // Get the list of groups
      List: 'HomeAutomation.HomeGenie/Config/Groups.List'
    },
    Interfaces: {
      // Get the list of available MIG interfaces
      List: 'HomeAutomation.HomeGenie/Config/Interfaces.ListConfig',
      Configure: {
        Hardware: {
          // Get the list of available serial ports
          SerialPorts: 'HomeAutomation.HomeGenie/Config/Interfaces.Configure/Hardware.SerialPorts'
        }
      }
    },
    Modules: {
      // Get the list of modules
      List: 'HomeAutomation.HomeGenie/Config/Modules.List'
    },
    WebSocket: {
      // Get autorization token to connect to WebSocket service
      GetToken: 'HomeAutomation.HomeGenie/Config/WebSocket.GetToken'
    },
    Command: (command, options) => `HomeAutomation.HomeGenie/Config/${command}/${options}`
  };
  static Automation = {
    Programs: {
      // Get the list of programs
      List: 'HomeAutomation.HomeGenie/Automation/Programs.List'
    },
    Command: (command, options) => `HomeAutomation.HomeGenie/Automation/${command}/${options}`
  };
}
