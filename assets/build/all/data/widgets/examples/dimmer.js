class CircularDimmer extends ControllerInstance {
  settings = { moduleSelect: { fieldFilter: 'Status.Level', typeFilter: 'Dimmer,Color' }, sizeOptions: ['small', 'medium', 'big'], defaultSize: 'small' };
  state = { level: 0, isOn: false, isDragging: false };
  throttleTimer = null;
  resizeObserver = null;

  onCreate() {
    this.declare({ 
      toggle: () => this.toggle(), 
      startDrag: (e) => this.startDrag(e),
      getOffset: () => {
        const circ = 2 * Math.PI * 42;
        return circ - (this.state.level / 100) * circ;
      },
      level: () => this.state.level,
      isOn: () => this.state.isOn
    });

    this.resizeObserver = new ResizeObserver((entries) => {
      for (let entry of entries) {
        const newHeight = entry.contentRect.height - 24;
        if (newHeight < 320) {
          this.view().get().style.setProperty('--container-height', newHeight + 'px');
        }
      }
    });
    this.resizeObserver.observe(this.view().get());

    if (this.boundModule) {
      this.model().name = this.boundModule.name;
      const levelField = this.boundModule.field('Status.Level');
      
      // Initial state sync
      if (levelField) {
        this.syncState(levelField);
        this.subscribe(levelField, (f) => this.syncState(f));
      }
    }

    var ch = Math.round(this.view().position().rect.height) - 24;
    this.view().get().style.setProperty('--container-height', ch + 'px');
  }

  syncState(f) {
    this.state.level = Math.round(f.decimalValue * 100);
    this.state.isOn = f.decimalValue > 0;
    this.model().status_text = this.state.isOn ? 'On' : 'Off';
    this.update();
  }

  toggle() {
    if (!this.state.isDragging) {
      this.boundModule.control(this.state.isOn ? 'Control.Off' : 'Control.On');
    }
  }

  startDrag(e) {
    if (!this.boundModule) {
      this.configure();
      return;
    }
    this.state.isDragging = false;
    const ring = this.field('ring').get();
    const rect = ring.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const calculateLevel = (ev) => {
      const angle = Math.atan2(ev.clientY - centerY, ev.clientX - centerX) * (180 / Math.PI);
      return Math.round(((angle + 90 + 360) % 360 / 360) * 100);
    };

    this.moveHandler = (ev) => {
      const dist = Math.sqrt(Math.pow(ev.clientX - e.clientX, 2) + Math.pow(ev.clientY - e.clientY, 2));
      if (dist > 5) this.state.isDragging = true;
      
      this.state.level = calculateLevel(ev);
      this.update();
      if (!this.throttleTimer) {
        this.throttleTimer = setTimeout(() => {
          this.boundModule.control('Control.Level', this.state.level);
          this.throttleTimer = null;
        }, 100);
      }
    };

    this.stopHandler = () => {
      window.removeEventListener('pointermove', this.moveHandler);
      window.removeEventListener('pointerup', this.stopHandler);
      if (!this.state.isDragging) {
         this.state.level = calculateLevel(e);
         this.boundModule.control('Control.Level', this.state.level);
      }
      setTimeout(() => this.state.isDragging = false, 100);
    };

    window.addEventListener('pointermove', this.moveHandler);
    window.addEventListener('pointerup', this.stopHandler);
  }

  onDispose() {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.moveHandler) window.removeEventListener('pointermove', this.moveHandler);
    if (this.stopHandler) window.removeEventListener('pointerup', this.stopHandler);
    if (this.throttleTimer) clearTimeout(this.throttleTimer);
  }
}
