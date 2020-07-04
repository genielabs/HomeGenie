export class ZWaveApi {
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
  static Property = {
    Basic:
      'ZWaveNode.Basic',
    SwitchBinary:
      'ZWaveNode.SwitchBinary',
    SwitchMultilevel:
      'ZWaveNode.SwitchMultilevel',
    WakeUpInterval:
      'ZWaveNode.WakeUpInterval',
    Battery:
      'ZWaveNode.Battery',
    MultiInstance:
      'ZWaveNode.MultiInstance',
    Associations:
      'ZWaveNode.Associations',
    ConfigVariables:
      'ZWaveNode.Variables',
    NodeInfo:
      'ZWaveNode.NodeInfo',
    RoutingInfo:
      'ZWaveNode.RoutingInfo',
    ManufacturerSpecific:
      'ZWaveNode.ManufacturerSpecific',
    VersionReport:
      'ZWaveNode.VersionReport'
  };
  static Classes = {
    '20': 'Basic',
    '22': 'Application Status',
    '25': 'Switch Binary',
    '26': 'Switch Multi Level',
    '27': 'Switch All',
    '2B': 'Scene Activation',
    '30': 'Sensor Binary',
    '31': 'Sensor Multi Level',
    '32': 'Meter',
    '38': 'Thermostat Heating',
    '40': 'Thermostat Mode',
    '42': 'Thermostat Operating State',
    '43': 'Thermostat Set Point',
    '44': 'Thermostat Fan Mode',
    '45': 'Thermostat Fan State',
    '47': 'Thermostat Set Back',
    '60': 'Multi Instance',
    '62': 'Door Lock',
    '63': 'User Code',
    '70': 'Configuration',
    '71': 'Alarm',
    '72': 'Manufacturer Specific',
    '77': 'Node Naming',
    '7A': 'Firmware Update',
    '80': 'Battery',
    '82': 'Hail',
    '84': 'Wake Up',
    '85': 'Association',
    '86': 'Version',
    '98': 'Security',
    '9C': 'Sensor Alarm',
    '9D': 'Silence Alarm'
  };
}
