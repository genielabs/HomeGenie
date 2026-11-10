class NeonTelemetry extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      climate: {label: 'Climate sensor', labelKey: 'bind_climate', fieldFilter: 'Sensor.Temperature,Sensor.Humidity'},
      energy: {
        label: 'Power meter', labelKey: 'bind_energy',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor'
      }
    }
  };

  labels = {
    title: 'Telemetry',
    subtitle: 'Indoor climate and electrical load',
    temperature_label: 'Temperature',
    humidity_label: 'Humidity',
    power_label: 'Live power',
    trace_label: 'Recent readings',
    trace_empty: 'Waiting for readings',
    no_power: 'No power meter linked'
  };
  disposed = false;

  onCreate() {
    this.climate = this.getBoundModule('climate');
    this.energy = this.getBoundModule('energy');
    if (this.energy) this.loadHistory();
    for (const module of new Set([this.climate, this.energy].filter(Boolean)))
      this.subscribe(module, () => this.render());
    this.render();
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.render();
      });
    }
  }

  loadHistory() {
    import(new URL('assets/js/widgets/power-history.js', document.baseURI).href).then(({PowerHistory}) => {
      if (this.disposed) return;
      this.history = new PowerHistory(() => this.renderTrace());
      this.render();
    }).catch(error => console.warn('Unable to load the power chart.', error));
  }

  read(module, key) {
    const field = module?.field(key);
    if (module?.isOnline === false || !field || field.value == null || field.value === '') return;
    return Number.isFinite(field.decimalValue) ? field : undefined;
  }

  render() {
    const model = this.model();
    for (const key of ['title', 'subtitle', 'temperature_label', 'humidity_label', 'power_label'])
      model[key] = this.labels[key];
    const temperature = this.read(this.climate, 'Sensor.Temperature');
    const humidity = this.read(this.climate, 'Sensor.Humidity');
    const powerKey = this.energy?.field('EnergyMonitor.WattLoad') ? 'EnergyMonitor.WattLoad' : 'Meter.Watts';
    const power = this.read(this.energy, powerKey);
    model.temperature = temperature ? utils.format.fieldValue(temperature) : '—';
    model.humidity = humidity ? utils.format.fieldValue(humidity) : '—';
    model.watts = power ? utils.format.fieldValue({key: 'Meter.Watts', value: power.value}) : '—';
    const units = utils.preferences?.units;
    const temperatureUnit = units?.temperature || 'C';
    model.temperature_unit = ['C', 'F'].includes(temperatureUnit) ? '°' + temperatureUnit : temperatureUnit;
    model.power_unit = units?.power || 'W';

    this.history?.update(this.energy, this.energy?.field(powerKey) ? powerKey : undefined, power);
    this.renderTrace();
  }

  renderTrace() {
    const points = this.history?.points || '';
    const chart = this.field('trace_area').get();
    chart.toggleAttribute('hidden', !points);
    chart.setAttribute('aria-label', this.labels.trace_label);
    this.model().trace_label = this.labels[points ? 'trace_label' : this.energy ? 'trace_empty' : 'no_power'];
    this.field('trace').get().setAttribute('points', points);
  }

  onDispose() {
    this.disposed = true;
    this.history?.dispose();
  }
}
