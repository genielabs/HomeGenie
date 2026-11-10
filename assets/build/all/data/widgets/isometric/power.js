class IsometricPower extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      energy: {
        label: 'Power', labelKey: 'bind_power',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor'
      }
    }
  };
  labels = {
    "energy_heading": "Energy",
    "no_meter": "No meter selected",
    "power_label": "Instantaneous power",
    "trace_label": "Recent readings",
    "trace_empty": "Waiting for readings",
    "bind_power": "Power"
  };
  disposed = false;

  onCreate() {
    this.meter = this.getBoundModule('energy');
    if (this.meter) this.loadHistory();
    this.applyLabels();
    this.loadLabels();
    if (this.meter) this.subscribe(this.meter, () => this.renderData());
  }

  loadHistory() {
    import(new URL('assets/js/widgets/power-history.js', document.baseURI).href).then(({PowerHistory}) => {
      if (this.disposed) return;
      this.history = new PowerHistory(() => this.renderTrace());
      this.renderData();
    }).catch(error => console.warn('Unable to load the power chart.', error));
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

  applyLabels() {
    const model = this.model();
    model.energy_heading = this.labels.energy_heading;
    model.power_label = this.labels.power_label;
    model.power_unit = utils.preferences?.units?.power || 'W';
    this.renderData();
  }

  renderData() {
    const model = this.model();
    const field = this.meter?.field('EnergyMonitor.WattLoad') ? 'EnergyMonitor.WattLoad' : 'Meter.Watts';
    const watts = this.numeric(this.meter, field);
    model.energy_name = this.meter?.name || this.labels.no_meter;
    model.power = watts === null ? '—' : String(utils.format.fieldValue({key: 'Meter.Watts', value: watts}));
    const reading = this.meter?.field(field);
    this.history?.update(this.meter, reading ? field : undefined,
      watts === null ? undefined : {timestamp: reading.timestamp, decimalValue: watts});
    this.renderTrace();
  }

  renderTrace() {
    const points = this.history?.points || '';
    const chart = this.field('trace_area').get();
    chart.toggleAttribute('hidden', !points);
    chart.setAttribute('aria-label', this.labels.trace_label);
    this.model().trace_label = this.meter ? this.labels[points ? 'trace_label' : 'trace_empty'] : '';
    this.field('trace').get().setAttribute('points', points);
  }

  onDispose() {
    this.disposed = true;
    this.history?.dispose();
  }
}
