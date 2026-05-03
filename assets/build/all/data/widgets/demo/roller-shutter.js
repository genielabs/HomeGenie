class RollerShutterWidget extends ControllerInstance {
  settings = {
    moduleSelect: {
      typeFilter: 'Shutter'
    },
    sizeOptions: ['medium', 'big'],
    defaultSize: 'medium'
  };

  // Internal state
  state = {
    level: 0.0, // 0.0 (Closed) to 1.0 (Open)
    levelPercent: 0, // 0-100 for UI
    statusText: 'Closed'
  };

  onCreate() {
    const self = this;

    const viewContext = {
      openShutter: () => self.openShutter(),
      closeShutter: () => self.closeShutter(),
      stopShutter: () => self.stopShutter(),
      setLevel: (value) => self.setLevel(value),
      updateShutterIndicator: ($el) => self.updateShutterIndicator($el)
    };
    this.declare(viewContext);

    if (!this.boundModule) {
      this.model().name = 'No module bound';
      this.model().status_text = 'N/A';
      this.model().level_display = 'N/A';
      return;
    }

    this.model().name = this.boundModule.name;

    const levelField = this.boundModule.field('Status.Level');
    if (levelField) {
      this.subscribe(levelField, (f) => {
        // Store state
        this.state.level = f.decimalValue;
        this.state.levelPercent = Math.round(f.decimalValue * 100);

        if (this.state.level === 0.0) {
          this.state.statusText = 'Closed';
        } else if (this.state.level === 1.0) {
          this.state.statusText = 'Open';
        } else {
          this.state.statusText = `${this.state.levelPercent}%`;
        }
        // Render UI
        this.refreshUI();
      });

      // Initial state
      this.state.level = levelField.decimalValue;
      this.state.levelPercent = Math.round(levelField.decimalValue * 100);
      if (this.state.level === 0.0) {
        this.state.statusText = 'Closed';
      } else if (this.state.level === 1.0) {
        this.state.statusText = 'Open';
      }
      else {
        this.state.statusText = `${this.state.levelPercent}%`;
      }
      this.refreshUI();
    }
  }

  refreshUI() {
    this.model().level_percent = this.state.levelPercent;
    this.model().status_text = this.state.statusText;
    this.model().level_display = `${this.state.levelPercent}%`;
  }

  openShutter() {
    if (this.boundModule) {
      this.boundModule.control('Control.Open');
    }
  }

  closeShutter() {
    if (this.boundModule) {
      this.boundModule.control('Control.Close');
    }
  }

  stopShutter() {
    if (this.boundModule) {
      this.boundModule.control('Control.Stop');
    }
  }

  setLevel(percentValue) {
    if (this.boundModule) {
      // Update internal state immediately for responsive UI
      this.state.levelPercent = percentValue;
      this.state.level = percentValue / 100;
      if (this.state.level === 0.0) this.state.statusText = 'Closed';
      else if (this.state.level === 1.0) this.state.statusText = 'Open';
      else this.state.statusText = `${this.state.levelPercent}%`;
      this.refreshUI(); // Force UI update

      // Send command to module
      this.boundModule.control('Control.Level', percentValue);
    }
  }

  updateShutterIndicator($el) {
    // Update fill height to reflect shutter level
    // If shutter is at 0% (closed), fill bar should be 100%.
    // If shutter is at 100% (open), fill bar should be 0%.
    const fillHeight = `${100 - this.state.levelPercent}%`;
    $el.get().style.setProperty('--shutter-fill-height', fillHeight);
  }

  onDispose() { }
}
