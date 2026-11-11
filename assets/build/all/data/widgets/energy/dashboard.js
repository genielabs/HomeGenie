class EnergyDashboard extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard',
    sizeOptions: [],
    boundModules: {
      meter: {
        label: 'Power meter', labelKey: 'meter',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor',
        defaultModule: 'HomeAutomation.EnergyMonitor:1'
      },
      device1: {label: 'Power meter 1', labelKey: 'device1', fieldFilter: 'Meter.Watts', defaultModule: 'HomeAutomation.PhilipsHue:1'},
      device2: {label: 'Power meter 2', labelKey: 'device2', fieldFilter: 'Meter.Watts', defaultModule: 'HomeAutomation.PhilipsHue:2'},
      device3: {label: 'Power meter 3', labelKey: 'device3', fieldFilter: 'Meter.Watts', defaultModule: 'HomeAutomation.PhilipsHue:4'},
      device4: {label: 'Power meter 4', labelKey: 'device4', fieldFilter: 'Meter.Watts'}
    }
  };

  labels = {
    title: 'Energy',
    current_label: 'Instantaneous power',
    today_label: 'Counter (today)',
    peak_label: 'Peak today',
    history_label: 'History',
    hour_label: 'Last 60 minutes',
    day_label: 'Since midnight',
    devices_label: 'Devices',
    no_meter: 'No power meter linked',
    waiting: 'Waiting for readings',
    unavailable: 'Unavailable'
  };

  hour = 3_600_000;
  range = 'hour';
  rangeStorageKey = 'homegenie.widgets.energy.dashboard.range';
  samples = new Map();
  active = true;
  disposed = false;
  visibility = () => this.sync();

  onInit() {
    try {
      if (localStorage.getItem(this.rangeStorageKey) === 'day') this.range = 'day';
    } catch {
      // Keep the default when the client blocks storage.
    }
    this.declare({selectRange: range => {
      if (range !== 'hour' && range !== 'day') return;
      this.range = range;
      try {
        localStorage.setItem(this.rangeStorageKey, range);
      } catch {
        // The current view still works when the client blocks storage.
      }
      this.render();
    }});
  }

  onCreate() {
    this.root = this.field('canvas').get().closest('.energy-dashboard');
    this.meter = this.getBoundModule('meter');
    this.devices = Array.from(new Set(['device1', 'device2', 'device3', 'device4']
      .map(role => this.getBoundModule(role)).filter(module => module && module !== this.meter)));
    for (const module of new Set([this.meter, ...this.devices].filter(Boolean))) {
      this.subscribe(module, ({key}) => {
        if (key === 'Meter.Watts' || key === 'EnergyMonitor.WattLoad' || key === 'EnergyMonitor.WattCounter.Today') {
          if (module === this.meter) this.updateMeter();
          this.render();
        }
      });
    }

    const body = this.root.ownerDocument.body;
    this.themeObserver = new MutationObserver(() => this.applyTheme());
    this.themeObserver.observe(body, {attributes: true, attributeFilter: ['class']});
    this.applyTheme();
    this.view().on('refresh:active', () => { this.active = true; this.sync(); });
    this.view().on('refresh:inactive', () => { this.active = false; clearTimeout(this.timer); });
    document.addEventListener('visibilitychange', this.visibility);
    this.sync();
    this.applyLabels();
    this.createChart().catch(() => {
      if (this.disposed) return;
      this.chartFailed = true;
      this.render();
    });
  }

  applyLabels() {
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.render();
      });
    }
  }

  element(name) { return this.field(name).get(); }
  write(name, value) { this.element(name).textContent = value; }
  midnight(now) { return new Date(now).setHours(0, 0, 0, 0); }
  locale() {
    const language = typeof utils !== 'undefined' ? utils.preferences?.ui?.language : undefined;
    return language && /^[a-z]{2,3}(?:-[a-zA-Z0-9]+)*$/.test(language)
      ? language : navigator.language;
  }
  format(value, digits = 0) {
    return value == null ? '—' : value.toLocaleString(this.locale(), {maximumFractionDigits: digits});
  }

  reading(module, field) {
    if (module?.isOnline === false) return undefined;
    const reading = module?.field(field);
    const value = reading?.decimalValue;
    return Number.isFinite(value) && value >= 0 && String(reading.value ?? '').trim() !== '' ? reading : undefined;
  }

  updateMeter() {
    const key = this.meter?.field('EnergyMonitor.WattLoad') ? 'EnergyMonitor.WattLoad'
      : this.meter?.field('Meter.Watts') ? 'Meter.Watts' : undefined;
    if (this.key !== key) {
      this.key = key;
      this.request?.unsubscribe();
      this.pending = undefined;
      this.lastEnd = undefined;
      this.failed = false;
      this.samples.clear();
    }
    const reading = this.reading(this.meter, key);
    const timestamp = Number(reading?.timestamp);
    const now = Date.now();
    const start = Math.min(this.midnight(now), now - this.hour);
    if (timestamp >= start && timestamp <= now) this.samples.set(timestamp, reading.decimalValue);
  }

  sync() {
    clearTimeout(this.timer);
    if (this.disposed || !this.active || document.hidden) return;
    this.updateMeter();
    this.loadHistory();
    this.render();
    this.resizeChart();
    this.timer = setTimeout(() => this.sync(), 60_000);
  }

  loadHistory() {
    if (!this.key || this.pending || this.meter.isOnline === false) return;
    const end = Date.now();
    const start = Math.min(this.midnight(end), end - this.hour);
    const since = Math.max(start, this.lastEnd ?? start);
    if (since >= end) return;
    const query = {};
    this.pending = query;
    const finish = rows => {
      if (this.disposed || this.pending !== query) return;
      this.pending = undefined;
      this.failed = !Array.isArray(rows);
      if (!this.failed) {
        for (const {x, y} of rows) {
          if (Number.isFinite(x) && x >= since && x <= end && Number.isFinite(y) && y >= 0 && !this.samples.has(x)) {
            this.samples.set(x, y);
          }
        }
        this.lastEnd = end;
      }
      this.render();
    };
    const result = this.meter.control('Statistics.Field.GetData', {
      field: this.key, rangeStart: since, rangeEnd: end, chartWidth: 720
    });
    if (!result) return finish();
    this.request = result.subscribe({next: finish, error: () => finish()});
  }

  render() {
    const now = Date.now();
    const midnight = this.midnight(now);
    const start = Math.min(midnight, now - this.hour);
    const chartStart = this.range === 'day' ? midnight : now - this.hour;
    const chartPoints = [];
    let peak;
    for (const [time, power] of this.samples) {
      if (time < start) this.samples.delete(time);
      if (time >= midnight) peak = peak === undefined ? power : Math.max(peak, power);
      if (time >= chartStart) chartPoints.push({x: time, y: power});
    }
    const counter = this.reading(this.meter, 'EnergyMonitor.WattCounter.Today');
    this.write('meter_name', this.meter?.name ?? '');
    this.write('current', this.format(this.reading(this.meter, this.key)?.decimalValue));
    this.write('today', this.format(counter ? counter.decimalValue / 1000 : undefined, 2));
    this.write('peak', this.format(peak));
    for (const key of ['title', 'current_label', 'today_label', 'peak_label', 'history_label',
      'hour_label', 'day_label', 'devices_label']) this.write(key, this.labels[key]);
    this.element('hour_label').setAttribute('aria-pressed', String(this.range === 'hour'));
    this.element('day_label').setAttribute('aria-pressed', String(this.range === 'day'));
    const status = !this.meter ? this.labels.no_meter
      : this.chartFailed ? this.labels.unavailable
      : chartPoints.length ? ''
      : this.failed || this.meter.isOnline === false ? this.labels.unavailable : this.labels.waiting;
    this.write('history_status', status);
    this.renderDevices();
    if (!this.chart) return;
    this.chart.canvas.setAttribute('aria-label', this.labels.history_label + ' · ' +
      this.labels[this.range === 'day' ? 'day_label' : 'hour_label']);
    chartPoints.sort((a, b) => a.x - b.x);
    this.chart.data.datasets[0].data = this.range === 'day' ? this.averageToday(chartPoints) : chartPoints;
    this.chart.options.scales.x.min = chartStart;
    this.chart.options.scales.x.max = Math.max(now, chartStart + 1000);
    this.chart.update('none');
  }

  averageToday(points) {
    if (points.length < 2) return points;
    const windowMs = 60_000;
    const first = points[0].x;
    const last = points.at(-1).x;
    const times = new Set();
    const area = [0];
    for (let index = 0; index < points.length; index++) {
      const {x} = points[index];
      times.add(x);
      // Include the instant a reading leaves the window, even between samples.
      if (x + windowMs < last) times.add(x + windowMs);
      if (index > 0) {
        const previous = points[index - 1];
        area.push(area[index - 1] + (x - previous.x) * previous.y);
      }
    }

    let left = 0;
    let right = 0;
    const integralAt = (index, time) => area[index] + (time - points[index].x) * points[index].y;
    // Weight each reading by how long it held within the trailing window.
    return [...times].sort((a, b) => a - b).map(x => {
      const start = Math.max(first, x - windowMs);
      while (points[left + 1]?.x <= start) left++;
      while (points[right + 1]?.x <= x) right++;
      const y = x === first ? points[0].y : (integralAt(right, x) - integralAt(left, start)) / (x - start);
      return {x, y};
    });
  }

  renderDevices() {
    const devices = this.devices.map(module => ({
      name: module.name, power: this.reading(module, 'Meter.Watts')?.decimalValue
    })).sort((a, b) => (b.power ?? -1) - (a.power ?? -1));
    const total = this.reading(this.meter, this.key)?.decimalValue ?? 0;
    const max = Math.max(1, total, devices[0]?.power ?? 0);
    const list = this.element('devices');
    const doc = list.ownerDocument;
    list.replaceChildren(...devices.map(device => {
      const item = doc.createElement('li');
      const name = doc.createElement('span');
      name.className = 'device-name';
      name.textContent = device.name;
      const value = doc.createElement('span');
      value.className = 'device-value';
      value.textContent = device.power === undefined ? '—' : this.format(device.power) + ' W';
      const track = doc.createElement('div');
      track.className = 'device-bar';
      track.setAttribute('aria-hidden', 'true');
      const bar = doc.createElement('span');
      bar.style.width = ((device.power ?? 0) / max * 100) + '%';
      track.append(bar);
      item.append(name, value, track);
      return item;
    }));
    this.write('devices_status', devices.length ? '' : this.labels.no_meter);
  }

  applyTheme() {
    const classes = this.root.ownerDocument.body.classList;
    const style = getComputedStyle(this.root);
    const text = style.color;
    const [red, green, blue] = text.match(/\d+(?:\.\d+)?/g)?.map(Number) ?? [];
    // Panel injects the theme colors without the YoT body classes.
    const dark = classes.contains('dark-theme') || (!classes.contains('light-theme') &&
      .2126 * red + .7152 * green + .0722 * blue > 150);
    this.root.toggleAttribute('data-dark', dark);
    if (!this.chart) return;
    const color = getComputedStyle(this.root).getPropertyValue('--energy-accent').trim();
    const grid = style.getPropertyValue('--divider-color').trim() || '#8794a640';
    const dataset = this.chart.data.datasets[0];
    dataset.borderColor = color;
    dataset.backgroundColor = ({chart}) => {
      const area = chart.chartArea;
      if (!area) return color + '26';
      const gradient = chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
      gradient.addColorStop(0, color + '64');
      gradient.addColorStop(1, color + '08');
      return gradient;
    };
    for (const axis of Object.values(this.chart.options.scales)) {
      axis.grid.color = grid;
      axis.ticks.color = text;
    }
    this.chart.update('none');
  }

  async createChart() {
    const {default: Chart} = await import(new URL('assets/js/chartjs/chart.module.js', document.baseURI).href);
    if (this.disposed) return;
    const clock = time => new Date(time).toLocaleTimeString(this.locale(), {hour: '2-digit', minute: '2-digit'});
    this.chart = new Chart(this.element('canvas'), {
      type: 'line',
      data: {datasets: [{data: [], borderWidth: 2, fill: 'origin', pointRadius: context =>
        context.dataset.data.length === 1 ? 3 : 0, pointHitRadius: 8}]},
      options: {
        responsive: false, maintainAspectRatio: false, animation: false, parsing: false,
        normalized: true,
        interaction: {mode: 'nearest', axis: 'x', intersect: false},
        plugins: {
          legend: {display: false},
          tooltip: {callbacks: {
            title: items => clock(items[0].parsed.x),
            label: item => this.format(item.parsed.y) + ' W'
          }},
          decimation: {enabled: true, algorithm: 'min-max'}
        },
        scales: {
          x: {type: 'linear', ticks: {callback: clock, maxTicksLimit: 6}},
          y: {beginAtZero: true, ticks: {maxTicksLimit: 6, callback: watts => this.format(watts) + ' W'}}
        }
      }
    });
    this.resizeObserver = new ResizeObserver(() => this.resizeChart());
    this.resizeObserver.observe(this.element('plot'));
    this.applyTheme();
    this.render();
    this.resizeChart();
  }

  resizeChart() {
    if (!this.chart || !this.active || document.hidden) return;
    const {width, height} = this.element('plot').getBoundingClientRect();
    if (width > 0 && height > 0) this.chart.resize(width, height);
  }

  onDispose() {
    this.disposed = true;
    clearTimeout(this.timer);
    this.request?.unsubscribe();
    this.themeObserver?.disconnect();
    this.resizeObserver?.disconnect();
    this.chart?.destroy();
    document.removeEventListener('visibilitychange', this.visibility);
  }
}
