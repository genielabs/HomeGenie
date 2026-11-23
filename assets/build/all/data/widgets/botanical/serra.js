class SerraDashboard extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard',
    sizeOptions: [],
    boundModules: {
      climate: {
        label: 'Climate sensor', labelKey: 'bind_climate',
        fieldFilter: 'Sensor.Temperature,Sensor.Humidity,Sensor.Luminance',
        defaultModule: 'HomeAutomation.Demo:1'
      },
      light: {
        label: 'Light', labelKey: 'bind_light', typeFilter: 'Light,Dimmer,Color',
        fieldFilter: 'Status.Level', defaultModule: 'HomeAutomation.PhilipsHue:1',
        defaultField: 'Meter.Watts'
      },
      fan: {label: 'Fan', labelKey: 'fan_heading', typeFilter: 'Fan,Switch', fieldFilter: 'Status.Level'}
    }
  };

  labels = {
    edition: 'Botanical observatory',
    intro: 'The light, warmth and air of your room, drawn as a living greenhouse.',
    caption: 'The sky follows the light sensor. The lamp reflects the device state.',
    climate_heading: 'Room climate',
    fan_heading: 'Fan',
    light_heading: 'Greenhouse light',
    temperature_label: 'Temperature',
    humidity_label: 'Humidity',
    luminance_label: 'Light level',
    no_sensor: 'No sensor linked',
    no_light: 'No light linked',
    unavailable: 'State unavailable',
    light_on: 'On',
    light_off: 'Off',
    turn_on: 'Turn on',
    turn_off: 'Turn off',
    waiting: 'Waiting for the light…',
    command_failed: 'The command failed. Try again.',
    command_timeout: 'The light did not confirm the change.',
    bind_climate: 'Climate sensor',
    bind_light: 'Light'
  };

  active = true;
  disposed = false;
  lightState = null;
  commands = {};
  feedback = {};

  onInit() {
    this.declare({setPower: (slot, desired) => this.setPower(slot, desired)});
  }

  onCreate() {
    this.sensor = this.getBoundModule('climate');
    this.light = this.getBoundModule('light');
    this.fan = this.getBoundModule('fan');
    this.scene = this.field('scene').get();
    this.glow = this.field('lamp_glow').get();
    this.lighting = this.field('lighting').get();
    this.reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    this.onMotionVisibility = () => {
      const moving = this.canAnimate();
      this.scene.dataset.breeze = moving ? 'running' : 'paused';
      if (!moving) this.stopMotion();
      this.renderData();
    };
    this.view().on('refresh:active', () => { this.active = true; this.onMotionVisibility(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.onMotionVisibility(); });
    document.addEventListener('visibilitychange', this.onMotionVisibility);
    this.reducedMotion.addEventListener('change', this.onMotionVisibility);

    this.renderLabels();
    this.onMotionVisibility();
    if (this.sensor) this.subscribe(this.sensor, ({key}) => {
      if (key === 'Sensor.Temperature' || key === 'Sensor.Humidity' || key === 'Sensor.Luminance') this.renderClimate();
    });
    for (const slot of ['light', 'fan']) {
      if (this[slot]) this.subscribe(this[slot], ({key}) => {
        if (key === 'Status.Level') this.renderDevice(slot);
      });
    }

    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.renderLabels();
      });
    }
    this.loadMotion();
  }

  async loadMotion() {
    try {
      const {animate} = await import(new URL('assets/js/animejs/anime.esm.min.js', document.baseURI).href);
      if (!this.disposed) this.animate = animate;
    } catch {
      // The scene also works with static state changes.
    }
  }

  renderLabels() {
    const model = this.model();
    for (const key of ['edition', 'intro', 'caption', 'climate_heading', 'light_heading', 'fan_heading',
      'temperature_label', 'humidity_label', 'luminance_label']) model[key] = this.labels[key];
    const unit = utils.preferences?.units?.temperature || 'C';
    model.temperature_unit = ['C', 'F'].includes(unit) ? '°' + unit : unit;
    this.renderData();
  }

  renderData() {
    if (this.disposed) return;
    this.renderClimate();
    this.renderDevice('light');
    this.renderDevice('fan');
  }

  read(module, key) {
    if (!module || module.isOnline === false) return;
    const field = module.field(key);
    return Number.isFinite(field?.decimalValue) ? field : undefined;
  }

  renderClimate() {
    const model = this.model();
    const temperature = this.read(this.sensor, 'Sensor.Temperature');
    const humidity = this.read(this.sensor, 'Sensor.Humidity');
    const luminance = this.read(this.sensor, 'Sensor.Luminance');
    model.sensor_name = this.sensor?.name || this.labels.no_sensor;
    model.temperature = temperature ? utils.format.fieldValue(temperature) : '—';
    model.humidity = humidity ? utils.format.fieldValue(humidity) : '—';
    model.luminance = luminance ? utils.format.fieldValue(luminance) : '—';
    const lux = luminance?.decimalValue;
    const sunlight = lux == null || lux < 0 ? .4 : Math.min(1, Math.log1p(lux) / Math.log1p(1400));
    this.scene.style.setProperty('--daylight', String(.15 + .85 * sunlight));
  }

  renderDevice(slot) {
    const level = this.read(this[slot], 'Status.Level')?.decimalValue;
    const state = level == null || level < 0 ? null : level > 0;
    if (slot === 'light' && state !== this.lightState) this.updateLamp(state);
    if (slot === 'light') this.lightState = state;
    else this.scene.dataset.fan = state ? 'on' : 'off';

    const model = this.model();
    model[slot + '_name'] = this[slot]?.name || (slot === 'light' ? this.labels.no_light : '');
    model[slot + '_state'] = this.labels[state === null ? 'unavailable' : state ? 'light_on' : 'light_off'];
    const pending = this.commands[slot];
    if (pending && pending.desired === state) this.finishCommand(slot, pending);
    for (const [suffix, desired] of [['on', true], ['off', false]]) {
      const button = this.field(slot + '_' + suffix).get();
      button.textContent = this.labels[desired ? 'turn_on' : 'turn_off'];
      button.disabled = state === null || !!this.commands[slot] || state === desired;
      button.setAttribute('aria-pressed', String(state === desired));
    }
    model[slot + '_feedback'] = this.commands[slot] ? '…' : this.labels[this.feedback[slot]] || '';
  }

  canAnimate() {
    return !this.disposed && this.active && !document.hidden && !this.reducedMotion.matches;
  }

  updateLamp(state) {
    const motion = this.animate && this.canAnimate();
    const opacity = motion ? Number(getComputedStyle(this.glow).opacity) : 0;
    this.stopMotion();
    const name = state === null ? 'unknown' : state ? 'on' : 'off';
    this.scene.dataset.lamp = name;
    this.lighting.dataset.lamp = name;
    const target = state ? 1 : 0;
    if (motion && opacity !== target) {
      this.motion = this.animate(this.glow, {
        opacity: [opacity, target], duration: 620, ease: 'outCubic',
        onComplete: () => {
          this.glow.style.removeProperty('opacity');
          this.motion = undefined;
        }
      });
    }
  }

  stopMotion() {
    if (!this.motion) return;
    this.motion.cancel();
    this.motion = undefined;
    this.glow.style.removeProperty('opacity');
  }

  setPower(slot, desired) {
    if (this.disposed || this.commands[slot] || !['light', 'fan'].includes(slot)) return;
    const level = this.read(this[slot], 'Status.Level')?.decimalValue;
    if (level == null || level < 0 || (level > 0) === desired) return;
    const command = {desired};
    this.commands[slot] = command;
    this.feedback[slot] = '';
    this.renderDevice(slot);
    command.timer = setTimeout(() => this.finishCommand(slot, command,
      slot === 'light' ? 'command_timeout' : 'command_failed'), 8000);
    try {
      const request = this[slot].control(command.desired ? 'Control.On' : 'Control.Off');
      if (!request?.subscribe) throw new Error('Command unavailable');
      command.subscription = request.subscribe({
        next: receipt => {
          const status = receipt?.response?.Status;
          if (receipt?.code != null && (receipt.code < 200 || receipt.code >= 300)
            || status != null && status !== 0 && status !== '0' && status !== 'Ok') {
            this.finishCommand(slot, command, 'command_failed');
          }
        },
        error: () => this.finishCommand(slot, command, 'command_failed')
      });
      if (this.commands[slot] !== command) command.subscription?.unsubscribe();
    } catch {
      this.finishCommand(slot, command, 'command_failed');
    }
  }

  finishCommand(slot, command, feedback = '') {
    if (this.commands[slot] !== command) return;
    delete this.commands[slot];
    clearTimeout(command.timer);
    command.subscription?.unsubscribe();
    this.feedback[slot] = feedback;
    if (!this.disposed) this.renderDevice(slot);
  }

  onDispose() {
    this.disposed = true;
    this.scene.dataset.breeze = 'paused';
    this.stopMotion();
    for (const [slot, command] of Object.entries(this.commands)) this.finishCommand(slot, command);
    document.removeEventListener('visibilitychange', this.onMotionVisibility);
    this.reducedMotion?.removeEventListener('change', this.onMotionVisibility);
  }
}
