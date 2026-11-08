class RadioCasa extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard',
    sizeOptions: [],
    boundModules: {
      power: {
        label: 'Power meter', labelKey: 'bind_power',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor',
        defaultModule: 'HomeAutomation.EnergyMonitor:1'
      },
      climate: {
        label: 'Climate sensor', labelKey: 'bind_climate',
        fieldFilter: 'Sensor.Temperature,Sensor.Humidity', defaultModule: 'HomeAutomation.Demo:1'
      },
      preset1: {
        label: 'Light 1', labelKey: 'bind_preset_1', typeFilter: 'Light,Dimmer,Color', fieldFilter: 'Status.Level',
        defaultModule: 'HomeAutomation.PhilipsHue:1', defaultField: 'Simulator.Meter.Watts'
      },
      preset2: {
        label: 'Light 2', labelKey: 'bind_preset_2', typeFilter: 'Light,Dimmer,Color', fieldFilter: 'Status.Level',
        defaultModule: 'HomeAutomation.PhilipsHue:2', defaultField: 'Simulator.Meter.Watts'
      },
      preset3: {
        label: 'Light 3', labelKey: 'bind_preset_3', typeFilter: 'Light,Dimmer,Color', fieldFilter: 'Status.Level',
        defaultModule: 'HomeAutomation.PhilipsHue:4', defaultField: 'Simulator.Meter.Watts'
      }
    }
  };

  labels = {
    eyebrow: 'Home receiver · solid state',
    power_heading: 'Power meter',
    trace_heading: 'Last 60 minutes',
    trace_waiting: 'Waiting for readings',
    climate_heading: 'Indoor climate',
    temp_label: 'Temperature',
    humidity_label: 'Humidity',
    preset_heading: 'Light presets',
    preset_hint: 'Press to switch',
    footer_note: 'Choose your devices in dashboard settings.',
    no_meter: 'No meter linked',
    no_climate: 'No sensor linked',
    no_light: 'No light linked',
    light_on: 'On',
    light_off: 'Off',
    unavailable: 'Unavailable',
    waiting: 'Waiting for the light…',
    command_error: 'The command failed.',
    command_timeout: 'The light did not confirm the change.',
    bind_power: 'Power meter',
    bind_climate: 'Climate sensor',
    bind_preset_1: 'Light 1',
    bind_preset_2: 'Light 2',
    bind_preset_3: 'Light 3'
  };

  disposed = false;
  active = true;
  fullScale = 200;
  commands = new Map();

  onInit() {
    this.declare({togglePreset: index => this.togglePreset(index)});
  }

  onCreate() {
    this.meter = this.getBoundModule('power');
    this.climate = this.getBoundModule('climate');
    this.presets = [1, 2, 3].map(index => this.getBoundModule('preset' + index));
    this.face = this.field('face').get();
    this.traceArea = this.field('trace_area').get();
    this.traceLine = this.field('trace').get();
    this.visibility = () => this.updateBacklight();
    document.addEventListener('visibilitychange', this.visibility);
    this.view().on('refresh:active', () => { this.active = true; this.updateBacklight(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.updateBacklight(); });
    this.updateBacklight();
    this.applyLabels();

    if (this.meter) this.subscribe(this.meter, ({key}) => {
      if (key === 'EnergyMonitor.WattLoad' || key === 'Meter.Watts') this.renderPower();
    });
    if (this.climate) this.subscribe(this.climate, ({key}) => {
      if (key === 'Sensor.Temperature' || key === 'Sensor.Humidity') this.renderClimate();
    });
    for (const module of new Set(this.presets.filter(Boolean))) this.subscribe(module, ({key}) => {
      if (key === 'Status.Level') this.renderPresets();
    });
    this.loadLabels();
    if (this.meter) this.loadHistory();
  }

  loadLabels() {
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.applyLabels();
      });
    }
  }

  applyLabels() {
    for (const key of ['eyebrow', 'power_heading', 'trace_heading', 'climate_heading', 'temp_label',
      'humidity_label', 'preset_heading', 'preset_hint', 'footer_note']) this.write(key, this.labels[key]);
    const unit = utils.preferences?.units?.temperature || 'C';
    this.write('temp_unit', ['C', 'F'].includes(unit) ? '°' + unit : unit);
    this.renderPower();
    this.renderClimate();
    this.renderPresets();
  }

  write(key, value) { this.field(key).get().textContent = value; }

  reading(module, key) {
    if (!module || module.isOnline === false) return;
    const field = module.field(key);
    if (Number.isFinite(field?.decimalValue)) return field;
  }

  formatted(field) {
    return field ? String(utils.format.fieldValue(field)) : '—';
  }

  needle(key, value, min, max) {
    const element = this.field(key).get();
    const position = value == null ? 0 : Math.min(1, Math.max(0, (value - min) / (max - min)));
    element.style.setProperty('--angle', (-60 + 120 * position) + 'deg');
    element.toggleAttribute('data-unavailable', value == null);
  }

  renderPower() {
    const key = this.meter?.field('EnergyMonitor.WattLoad') ? 'EnergyMonitor.WattLoad' : 'Meter.Watts';
    const field = this.reading(this.meter, key), watts = field?.decimalValue;
    while (watts > this.fullScale) this.fullScale *= 2;
    const formatScale = value => Number(value.toPrecision(4)).toLocaleString(undefined, {maximumFractionDigits: 2});
    this.write('scale_quarter', formatScale(this.fullScale / 4));
    this.write('scale_half', formatScale(this.fullScale / 2));
    this.write('scale_three_quarters', formatScale(this.fullScale * .75));
    this.write('scale_max', formatScale(this.fullScale));
    this.write('power_name', this.meter?.name || this.labels.no_meter);
    this.write('watts', this.formatted(field));
    this.needle('needle', watts, 0, this.fullScale);
    this.history?.update(this.meter, key, field);
    this.renderTrace();
  }

  async loadHistory() {
    try {
      const {PowerHistory} = await import('/widgets/_shared/power-history.js?v=2');
      if (this.disposed) return;
      this.history = new PowerHistory(() => this.renderTrace(), 60 * 60 * 1000);
      this.renderPower();
    } catch {
      this.renderTrace();
    }
  }

  renderTrace() {
    if (this.disposed) return;
    const points = this.history?.points || '';
    this.traceLine.setAttribute('points', points);
    this.traceArea.setAttribute('aria-label', this.labels.trace_heading);
    this.write('trace_status', this.meter && !points ? this.labels.trace_waiting : '');
  }

  renderClimate() {
    const temperature = this.reading(this.climate, 'Sensor.Temperature');
    const humidity = this.reading(this.climate, 'Sensor.Humidity');
    this.write('climate_name', this.climate?.name || this.labels.no_climate);
    this.write('temperature', this.formatted(temperature));
    this.write('humidity', this.formatted(humidity));
    this.needle('temp_needle', temperature?.decimalValue, -10, 45);
    this.needle('humidity_needle', humidity?.decimalValue, 0, 100);
  }

  state(module) {
    const level = this.reading(module, 'Status.Level')?.decimalValue;
    return level == null || level < 0 ? null : level > 0;
  }

  renderPresets() {
    this.presets.forEach((module, index) => {
      const state = this.state(module), command = this.commands.get(module);
      if (command && state === command.on) this.finishCommand(module, command);
      const pending = this.commands.has(module), button = this.field('preset_' + index).get();
      button.disabled = pending || state === null;
      button.dataset.state = state === null ? 'unknown' : state ? 'on' : 'off';
      button.setAttribute('aria-pressed', String(state === true));
      button.setAttribute('aria-busy', String(pending));
      this.write('name_' + index, module?.name || this.labels.no_light);
      this.write('state_' + index, this.labels[pending ? 'waiting' : state === null ? 'unavailable' : state ? 'light_on' : 'light_off']);
    });
    const feedback = this.feedback;
    this.write('feedback', feedback ? (feedback.module.name ? feedback.module.name + ' · ' : '') + this.labels[feedback.key] : '');
  }

  togglePreset(index) {
    const module = this.presets[index], state = this.state(module);
    if (this.disposed || state === null || this.commands.has(module)) return;
    const command = {on: !state};
    this.commands.set(module, command);
    this.feedback = undefined;
    this.renderPresets();
    const fail = key => {
      if (this.disposed || !this.finishCommand(module, command)) return;
      this.feedback = {module, key};
      this.renderPresets();
    };
    command.timer = setTimeout(() => fail('command_timeout'), 8000);
    try {
      command.subscription = module.control(command.on ? 'Control.On' : 'Control.Off').subscribe({
        next: receipt => {
          const status = receipt?.response?.Status;
          if (receipt?.code != null && (receipt.code < 200 || receipt.code >= 300)
            || status != null && status !== 0 && status !== '0' && status !== 'Ok') fail('command_error');
        },
        error: () => fail('command_error')
      });
      if (this.commands.get(module) !== command) command.subscription?.unsubscribe();
    } catch { fail('command_error'); }
  }

  finishCommand(module, command) {
    if (this.commands.get(module) !== command) return false;
    this.commands.delete(module);
    clearTimeout(command.timer);
    command.subscription?.unsubscribe();
    return true;
  }

  updateBacklight() {
    clearTimeout(this.backlightTimer);
    if (this.disposed || !this.active || document.hidden) return;
    const now = new Date(), hour = now.getHours();
    this.face.dataset.night = String(hour < 7 || hour >= 18);
    const next = new Date(now);
    if (hour >= 18) next.setDate(next.getDate() + 1);
    next.setHours(hour < 7 || hour >= 18 ? 7 : 18, 0, 0, 0);
    this.backlightTimer = setTimeout(() => this.updateBacklight(), next - now);
  }

  onDispose() {
    this.disposed = true;
    document.removeEventListener('visibilitychange', this.visibility);
    clearTimeout(this.backlightTimer);
    this.history?.dispose();
    for (const [module, command] of this.commands) this.finishCommand(module, command);
  }
}
