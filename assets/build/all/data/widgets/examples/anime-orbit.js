// Anime.js v4: animate actual elements inside this widget's Shadow DOM.
class AnimeOrbit extends ControllerInstance {
  settings = { defaultSize: '', sizeOptions: [] };
  animation = null;
  moving = true;
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
    this.declare({ toggleMotion: () => this.toggleMotion() });
  }

  async load() {
    this.model().action = 'Pause';
    const { animate, stagger } = await import(new URL('assets/js/animejs/anime.esm.min.js', document.baseURI).href);
    if (this.disposed) return;
    this.moving = !matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.model().action = this.moving ? 'Pause' : 'Play';
    this.animation = animate(Array.from(this.view().get().querySelectorAll('.orb')), {
      translateY: [-22, 22], scale: [.75, 1.15], rotate: [0, 180],
      duration: 1600, delay: stagger(180), alternate: true, loop: true,
      ease: 'inOutSine', autoplay: false
    });
    this.expose('toggleMotion', () => this.toggleMotion());
    this.sync();
  }

  sync() {
    if (this.canRun() && this.moving) this.animation?.resume();
    else this.animation?.pause();
  }

  toggleMotion() {
    if (this.disposed) return;
    this.moving = !this.moving;
    this.model().action = this.moving ? 'Pause' : 'Play';
    this.sync();
    this.trigger('motion-changed', { playing: this.moving });
  }

  onDispose() {
    this.disposed = true;
    document.removeEventListener('visibilitychange', this.visibility);
    this.animation?.revert();
    this.animation = null;
  }
}
