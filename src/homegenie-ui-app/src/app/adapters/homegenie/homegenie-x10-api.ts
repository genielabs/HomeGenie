export class HomegenieX10Api {
  static Options = {
    Get: {
      Port: 'MIGService.Interfaces/HomeAutomation.X10/Options.Get/Port',
      HouseCodes: 'MIGService.Interfaces/HomeAutomation.X10/Options.Get/HouseCodes'
    },
    Set: {
      Port: 'MIGService.Interfaces/HomeAutomation.X10/Options.Set/Port/{{portName}}',
      HouseCodes: 'MIGService.Interfaces/HomeAutomation.X10/Options.Set/HouseCodes/{{houseCodes}}'
    }
  };
}
