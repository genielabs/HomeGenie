class IsometricClimate extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      climate: {label: 'Climate', labelKey: 'bind_climate', fieldFilter: 'Sensor.Temperature,Sensor.Humidity'}
    }
  };
  labels = {
    "climate_heading": "Climate",
    "no_sensor": "No sensor selected",
    "temperature_label": "Temperature",
    "humidity_label": "Humidity",
    "bind_climate": "Climate"
  };
  disposed = false;

  onCreate() {
    this.meter = this.getBoundModule('climate');
    this.applyLabels();
    this.loadLabels();
    if (this.meter) this.subscribe(this.meter, () => this.renderData());
  }

  loadLabels() {
    if (typeof this.translate !== 'function') return;
    const entries = Object.entries(this.labels);
    let remaining = entries.length;
    for (const [key, fallback] of entries) {
      this.translate(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed) return;
        if (typeof value === 'string' && value !== key && value !== 'WIDGETS.' + key) this.labels[key] = value;
        if (--remaining === 0) {
          this.applyLabels();
        }
      });
    }
  }

  numeric(module, key) {
    if (!module || module.isOnline === false) return null;
    const field = module.field(key), raw = field?.decimalValue ?? field?.value;
    if (raw == null || typeof raw === 'string' && !raw.trim()) return null;
    const number = typeof raw === 'string' ? Number(raw.replace(',', '.')) : raw;
    return typeof number === 'number' && Number.isFinite(number) ? number : null;
  }

  formatted(module, key) {
    if (this.numeric(module, key) === null) return '—';
    return String(utils.format.fieldValue(module.field(key)));
  }

  applyLabels() {
    const model = this.model();
    model.climate_heading = this.labels.climate_heading;
    model.temperature_label = this.labels.temperature_label;
    model.humidity_label = this.labels.humidity_label;
    const unit = utils.preferences?.units?.temperature || 'C';
    model.temperature_unit = ['C', 'F'].includes(unit) ? '°' + unit : unit;
    model.humidity_unit = '%';
    this.renderData();
  }

  renderData() {
    const model = this.model();
    model.climate_name = this.meter?.name || this.labels.no_sensor;
    model.temperature = this.formatted(this.meter, 'Sensor.Temperature');
    model.humidity = this.formatted(this.meter, 'Sensor.Humidity');
  }

  onDispose() {
    this.disposed = true;
  }
}
