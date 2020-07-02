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
  Properties: Array<ModuleParameter>;
}

export class ModuleParameter {
  Name: string;
  Value: any;
  UpdateTime: number;
}

export class Program {
  // TODO: ...
  IsEnabled: boolean;
  Features: any[];
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

export class ZWaveApi {
  static Master = {
    Controller: {
      Discovery: 'HomeAutomation.ZWave/1/Controller.Discovery/',
      NodeAdd: 'HomeAutomation.ZWave/1/Controller.NodeAdd',
      NodeRemove: 'HomeAutomation.ZWave/1/Controller.NodeRemove',
    }
  };
}
