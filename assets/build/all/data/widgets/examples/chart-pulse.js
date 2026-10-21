// Chart.js: sample data only; no module or device commands.
class ChartPulse extends ControllerInstance {
  settings = { defaultSize: '', sizeOptions: [] };
  chart = null;
  observer = null;
  alternate = false;
  disposed = false;
  active = false;
  visibility = () => this.sync();

  onCreate() {
    this.model().status = '';
    this.view().on('refresh:active', () => { this.active = true; this.sync(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.sync(); });
    document.addEventListener('visibilitychange', this.visibility);
    this.load().catch(error => {
      if (!this.disposed) this.model().status = 'Unable to load this demo: ' + error.message;
    });
  }

  canRun() { return !this.disposed && this.active && !document.hidden; }

  onInit() {
    this.declare({ compare: () => this.compare() });
  }

  async load() {
    this.model().period = 'Morning';
    const { default: Chart } = await import(new URL('assets/js/chartjs/chart.module.js', document.baseURI).href);
    if (this.disposed) return;
    const canvas = this.field('chart_canvas').get();
    const color = getComputedStyle(this.view().get()).getPropertyValue('--secondary-text-color').trim() || '#8b93a8';
    this.chart = new Chart(canvas, {
      type: 'bar',
      data: { labels: ['06', '07', '08', '09', '10', '11'], datasets: [{
        label: 'Sample energy (kWh)', data: [1.2, 2.1, 1.4, 3.2, 2.4, 1.8],
        backgroundColor: ['#7090ff', '#8d7cf8', '#ab77ed', '#c775cc', '#dc83ab', '#ec9a97'],
        borderRadius: 8, borderSkipped: false
      }] },
      options: {
        responsive: false, maintainAspectRatio: false, animation: false,
        locale: utils.preferences.ui.language || navigator.language,
        plugins: { legend: { display: false } },
        scales: { x: { grid: { display: false }, ticks: { color } },
          y: { beginAtZero: true, grid: { color: '#8b93a820' }, ticks: { color } } }
      }
    });
    this.observer = new ResizeObserver(() => { if (this.canRun()) this.resize(); });
    this.observer.observe(this.field('chart_area').get());
    this.expose('compare', () => this.compare());
    this.sync();
  }

  resize() {
    const box = this.field('chart_area').get().getBoundingClientRect();
    if (box.width > 0 && box.height > 0) this.chart.resize(box.width, box.height);
  }

  sync() {
    if (!this.chart) return;
    if (this.canRun()) this.resize();
    else this.chart.stop();
  }

  compare() {
    if (!this.chart || !this.canRun()) return;
    this.alternate = !this.alternate;
    this.model().period = this.alternate ? 'Evening' : 'Morning';
    this.chart.data.labels = this.alternate ? ['18', '19', '20', '21', '22', '23'] : ['06', '07', '08', '09', '10', '11'];
    this.chart.data.datasets[0].data = this.alternate ? [2.4, 3.8, 2.6, 1.9, 1.2, .7] : [1.2, 2.1, 1.4, 3.2, 2.4, 1.8];
    this.chart.update('none');
    this.trigger('period-changed', { period: this.model().period });
  }

  onDispose() {
    this.disposed = true;
    document.removeEventListener('visibilitychange', this.visibility);
    this.observer?.disconnect();
    this.chart?.destroy();
    this.chart = null;
  }
}
