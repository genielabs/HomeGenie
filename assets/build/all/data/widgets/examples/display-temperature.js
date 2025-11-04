const fieldName = 'Sensor.Temperature';

class DisplayTemperature extends ControllerInstance {
  // Widget Settings
  settings = {
    moduleSelect: {
      // In the widget settings dialog
      // show only modules with this field
      fieldFilter: 'Sensor.Temperature'
    }
  };

  onCreate() {

    const viewModel = this.model();
    viewModel.unit = utils.preferences.units.temperature;

    // the bound module selected by the user
    const bm = this.boundModule;

    // the temperature field of the bound module
    const field = bm?.field(fieldName);

    if (bm && field) {

      const formatValue = utils.format.fieldValue;
      viewModel.title = bm.name;
      viewModel.temperature = formatValue(field);

      // subscribe to field events to update temperature value as it changes
      this.subscribe(field, () => {
        viewModel.temperature = formatValue(field);
      });

    }

  }

}
