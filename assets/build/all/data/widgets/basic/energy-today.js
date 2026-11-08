class EnergyToday extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      energy: {
        label: 'Power meter',
        labelKey: 'meter',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts,EnergyMonitor.WattCounter.Today',
        includeDomains: 'HomeAutomation.EnergyMonitor'
      }
    }
  };

  labels = {
    title: 'Energy today',
    today: 'Since midnight',
    now: 'Now',
    peak: '24 h peak',
    history: 'Power over the last 24 hours',
    estimated: 'Estimated',
    partial: 'Partial estimate',
    no_meter: 'Choose a power meter in the widget settings.',
    offline: 'Power meter offline',
    no_history: 'No power readings in the last 24 hours.',
    unavailable: 'Power history is unavailable.',
    loading: 'Loading power history…'
  };
  powerKeys = ['EnergyMonitor.WattLoad', 'Meter.Watts'];
  counterKey = 'EnergyMonitor.WattCounter.Today';
  halfHour = 30 * 60 * 1000;
  active = true;
  disposed = false;
  history = [];
  visibility = () => this.sync();

  onCreate() {
    this.meter = this.getBoundModule('energy');
    this.applyLabels();
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.applyLabels();
      });
    }
    if (!this.meter) return;

    this.subscribe(this.meter, () => {
      this.render();
      if (this.powerKey() !== this.requestedField) this.sync();
    });
    this.view().on('refresh:active', () => { this.active = true; this.sync(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.sync(); });
    document.addEventListener('visibilitychange', this.visibility);
    this.sync();
    this.createChart().catch(() => {
      if (this.disposed) return;
      this.chartFailed = true;
      this.render();
    });
  }

  powerKey() {
    return this.powerKeys.find(key => this.meter?.field(key));
  }

  readNumber(raw) {
    if (typeof raw !== 'number' && (typeof raw !== 'string' || !raw.trim())) return null;
    const value = typeof raw === 'string' ? Number(raw.replace(',', '.')) : raw;
    return Number.isFinite(value) && value >= 0 ? value : null;
  }

  summarize(points, now, midnight, livePower) {
    let peak = null;
    let wattMilliseconds = 0;
    let covered = 0;
    let first = null;
    for (let index = 0; index < points.length; index++) {
      const {x, y} = points[index];
      peak = peak === null ? y : Math.max(peak, y);
      // Hold a reading until the next change. Only extend the latest reading
      // to now when it still matches the meter's live value.
      const nextTime = points[index + 1]?.x ?? (livePower === y ? now : x);
      const from = Math.max(x, midnight);
      const duration = Math.min(nextTime, now) - from;
      if (duration > 0) {
        first ??= from;
        covered += duration;
        wattMilliseconds += y * duration;
      }
    }
    return {
      peak,
      estimate: covered ? wattMilliseconds / 3_600_000_000 : null,
      partial: first !== midnight || covered < now - midnight
    };
  }

  applyLabels() {
    const model = this.model();
    for (const key of ['title', 'today', 'now', 'peak', 'history']) model[key] = this.labels[key];
    this.field('chart_canvas').get().setAttribute('aria-label', this.labels.history);
    this.render();
  }

  format(value, digits) {
    if (value === null) return '—';
    const locale = utils.preferences?.ui?.language || navigator.language;
    return value.toLocaleString(locale, {maximumFractionDigits: digits});
  }

  render() {
    const now = Date.now();
    const midnight = new Date(now).setHours(0, 0, 0, 0);
    const model = this.model();
    const online = this.meter?.isOnline !== false;
    const counterField = this.meter?.field(this.counterKey);
    const counter = online ? this.readNumber(counterField?.value) : null;
    const key = this.powerKey();
    const power = online && key ? this.readNumber(this.meter.field(key)?.value) : null;
    const {peak, estimate, partial} = this.summarize(this.history, now, midnight, power);
    model.meter_name = this.meter?.name ?? '';
    model.power = this.format(power, 0);
    model.maximum = this.format(peak, 0);
    model.energy = this.format(counter !== null ? counter / 1000 : estimate, 3);
    model.estimate = counter === null && estimate !== null ? this.labels[partial ? 'partial' : 'estimated'] : '';
    model.status = !this.meter ? this.labels.no_meter
      : !online ? this.labels.offline
      : this.failed || this.chartFailed ? this.labels.unavailable
      : this.loading && !this.history.length ? this.labels.loading
      : !this.history.length ? this.labels.no_history : '';
  }

  sync() {
    clearTimeout(this.timer);
    if (this.disposed || !this.active || document.hidden) return;
    this.refreshHistory();
    this.resizeChart();
    this.timer = setTimeout(() => this.sync(), 60_000);
  }

  refreshHistory() {
    const key = this.powerKey();
    if (this.loading && key === this.requestedField) return;
    if (key !== this.requestedField) {
      this.request?.unsubscribe();
      this.history = [];
      this.drawChart();
      this.range = undefined;
      this.lastRequestAt = undefined;
      this.loading = false;
      this.failed = false;
      this.requestedField = key;
    }
    if (!key || this.meter.isOnline === false) {
      this.render();
      this.drawChart();
      return;
    }

    const end = Date.now();
    if (end >= this.lastRequestAt && end - this.lastRequestAt < 60_000) return;
    const start = end - 48 * this.halfHour;
    const since = this.range && this.range.end <= end ? Math.max(start, this.range.end) : start;
    this.lastRequestAt = end;
    this.loading = true;
    this.render();
    const finish = (rows, failed) => {
      if (this.disposed) return;
      this.loading = false;
      this.failed = failed;
      if (!failed) {
        const readings = new Map(this.history.filter(point => point.x >= start && point.x <= end)
          .map(({x, y}) => [x, y]));
        for (const row of rows) {
          const time = this.readNumber(row?.x);
          const power = this.readNumber(row?.y);
          if (time !== null && time >= since && time <= end && power !== null) readings.set(time, power);
        }
        this.history = Array.from(readings, ([x, y]) => ({x, y})).sort((a, b) => a.x - b.x);
        this.range = {start, end};
        this.drawChart();
      }
      this.render();
    };
    const result = this.meter.control('Statistics.Field.GetData', {
      field: key, rangeStart: since, rangeEnd: end, chartWidth: 480
    });
    if (!result) {
      finish([], true);
      return;
    }
    this.request = result.subscribe({
      next: rows => finish(Array.isArray(rows) ? rows : [], !Array.isArray(rows)),
      error: () => finish([], true)
    });
  }

  async createChart() {
    const {default: Chart} = await import(new URL('assets/js/chartjs/chart.module.js', document.baseURI).href);
    if (this.disposed) return;
    const canvas = this.field('chart_canvas').get();
    const style = getComputedStyle(canvas);
    const textColor = style.getPropertyValue('--secondary-text-color').trim() || '#8794a6';
    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {datasets: [{
        data: [], backgroundColor: '#6089c9', borderRadius: 2, barPercentage: .9,
        categoryPercentage: .9
      }]},
      options: {
        responsive: false, maintainAspectRatio: false, animation: false, parsing: false,
        plugins: {
          legend: {display: false},
          tooltip: {callbacks: {
            title: items => new Date(items[0].parsed.x).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'}),
            label: item => this.format(item.parsed.y, 0) + ' W'
          }}
        },
        scales: {
          x: {type: 'linear', grid: {display: false}, ticks: {
            color: textColor, maxTicksLimit: 5,
            callback: time => new Date(time).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})
          }},
          y: {beginAtZero: true, grid: {color: '#8794a625'}, ticks: {color: textColor, maxTicksLimit: 4}}
        }
      }
    });
    this.observer = new ResizeObserver(() => this.resizeChart());
    this.observer.observe(this.field('chart_area').get());
    this.drawChart();
    this.resizeChart();
  }

  drawChart() {
    if (!this.chart || !this.range) return;
    const {start, end} = this.range;
    const buckets = Array.from({length: 48}, (_, index) => ({x: start + (index + .5) * this.halfHour, y: null}));
    for (const {x, y} of this.history) {
      const index = Math.min(47, Math.floor((x - start) / this.halfHour));
      if (buckets[index].y === null || y > buckets[index].y) buckets[index].y = y;
    }
    this.chart.data.datasets[0].data = buckets;
    this.chart.options.scales.x.min = start;
    this.chart.options.scales.x.max = end;
    this.chart.update('none');
  }

  resizeChart() {
    if (!this.chart || !this.active || document.hidden) return;
    const {width, height} = this.field('chart_area').get().getBoundingClientRect();
    if (width > 0 && height > 0) this.chart.resize(width, height);
  }

  onDispose() {
    this.disposed = true;
    clearTimeout(this.timer);
    this.request?.unsubscribe();
    this.observer?.disconnect();
    this.chart?.destroy();
    document.removeEventListener('visibilitychange', this.visibility);
  }
}
