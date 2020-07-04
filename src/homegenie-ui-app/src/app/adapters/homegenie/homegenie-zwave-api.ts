export class HomegenieZwaveApi {
  static Options = {
    Get: {
      Port: 'MIGService.Interfaces/HomeAutomation.ZWave/Options.Get/Port'
    },
    Set: {
      Port: 'MIGService.Interfaces/HomeAutomation.ZWave/Options.Set/Port'
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
}
