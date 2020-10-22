import {Module as HguiModule, ModuleField} from 'src/app/services/hgui/module';

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
  Description?: string;
  FieldType?: string;
  UpdateTime: number;
}

export class Program {
  // TODO: ...
  Domain: string;
  Address: string;
  Name: string;
  Description: string;
  Group: string;
  IsEnabled: boolean;
  IsRunning: boolean;
  Features: ProgramFeature[];
  Type: string;
  ScriptSetup: string;
  ScriptSource: string;
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
      },
      Enable: (domain: string) =>
        `MIGService.Interfaces/${domain}/IsEnabled.Set/1`,
      Disable: (domain: string) =>
        `MIGService.Interfaces/${domain}/IsEnabled.Set/0`,
    },
    Modules: {
      // Get the list of modules
      List: 'HomeAutomation.HomeGenie/Config/Modules.List',
      ParameterGet: (module: HguiModule, parameter: string) =>
        `HomeAutomation.HomeGenie/Config/Modules.ParameterGet/${module.id}/${parameter}`,
      ParameterSet: (module: HguiModule, parameter?: string, value?: any) =>
        `HomeAutomation.HomeGenie/Config/Modules.ParameterSet/${module.id}/${parameter || ''}/${value || ''}`,
      // Get parameter statistics
      StatisticsGet: (address: string, parameter: string) =>
        `HomeAutomation.HomeGenie/Config/Modules.StatisticsGet/${address}/${parameter}`
    },
    WebSocket: {
      // Get autorization token to connect to WebSocket service
      GetToken: 'HomeAutomation.HomeGenie/Config/WebSocket.GetToken'
    }
  };
  static Automation = {
    Programs: {
      // Get the list of programs
      List: 'HomeAutomation.HomeGenie/Automation/Programs.List'
    },
    Command: (command: string, options: string) =>
      `HomeAutomation.HomeGenie/Automation/${command}/${options}`
  };
}
