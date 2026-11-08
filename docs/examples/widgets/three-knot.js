// Three.js: a small procedural sculpture, no external models or textures.
class ThreeKnot extends ControllerInstance {
  settings = { defaultSize: '', sizeOptions: [] };
  renderer = null;
  observer = null;
  frame = null;
  lastTime = null;
  spinning = true;
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
    this.declare({ toggleSpin: () => this.toggleSpin() });
  }

  async load() {
    this.model().action = 'Pause';
    const THREE = await import(new URL('assets/js/three/three.module.js', document.baseURI).href);
    if (this.disposed) return;
    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    this.renderer.setPixelRatio(Math.min(devicePixelRatio || 1, 2));
    this.field('scene_area').get().appendChild(this.renderer.domElement);
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(38, 1, .1, 30);
    this.camera.position.z = 5;
    this.geometry = new THREE.TorusKnotGeometry(.85, .23, 100, 16);
    this.material = new THREE.MeshNormalMaterial();
    this.sculpture = new THREE.Mesh(this.geometry, this.material);
    this.scene.add(this.sculpture);
    this.spinning = !matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.model().action = this.spinning ? 'Pause' : 'Play';
    this.observer = new ResizeObserver(() => { if (this.canRun()) this.resize(); });
    this.observer.observe(this.field('scene_area').get());
    this.expose('toggleSpin', () => this.toggleSpin());
    this.sync();
  }

  resize() {
    const box = this.field('scene_area').get().getBoundingClientRect();
    if (box.width <= 0 || box.height <= 0) return;
    this.renderer.setSize(box.width, box.height);
    this.camera.aspect = box.width / box.height;
    this.camera.updateProjectionMatrix();
    this.renderer.render(this.scene, this.camera);
  }

  draw = time => {
    this.frame = null;
    if (!this.canRun() || !this.spinning) return;
    const dt = this.lastTime === null ? 0 : Math.min((time - this.lastTime) / 1000, .05);
    this.lastTime = time;
    this.sculpture.rotation.x += dt * .22;
    this.sculpture.rotation.y += dt * .4;
    this.renderer.render(this.scene, this.camera);
    this.frame = requestAnimationFrame(this.draw);
  };

  sync() {
    if (!this.renderer) return;
    if (this.canRun()) this.resize();
    if (this.canRun() && this.spinning) {
      if (this.frame === null) this.frame = requestAnimationFrame(this.draw);
    } else {
      if (this.frame !== null) cancelAnimationFrame(this.frame);
      this.frame = null;
      this.lastTime = null;
    }
  }

  toggleSpin() {
    if (this.disposed) return;
    this.spinning = !this.spinning;
    this.model().action = this.spinning ? 'Pause' : 'Play';
    this.sync();
    this.trigger('motion-changed', { playing: this.spinning });
  }

  onDispose() {
    this.disposed = true;
    this.sync();
    document.removeEventListener('visibilitychange', this.visibility);
    this.observer?.disconnect();
    this.geometry?.dispose();
    this.material?.dispose();
    this.renderer?.dispose();
    this.renderer?.forceContextLoss();
    this.renderer?.domElement.remove();
    this.renderer = null;
  }
}
