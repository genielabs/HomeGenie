// set the mode of all selected thermostat modules to 'Heat'
$$.boundModules
  .command('Thermostat.ModeSet')
  .set('Heat');
