class SecurityCameraView extends ControllerInstance {
  settings = {defaultSize: 'big', sizeOptions: ['medium', 'big'], moduleSelect: {typeFilter: 'VideoInput'}};

  labels = {
    name: 'Camera', not_set: 'Not set', waiting: 'Waiting', live: 'Live',
    offline: 'No video signal', disabled: 'Disabled', fullscreen: 'Fullscreen',
    fullscreen_exit: 'Exit Fullscreen', recordings: 'Recordings'
  };

  active = true;
  visible = true;
  disposed = false;
  state = 'waiting';

  onInit() {
    this.declare({toggleFullscreen: () => this.toggleFullscreen()});
  }

  onCreate() {
    this.module = this.boundModule;
    this.card = this.element('card');
    this.image = this.element('image');
    this.doc = this.card.ownerDocument;
    this.onVisibility = () => this.sync();
    this.onFullscreen = () => this.render();
    this.doc.addEventListener('visibilitychange', this.onVisibility);
    this.doc.addEventListener('fullscreenchange', this.onFullscreen);
    this.view().on('refresh:active', () => { this.active = true; this.sync(); });
    this.view().on('refresh:inactive', () => { this.active = false; this.sync(); });
    if (typeof IntersectionObserver === 'function') {
      this.visible = false;
      this.observer = new IntersectionObserver(([entry]) => {
        this.visible = entry.isIntersecting;
        this.sync();
      });
      this.observer.observe(this.card);
    }
    if (this.module) this.subscribe(this.module, ({key}) => {
      if (key === 'VideoInput.Disable' || key === 'VideoInput.SourceURL' || key === 'IpCamera.ImageURL') this.sync();
      if (key === 'NVR.Status.Recording') this.render();
    });
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || !value || value === key || value === 'WIDGETS.' + key) return;
        this.labels[key] = value;
        this.render();
      });
    }
    this.sync();
  }

  element(name) { return this.field(name).get(); }

  get disabled() {
    return this.module?.field('VideoInput.Disable')?.value === 'On';
  }

  get running() {
    return this.module && !this.disabled && this.active && this.visible && !this.doc.hidden && !this.disposed;
  }

  get fullscreen() {
    return (this.card.getRootNode().fullscreenElement ?? this.doc.fullscreenElement) === this.card;
  }

  render() {
    const name = this.module?.name || this.labels.name;
    this.element('name').textContent = name;
    this.image.alt = name;
    this.element('status').textContent = this.labels[!this.module ? 'not_set' : this.disabled ? 'disabled' : this.state];
    this.card.toggleAttribute('data-live', this.state === 'live' && !this.disabled);
    this.image.hidden = !this.hasFrame || this.disabled;
    this.element('placeholder').hidden = !this.image.hidden;
    const recording = this.element('recording');
    recording.hidden = String(this.module?.field('NVR.Status.Recording')?.value) !== '1';
    recording.setAttribute('aria-label', this.labels.recordings);
    const expand = this.element('expand');
    expand.hidden = !this.hasFrame || this.disabled || !this.doc.fullscreenEnabled;
    expand.title = this.labels[this.fullscreen ? 'fullscreen_exit' : 'fullscreen'];
    expand.setAttribute('aria-label', expand.title);
    this.element('expand_icon').textContent = this.fullscreen ? 'fullscreen_exit' : 'fullscreen';
  }

  async toggleFullscreen() {
    try {
      if (this.fullscreen) await this.doc.exitFullscreen();
      else if (!this.doc.fullscreenElement && this.hasFrame) await this.card.requestFullscreen();
    } catch {
      // A client may disallow fullscreen in its WebView or frame.
    }
  }

  sync() {
    clearTimeout(this.refreshTimer);
    if (!this.running) {
      this.cancelFrame();
      this.state = 'waiting';
      this.render();
      return;
    }
    if (!this.request) this.loadFrame();
  }

  loadFrame() {
    if (!this.running || this.request) return;
    const request = {};
    this.request = request;
    const finish = ok => {
      if (this.request !== request) return;
      this.cancelFrame();
      if (ok) this.hasFrame = true;
      this.state = ok ? 'live' : 'offline';
      this.render();
      if (this.running) this.refreshTimer = setTimeout(() => this.loadFrame(), ok ? 1000 : 3000);
    };
    request.timeout = setTimeout(() => finish(false), 8000);
    try {
      const stream = this.module.control('Camera.GetPicture');
      if (!stream) return finish(false);
      request.subscription = stream.subscribe({
        next: result => {
          if (this.request !== request) return;
          const jpeg = typeof result === 'string' ? result : result?.response;
          if (typeof jpeg !== 'string' || !jpeg.startsWith('/9j/')) return finish(false);
          const source = 'data:image/jpeg;base64,' + jpeg;
          if (this.image.src === source && this.image.complete && this.image.naturalWidth > 0) return finish(true);
          this.image.onload = () => finish(true);
          this.image.onerror = () => finish(false);
          this.image.src = source;
        },
        error: () => finish(false)
      });
      if (this.request !== request) request.subscription.unsubscribe();
    } catch {
      finish(false);
    }
  }

  cancelFrame() {
    const request = this.request;
    this.request = undefined;
    clearTimeout(request?.timeout);
    request?.subscription?.unsubscribe();
    this.image.onload = null;
    this.image.onerror = null;
  }

  onDispose() {
    this.disposed = true;
    clearTimeout(this.refreshTimer);
    this.cancelFrame();
    this.observer?.disconnect();
    this.doc.removeEventListener('visibilitychange', this.onVisibility);
    this.doc.removeEventListener('fullscreenchange', this.onFullscreen);
  }
}
