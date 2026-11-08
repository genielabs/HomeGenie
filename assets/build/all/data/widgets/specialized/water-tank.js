class WaterTank extends ControllerInstance {
  settings = {
    moduleSelect: {fieldFilter: 'Sensor.WaterLevel'},
    defaultSize: 'medium',
    sizeOptions: ['small', 'medium', 'big']
  };

  labels = {title: 'Water tank', preview: 'Preview', unavailable: 'Reading unavailable'};
  active = true;
  inView = false;
  disposed = false;

  onCreate() {
    this.tank = this.field('tank_graphic').get();
    this.visibilityChanged = () => this.syncMotion();
    document.addEventListener('visibilitychange', this.visibilityChanged);
    this.view().on('refresh:active', () => { this.active = true; this.render(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.syncMotion(); });
    if (typeof IntersectionObserver === 'function') {
      this.observer = new IntersectionObserver(([entry]) => {
        this.inView = entry.isIntersecting;
        this.syncMotion();
      });
      this.observer.observe(this.tank);
    } else {
      this.inView = true;
    }

    if (this.boundModule) {
      this.subscribe(this.boundModule, () => this.render());
    }
    this.render();
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || typeof value !== 'string' || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.render();
      });
    }
  }

  render() {
    if (this.disposed) return;
    const module = this.boundModule;
    const options = this.options();
    const preview = !module && !options.configuration?.module && !options.module;
    const value = module?.field('Sensor.WaterLevel')?.value;
    const numeric = typeof value === 'string' && value.trim() ? Number(value.replace(',', '.')) : value;
    const valid = module?.isOnline !== false && Number.isFinite(numeric) && numeric >= 0 && numeric <= 100;
    const level = preview ? 42 : valid ? numeric : null;
    const model = this.model();

    model.title = module?.name || this.labels.title;
    model.level_text = level === null ? '—' : Math.round(level) + '%';
    model.status_text = preview ? this.labels.preview : level === null ? this.labels.unavailable : '';
    this.tank.style.setProperty('--water-level-pct', (level ?? 0) + '%');
    this.hasSurface = level !== null && level > 0 && level < 100;
    this.tank.classList.toggle('has-surface', this.hasSurface);
    this.syncMotion();
  }

  syncMotion() {
    this.tank.classList.toggle('motion-active', this.hasSurface && this.active && this.inView && !document.hidden);
  }

  onDispose() {
    this.disposed = true;
    this.observer?.disconnect();
    document.removeEventListener('visibilitychange', this.visibilityChanged);
  }
}
