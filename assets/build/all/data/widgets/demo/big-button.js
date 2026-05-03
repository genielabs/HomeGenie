class LightbulbWidget extends ControllerInstance {
  settings = {
    moduleSelect: {
        fieldFilter: 'Status.Level',
        typeFilter: 'Light,Dimmer,Color,Switch'
    },
    sizeOptions: ['small', 'medium'],
    defaultSize: 'small'
  };

  // Internal State
  state = { isOn: false };

  onCreate() {
    const self = this;

    const viewContext = {
        toggle: () => self.toggle(),
        isOn: () => self.state.isOn
    };
    this.declare(viewContext);

    if (!this.boundModule) {
        this.model().name = 'No module bound';
        this.model().status_text = 'N/A';
        this.model().icon = 'lightbulb'; // Default icon text
        return;
    }

    this.model().name = this.boundModule.name;

    const field = this.boundModule.field('Status.Level');
    if (field) {
        this.subscribe(field, (f) => {
            // STORE
            const val = f.decimalValue;
            self.state.isOn = val > 0;
            // RENDER
            self.refreshUI();
        });

        // Initial Store & Render
        const val = field.decimalValue;
        this.state.isOn = val > 0;
        this.refreshUI();
    }
  }

  // Parameter-less render function
  refreshUI() {
    this.model().status_text = this.state.isOn ? 'On' : 'Off';
    // The icon text remains 'lightbulb', its appearance is controlled by CSS classes
  }

  toggle() {
    if (this.boundModule) {
      const cmd = this.state.isOn ? 'Control.Off' : 'Control.On';
      this.boundModule.control(cmd);
    } else {
      this.configure();
    }
  }
}
