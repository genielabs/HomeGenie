class NeonLighting extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      light1: {label: 'Light 1', labelKey: 'bind_light1', typeFilter: 'Light,Dimmer,Color'},
      light2: {label: 'Light 2', labelKey: 'bind_light2', typeFilter: 'Light,Dimmer,Color'},
      light3: {label: 'Light 3', labelKey: 'bind_light3', typeFilter: 'Light,Dimmer,Color'}
    }
  };

  labels = {
    title: 'Lighting',
    subtitle: 'Three configurable circuits',
    circuit: 'Circuit {number}',
    unassigned: 'Not linked',
    unavailable: 'Status unavailable',
    on: 'On',
    off: 'Off',
    pending: 'Waiting for device…',
    failed: 'Command failed',
    timeout: 'No confirmation',
    toggle: 'Switch {name}'
  };
  disposed = false;

  onInit() {
    this.declare({
      toggleLight: index => this.toggleLight(this.slots[index].state),
      previewLevel: (index, value) => this.previewLevel(this.slots[index].state, value),
      setLevel: (index, value) => this.setLevel(this.slots[index].state, value)
    });
  }

  onCreate() {
    this.states = new Map();
    this.slots = ['light1', 'light2', 'light3'].map(key => {
      const module = this.getBoundModule(key);
      if (module && !this.states.has(module)) this.states.set(module, {module});
      return {node: this.field(key).get(), state: this.states.get(module)};
    });
    for (const state of this.states.values()) this.subscribe(state.module, () => {
      const level = this.readLevel(state.module);
      if (state.command && level !== null && state.command.matches(level)) this.finish(state);
      else this.render();
    });
    this.render();
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.render();
      });
    }
  }

  readLevel(module) {
    const field = module?.field('Status.Level');
    if (module?.isOnline === false || !field || field.value == null || field.value === '') return null;
    const level = field.decimalValue;
    return Number.isFinite(level) && level >= 0 && level <= 1 ? level : null;
  }

  canDim(state) {
    return ['dimmer', 'color'].includes(state?.module.type?.toLowerCase());
  }

  render() {
    const labels = this.labels;
    this.model().title = labels.title;
    this.model().subtitle = labels.subtitle;
    this.slots.forEach(({node, state}, index) => {
      const level = this.readLevel(state?.module);
      const on = level === null ? null : level > 0;
      const name = state?.module.name || labels.circuit.replace('{number}', index + 1);
      node.classList.toggle('is-on', on === true);
      node.classList.toggle('is-pending', !!state?.command);
      node.classList.toggle('is-error', !!state?.error);
      const nameNode = node.querySelector('[data-part="name"]');
      nameNode.textContent = name;
      nameNode.title = name;
      const text = !state ? labels.unassigned : state.command ? labels.pending
        : state.error ? labels[state.error] : on === null ? labels.unavailable : on ? labels.on : labels.off;
      const status = node.querySelector('[data-part="state"]');
      if (status.textContent !== text) status.textContent = text;
      const button = node.querySelector('button');
      button.disabled = on === null || !!state?.command;
      const levelControl = node.querySelector('.level-control');
      const slider = levelControl.querySelector('input');
      const percent = state?.preview ?? (level === null ? null : Math.round(level * 100));
      levelControl.hidden = !this.canDim(state);
      slider.disabled = level === null || !!state?.command;
      slider.value = String(percent ?? 0);
      slider.setAttribute('aria-label', name);
      slider.setAttribute('aria-valuetext', percent === null ? labels.unavailable : percent + '%');
      levelControl.querySelector('output').textContent = percent === null ? '—' : percent + '%';
      button.setAttribute('aria-pressed', String(on === true));
      button.setAttribute('aria-label', labels.toggle.replace('{name}', name));
    });
  }

  toggleLight(state) {
    if (this.disposed || !state || state.command) return;
    const level = this.readLevel(state.module);
    if (level === null) return;
    const turnOn = level === 0;
    this.sendCommand(state, turnOn ? 'Control.On' : 'Control.Off', undefined, value => (value > 0) === turnOn);
  }

  previewLevel(state, value) {
    if (!this.canDim(state) || state.command) return;
    state.preview = Number(value);
    this.render();
  }

  setLevel(state, value) {
    if (!this.canDim(state) || state.command) return;
    state.preview = undefined;
    const level = this.readLevel(state.module);
    const percent = Number(value);
    if (level === null || !Number.isInteger(percent) || percent < 0 || percent > 100 || Math.round(level * 100) === percent) {
      this.render();
      return;
    }
    this.sendCommand(state, 'Control.Level', percent, actual => percent === 0 ? actual === 0 : Math.abs(actual * 100 - percent) <= 1 && actual > 0);
  }

  sendCommand(state, action, value, matches) {
    if (this.disposed) return;
    state.preview = undefined;
    const command = {matches};
    state.command = command;
    state.error = undefined;
    command.timer = setTimeout(() => this.finishIfCurrent(state, command, 'timeout'), 8000);
    this.render();
    try {
      command.subscription = state.module.control(action, value).subscribe({
        next: receipt => {
          const status = receipt?.response?.Status;
          if (receipt?.code != null && (receipt.code < 200 || receipt.code >= 300)
              || status != null && status !== 0 && status !== '0' && status !== 'Ok')
            this.finishIfCurrent(state, command, 'failed');
        },
        error: () => this.finishIfCurrent(state, command, 'failed')
      });
      if (state.command !== command) command.subscription.unsubscribe();
    } catch {
      this.finishIfCurrent(state, command, 'failed');
    }
  }

  finishIfCurrent(state, command, error) {
    if (!this.disposed && state.command === command) this.finish(state, error);
  }

  finish(state, error) {
    const command = state.command;
    state.command = undefined;
    state.error = error;
    clearTimeout(command.timer);
    command.subscription?.unsubscribe();
    this.render();
  }

  onDispose() {
    this.disposed = true;
    for (const {command} of this.states.values()) {
      if (!command) continue;
      clearTimeout(command.timer);
      command.subscription?.unsubscribe();
    }
  }
}
