export class HomegenieZwaveApi {
  static Options = {
    Get: {
      Port: 'MIGService.Interfaces/HomeAutomation.ZWave/Options.Get/Port'
    },
    Set: {
      Port: 'MIGService.Interfaces/HomeAutomation.ZWave/Options.Set/Port/{{portName}}'
    }
  };
  static Master = {
    Controller: {
      Discovery: 'HomeAutomation.ZWave/1/Controller.Discovery',
      NodeAdd: 'HomeAutomation.ZWave/1/Controller.NodeAdd',
      NodeRemove: 'HomeAutomation.ZWave/1/Controller.NodeRemove'
    },
    Db: {
      GetDevice: 'HomeAutomation.ZWave/1/Db.GetDevice'
    }
  };
  static Node = {
    NodeInfo: {
      Get: 'HomeAutomation.ZWave/{{nodeId}}/Db.GetDevice'
    }
  };
  static Associations = {
    Get: '{{nodeId}}/Association.Get/{{groupId}}',
    Set: '{{nodeId}}/Association.Set/{{groupId}}/{{groupNode}}',
    Remove: '{{nodeId}}/Association.Remove/{{groupId}}/{{groupNode}}'
  };
  static Config = {
    Parameter: {
      Get: '{{nodeId}}/Config.ParameterGet/{{parameterId}}',
      Set: '{{nodeId}}/Config.ParameterSet/{{parameterId}}/{{parameterValue}}'
    }
  };
}
