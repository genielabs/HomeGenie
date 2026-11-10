// A procedural room dashboard with a local LED demo and optional real device bindings.
class IsometricRoom extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard', sizeOptions: [],
    boundModules: {
      dashboardProgram: {
        label: 'Dashboard program', labelKey: 'bind_dashboard_program',
        typeFilter: 'program', fieldFilter: 'Dashboard.Theme.Isometric',
        defaultModule: 'HomeAutomation.HomeGenie.Automation:652'
      },
      light: {
        label: 'Light', labelKey: 'bind_light', typeFilter: 'Light,Dimmer,Color',
        defaultModule: 'HomeAutomation.PhilipsHue:1', defaultField: 'Simulator.Meter.Watts'
      },
      rgbLight: {label: 'RGB light', labelKey: 'bind_rgb', typeFilter: 'Color', fieldFilter: 'Status.ColorHsb'},
      climate: {label: 'Climate', labelKey: 'bind_climate', fieldFilter: 'Sensor.Temperature,Sensor.Humidity', defaultModule: 'HomeAutomation.Demo:1'},
      energy: {
        label: 'Power', labelKey: 'bind_power',
        fieldFilter: 'EnergyMonitor.WattLoad,Meter.Watts',
        includeDomains: 'HomeAutomation.EnergyMonitor',
        defaultModule: 'HomeAutomation.EnergyMonitor:1'
      }
    }
  };
  // English defaults are available while the widget catalog loads.
  labels = {
    "eyebrow": "YOUR SPACE, CONNECTED",
    "heading": "A room, in real time.",
    "subtitle": "A view of your home. A touch to control it.",
    "scene_tag": "The room",
    "scene_hint": "The lamp in the scene reflects the selected light.",
    "footer_note": "Choose your light and sensors in dashboard settings.",
    "scene_message": "Preparing the room…",
    "scene_aria": "Isometric room with an interactive lamp",
    "controls_aria": "Controls and sensors",
    "scene_unavailable": "The 3D view is unavailable. You can still use the controls.",
    "light_on": "On",
    "light_off": "Off",
    "light_not_selected": "Light not selected",
    "unknown_state": "Unknown state",
    "pause_animation": "Pause animation",
    "animate_room": "Animate room",
    "scene_paused": "3D view paused. Controls remain available.",
    "bind_rgb": "RGB light"
  };
  disposed = false;
  active = false;
  intersecting = true;
  motionRequested = false;
  animationsDisabled = false;
  get motion() { return this.motionRequested && !this.animationsDisabled; }
  frame = null;
  lastTime = null;
  phase = 0;
  renderer = null;
  geometries = new Set();
  materials = new Set();
  plantLeaves = [];
  markerPositions = [
    [-.02, 1.15, .56], // Coffee table (home)
    [2.9, 1.38, -2.31], // Sideboard
    [-2.32, 1.76, -1.67], // Above the sofa
    [-.75, .78, 2.45] // Near the front of the room
  ];
  markerPosition = 0;
  markerFlight = null;
  markerReturnTimer = null;
  matrixDemoOn = true;
  matrixEffect = 'waves';
  matrixPhase = 0;
  matrixColor = [81, 222, 209];

  onInit() {
    this.declare({toggleMotion: () => this.toggleMotion(), toggleLight: () => this.toggleLight()});
  }

  onCreate() {
    this.applyLabels();
    this.light = this.getBoundModule('light');
    this.energy = this.getBoundModule('energy');
    this.rgbLight = this.getBoundModule('rgbLight');
    this.bindMatrixControls();
    Promise.all([
      import('/widgets/assistants/genie.module.js'),
      import('/widgets/isometric/light.module.js'),
      import('/widgets/isometric/led-panel.module.js'),
      import('/widgets/isometric/climate.module.js'),
      import('/widgets/isometric/power.module.js')
    ]).catch(error => console.error('Could not load dashboard widgets', error));
    this.renderData();
    for (const module of new Set([this.light, this.energy].filter(Boolean)))
      this.subscribe(module, () => { if (!this.disposed) this.renderData(); });

    this.reduce = matchMedia('(prefers-reduced-motion: reduce)');
    this.motionRequested = !this.reduce.matches;
    this.motionChanged = event => { if (event.matches) this.motionRequested = false;this.updateMotionLabel();this.sync(); };
    this.bindDashboardProgram();
    this.reduce.addEventListener?.('change', this.motionChanged);
    this.visibility = () => this.sync();
    document.addEventListener('visibilitychange', this.visibility);
    this.view().on('refresh:active', () => {
      this.active = true;
      this.bindDashboardProgram();
      this.renderData();
      this.sync();
    });
    this.view().on('refresh:inactive', () => {this.active = false;this.sync();});
    this.updateMotionLabel();
    this.loadLabels();
    this.loadScene().catch(error => {
      if (this.disposed) return;
      this.releaseScene();
      this.showSceneMessage('scene_unavailable');
      this.field('scene_fallback').get().hidden = false;
    });
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
          this.updateMotionLabel();
          this.renderData();
        }
      });
    }
  }

  applyLabels() {
    const labels = this.labels, model = this.model();
    for (const key of ['scene_tag', 'scene_hint', 'footer_note']) model[key] = labels[key];
    this.renderHeadings();
    this.showSceneMessage(this.sceneMessage || 'scene_message');
    this.field('scene_area').get().setAttribute('aria-label', labels.scene_aria);
    this.field('controls_area').get().setAttribute('aria-label', labels.controls_aria);
  }

  renderHeadings() {
    for (const [field, option] of [['eyebrow', 'Eyebrow'], ['heading', 'Title'], ['subtitle', 'Subtitle']]) {
      const text = this.dashboardProgram?.field('Dashboard.Isometric.' + option)?.value;
      this.field(field).get().textContent = String(text ?? '').trim() || this.labels[field];
    }
  }

  showSceneMessage(key) {
    this.sceneMessage = key;
    this.model().scene_message = this.labels[key];
  }

  numeric(module, key) {
    if (!module || module.isOnline === false) return null;
    const field = module.field(key), raw = field?.decimalValue ?? field?.value;
    if (raw == null || typeof raw === 'string' && !raw.trim()) return null;
    const number = typeof raw === 'string' ? Number(raw.replace(',', '.')) : raw;
    return typeof number === 'number' && Number.isFinite(number) ? number : null;
  }

  renderData() {
    const level = this.numeric(this.light, 'Status.Level');
    this.isOn = level === null ? null : level > 0;
    const field = this.energy?.field('EnergyMonitor.WattLoad') ? 'EnergyMonitor.WattLoad' : 'Meter.Watts';
    this.watts = this.numeric(this.energy, field);
    this.model().scene_state = this.labels[this.isOn === null
      ? (this.light ? 'unknown_state' : 'light_not_selected')
      : (this.isOn ? 'light_on' : 'light_off')];
    this.field('scene_dot').get().style.background = this.isOn === null ? '#8a9da8' : this.isOn ? '#f3d382' : '#9be5cf';
    this.updateSceneState();
  }

  toggleLight() {
    this.field('light_widget').get().dispatchEvent(new Event('isometric:toggle-light'));
  }

  bindDashboardProgram() {
    const program = this.getBoundModule('dashboardProgram');
    if (program === this.dashboardProgram) return;
    this.dashboardProgramSubscription?.unsubscribe();
    this.dashboardProgram = program;
    this.dashboardProgramSubscription = program && this.subscribe(program, field => {
      if (field?.key === 'Dashboard.AnimationsDisabled') this.syncMotionSetting();
      if (field?.key?.startsWith('Dashboard.Isometric.')) this.renderHeadings();
    });
    this.renderHeadings();
    this.syncMotionSetting();
  }

  syncMotionSetting() {
    const value = this.dashboardProgram?.field('Dashboard.AnimationsDisabled')?.value;
    this.animationsDisabled = /^(on|true|1)$/i.test(String(value ?? ''));
    this.updateMotionLabel();
    this.sync();
  }

  updateMotionLabel() {
    this.model().motion_label = this.motion ? this.labels.pause_animation : this.labels.animate_room;
    this.model().motion_icon = this.motion ? 'pause' : 'play_arrow';
    const button = this.field('motion_toggle').get();
    button.setAttribute('aria-label', this.model().motion_label);
    button.setAttribute('aria-pressed', String(this.motion));
    button.disabled = this.animationsDisabled;
  }
  toggleMotion() {
    if (this.disposed || this.animationsDisabled) return;
    this.motionRequested = !this.motionRequested;
    this.updateMotionLabel();
    this.sync();
  }
  canRender() { return !this.disposed && this.active && this.intersecting && !document.hidden && !this.contextLost; }

  async loadScene() {
    const THREE = await import(new URL('assets/js/three/three.module.js', document.baseURI).href);
    if (this.disposed) return;
    this.THREE = THREE;
    this.renderer = new THREE.WebGLRenderer({alpha: true, antialias: true, powerPreference: 'low-power'});
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.3;
    const area = this.field('scene_area').get(), canvas = this.renderer.domElement;
    this.sceneGenie = this.field('scene_genie').get();
    this.genieAnchor = new THREE.Vector3(3.13, 1.55, -2.04);
    this.genieProjection = new THREE.Vector3();
    canvas.setAttribute('aria-hidden', 'true');area.appendChild(canvas);
    this.scene = new THREE.Scene();
    this.camera = new THREE.OrthographicCamera(-6, 6, 4.5, -4.5, .1, 80);
    this.camera.position.set(9, 8.5, 11);this.camera.lookAt(0, .65, 0);
    this.scene.add(new THREE.HemisphereLight(0xe4fff8, 0x3b465e, 2.2));
    const sun = new THREE.DirectionalLight(0xffe4bd, 3.2);sun.position.set(0, 8, 5);this.scene.add(sun);
    const rim = new THREE.DirectionalLight(0x72dcd2, 1.5);rim.position.set(-5, 3, -4);this.scene.add(rim);
    this.buildRoom();
    this.updateMatrixPixels();
    this.roomBounds = new THREE.Box3().setFromObject(this.scene);
    this.frameRoom();
    this.bindSceneControls(canvas, area);
    this.lost = event => {
      event.preventDefault();this.contextLost = true;this.sync();
      this.showSceneMessage('scene_paused');
      this.field('scene_fallback').get().hidden = false;
    };
    this.restored = () => {this.contextLost = false;this.field('scene_fallback').get().hidden = true;this.sync();};
    canvas.addEventListener('webglcontextlost', this.lost);
    canvas.addEventListener('webglcontextrestored', this.restored);
    this.resizeObserver = new ResizeObserver(() => {if (this.canRender()) this.resize();});this.resizeObserver.observe(area);
    this.intersectionObserver = new IntersectionObserver(entries => {
      this.intersecting = entries.some(entry => entry.isIntersecting);this.sync();
    }, {threshold: 0});this.intersectionObserver.observe(area);
    this.field('scene_fallback').get().hidden = true;
    this.updateSceneState();this.sync();
  }

  bindSceneControls(canvas, area) {
    const controls = this.sceneControls = new AbortController();
    const {signal} = controls;
    const raycaster = new this.THREE.Raycaster();
    const pointer = new this.THREE.Vector2();
    const targets = [this.markerHit, ...this.lampParts, this.matrixHit];
    let drag, dragged = false;

    const pick = event => {
      if (!this.canRender()) return;
      const box = canvas.getBoundingClientRect();
      if (!box.width || !box.height) return;
      const x = (event.clientX - box.left) / box.width;
      const y = (event.clientY - box.top) / box.height;
      if (x < 0 || x > 1 || y < 0 || y > 1) return;
      pointer.set(x * 2 - 1, 1 - y * 2);
      raycaster.setFromCamera(pointer, this.camera);
      return raycaster.intersectObjects(targets, false)[0]?.object;
    };
    const updateCursor = event => canvas.classList.toggle('is-pointing', !!pick(event));
    canvas.addEventListener('pointerenter', updateCursor, {signal});
    canvas.addEventListener('pointerleave', () => canvas.classList.remove('is-pointing'), {signal});

    canvas.addEventListener('pointerdown', event => {
      if (!event.isPrimary || event.button !== 0 || !this.canRender()) return;
      dragged = false;
      if (event.pointerType !== 'mouse') return;
      drag = {id: event.pointerId, x: event.clientX, y: event.clientY};
      canvas.setPointerCapture(event.pointerId);
    }, {signal});
    canvas.addEventListener('pointermove', event => {
      if (drag?.id !== event.pointerId) {
        updateCursor(event);
        return;
      }
      if (!this.canRender()) return;
      const dx = event.clientX - drag.x;
      if (!dragged && Math.hypot(dx, event.clientY - drag.y) < 5) {
        updateCursor(event);
        return;
      }
      dragged = true;
      canvas.classList.add('is-orbiting');
      drag.x = event.clientX;
      this.orbitRoom(dx);
    }, {signal});
    const endDrag = event => {
      if (drag?.id !== event.pointerId) return;
      drag = undefined;
      canvas.classList.remove('is-orbiting');
      if (event.type === 'pointerup') updateCursor(event);
      else canvas.classList.remove('is-pointing');
    };
    for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) {
      canvas.addEventListener(type, endDrag, {signal});
    }
    area.addEventListener('wheel', event => {
      if (!this.canRender() || event.ctrlKey || !event.deltaY) return;
      const units = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? this.height : 1;
      const zoom = this.THREE.MathUtils.clamp(this.camera.zoom * Math.exp(-event.deltaY * units * .0015), .8, 1.8);
      if (zoom === this.camera.zoom) return;
      event.preventDefault();
      this.camera.zoom = zoom;
      this.camera.updateProjectionMatrix();
      this.positionGenie();
      this.renderOnce();
      if (!drag) updateCursor(event);
    }, {signal, passive: false});
    canvas.addEventListener('click', event => {
      if (dragged || event.button !== 0) return;
      const hit = pick(event);
      if (!hit) return;
      if (hit === this.markerHit) this.hopMarker();
      else if (hit === this.matrixHit) this.toggleMatrix();
      else this.toggleLight();
    }, {signal});
  }

  orbitRoom(dx) {
    const position = this.camera.position;
    const angle = Math.atan2(position.x, position.z);
    // Stay on the open side of the two walls.
    const nextAngle = this.THREE.MathUtils.clamp(angle - dx * .006, Math.PI / 12, 5 * Math.PI / 12);
    if (nextAngle === angle) return;
    const radius = Math.hypot(position.x, position.z);
    position.x = radius * Math.sin(nextAngle);
    position.z = radius * Math.cos(nextAngle);
    this.camera.lookAt(0, .65, 0);
    this.frameRoom();
    this.updateCameraProjection();
    this.positionGenie();
    this.renderOnce();
  }

  frameRoom() {
    this.camera.updateMatrixWorld();
    const bounds = this.roomBounds.clone().applyMatrix4(this.camera.matrixWorldInverse);
    this.roomFrame = {x: (bounds.min.x + bounds.max.x) / 2, y: (bounds.min.y + bounds.max.y) / 2,
      width: bounds.max.x - bounds.min.x, height: bounds.max.y - bounds.min.y};
  }

  bindMatrixControls() {
    this.matrixControls = new AbortController();
    const {signal} = this.matrixControls;
    const slot = this.field('matrix_slot').get();
    slot.addEventListener('led-panel-ready', event => {
      if (typeof event.detail?.setState !== 'function') return;
      this.updateMatrixPanel = event.detail.setState;
      this.renderMatrix();
    }, {signal});
    slot.addEventListener('led-panel-toggle', () => this.toggleMatrix(), {signal});
    slot.addEventListener('led-panel-color', event => this.setMatrixColor(event.detail), {signal});
    slot.addEventListener('led-panel-effect', event => this.selectMatrixEffect(event.detail), {signal});
    if (this.rgbLight) this.subscribe(this.rgbLight, field => {
      if (field?.key === 'Status.Level' || field?.key === 'Status.ColorHsb') this.renderMatrix();
    });
    this.renderMatrix();
  }

  async selectMatrixEffect(effect) {
    if (this.disposed || this.rgbLight || !['waves', 'solid', 'ghost', 'heart'].includes(effect)) return;
    this.matrixEffect = effect;
    this.matrixPhase = 0;
    this.matrixScript = null;
    this.matrixFeedback = '';
    this.renderMatrix();
    if (effect !== 'ghost' && effect !== 'heart') return;
    try {
      const {createMatrixScript} = await import(new URL('/widgets/isometric/matrix-demo/matrix-scripts.js', document.baseURI).href);
      if (this.disposed || this.matrixEffect !== effect) return;
      this.matrixScript = createMatrixScript(effect);
      if (effect === 'heart') {
        const hsb = this.matrixRgbToHsb(this.matrixColor);
        this.matrixScript.update(20, ...hsb);
        this.matrixScript.update(120, ...hsb);
      }
      this.renderMatrix();
    } catch {
      if (this.disposed || this.matrixEffect !== effect) return;
      this.matrixEffect = 'waves';
      this.matrixFeedback = 'matrix_load_error';
      this.renderMatrix();
    }
  }

  matrixDeviceState() {
    const level = this.numeric(this.rgbLight, 'Status.Level');
    const raw = this.rgbLight.field('Status.ColorHsb')?.value;
    const values = typeof raw === 'string'
      ? raw.split(',').slice(0, 3).map(value => value.trim() ? Number(value) : NaN) : [];
    const rgb = values.length === 3 && values.every(Number.isFinite)
      ? this.matrixHsbToRgb(values[0], this.matrixClamp(values[1]), this.matrixClamp(level ?? values[2]))
      : null;
    return {on: level === null ? null : level > 0, rgb};
  }

  renderMatrix() {
    const state = this.rgbLight ? this.matrixDeviceState() : {on: this.matrixDemoOn, rgb: this.matrixColor};
    this.matrixState = state;
    if (this.matrixCommand?.matches(state)) this.finishMatrixCommand();
    this.updateMatrixPanel?.({
      on: state.on,
      source: this.rgbLight?.name,
      demo: !this.rgbLight,
      effect: this.rgbLight ? null : this.matrixEffect,
      color: state.rgb && (!this.rgbLight || state.on)
        ? '#' + state.rgb.map(channel => channel.toString(16).padStart(2, '0')).join('') : undefined,
      colorDisabled: !this.canSetMatrixColor(),
      busy: !!this.matrixCommand,
      feedback: this.matrixFeedback
    });
    this.updateMatrixPixels();
    this.renderOnce();
  }

  toggleMatrix() {
    if (this.disposed || this.matrixCommand || this.matrixState?.on == null) return;
    const on = !this.matrixState.on;
    if (this.rgbLight) this.sendMatrixCommand(on ? 'Control.On' : 'Control.Off', undefined, state => state.on === on);
    else {
      this.matrixDemoOn = on;
      this.renderMatrix();
    }
  }

  setMatrixColor(hex) {
    if (this.disposed || !this.canSetMatrixColor() || typeof hex !== 'string' || !/^#[0-9a-f]{6}$/i.test(hex)) return;
    const rgb = [1, 3, 5].map(offset => parseInt(hex.slice(offset, offset + 2), 16));
    if (!this.rgbLight) {
      this.matrixColor = rgb;
      this.matrixDemoOn = true;
      this.renderMatrix();
      return;
    }
    const hsb = this.matrixRgbToHsb(rgb);
    this.sendMatrixCommand('Control.ColorHsb', hsb.join(','), state => hsb[2] === 0
      ? state.on === false : state.on === true && !!state.rgb && state.rgb.every((value, i) => Math.abs(value - rgb[i]) <= 12));
  }

  canSetMatrixColor() {
    return !this.rgbLight || !!(this.matrixState?.on && this.matrixState.rgb && !this.matrixCommand);
  }

  sendMatrixCommand(action, options, matches) {
    if (this.matrixCommand) return;
    if (matches(this.matrixState)) {
      this.renderMatrix();
      return;
    }
    const command = {matches};
    this.matrixCommand = command;
    this.matrixFeedback = '';
    this.renderMatrix();
    const fail = () => {
      if (this.disposed || this.matrixCommand !== command) return;
      this.finishMatrixCommand('matrix_failed');
      this.renderMatrix();
    };
    command.timer = setTimeout(fail, 8000);
    try {
      command.subscription = this.rgbLight.control(action, options).subscribe({next: receipt => {
        const status = receipt?.response?.Status;
        if (receipt?.code != null && (receipt.code < 200 || receipt.code >= 300)
          || status != null && status !== 0 && status !== '0' && status !== 'Ok') fail();
      }, error: fail});
      if (this.matrixCommand !== command) command.subscription?.unsubscribe();
    } catch { fail(); }
  }

  finishMatrixCommand(feedback = '') {
    const command = this.matrixCommand;
    this.matrixCommand = null;
    clearTimeout(command.timer);
    command.subscription?.unsubscribe();
    this.matrixFeedback = feedback;
  }

  matrixClamp(value) { return Math.min(1, Math.max(0, value)); }

  matrixHsbToRgb(h, s, v) {
    const channel = offset => {
      const k = ((offset + h * 6) % 6 + 6) % 6;
      return Math.round(255 * v * (1 - s * Math.max(0, Math.min(k, 4 - k, 1))));
    };
    return [channel(5), channel(3), channel(1)];
  }

  matrixRgbToHsb(rgb) {
    const [r, g, b] = rgb.map(value => value / 255);
    const max = Math.max(r, g, b), delta = max - Math.min(r, g, b);
    const hue = !delta ? 0 : max === r ? (g - b) / delta : max === g ? 2 + (b - r) / delta : 4 + (r - g) / delta;
    return [((hue / 6) + 1) % 1, max ? delta / max : 0, max];
  }

  buildMatrix(frameMaterial) {
    const T = this.THREE;
    this.matrixHit = this.box(frameMaterial, 2.55, 2.22, -2.84, 1.62, 1.62, .17);
    const pixels = new Uint8Array(64 * 64 * 4);
    for (let i = 0; i < pixels.length; i += 4) pixels.set([10, 16, 22, 255], i);
    this.matrixTexture = new T.DataTexture(pixels, 64, 64, T.RGBAFormat);
    this.matrixTexture.colorSpace = T.SRGBColorSpace;
    const display = new T.MeshBasicMaterial({map: this.matrixTexture, toneMapped: false});
    this.materials.add(display);
    this.mesh(this.geometry(new T.PlaneGeometry(1.49, 1.49)), display, 2.55, 2.22, -2.75);
    this.matrixGlow = new T.PointLight(0x51ded1, 0, 3.6, 2);
    this.matrixGlow.position.set(2.55, 2.22, -2.35);
    this.scene.add(this.matrixGlow);
  }

  advanceMatrix(dt) {
    this.matrixPhase += dt;
    if (this.matrixPhase - this.matrixFrameTime < 1 / 15) return;
    this.updateMatrixPixels();
  }

  updateMatrixPixels() {
    if (!this.matrixTexture || !this.matrixState) return;
    const dt = Math.max(0, this.matrixPhase - (this.matrixFrameTime ?? 0));
    this.matrixFrameTime = this.matrixPhase;
    const {on, rgb} = this.matrixState, data = this.matrixTexture.image.data;
    const demo = !this.rgbLight, hsb = this.matrixRgbToHsb(this.matrixColor);
    if (demo && on && this.matrixScript) {
      const pulse = this.matrixEffect === 'ghost' ? .85 + .15 * Math.cos(this.matrixPhase * 3.2) : 1;
      this.matrixScript.update(dt * 1000, hsb[0], hsb[1], hsb[2] * pulse);
    }
    const scripted = demo && this.matrixScript?.getPixels();
    const totals = [0, 0, 0];
    for (let row = 0; row < 16; row++) for (let col = 0; col < 16; col++) {
      let color = [0, 0, 0];
      if (on) {
        if (scripted) {
          const pixel = scripted[row * 16 + (row % 2 ? 15 - col : col)];
          color = this.matrixHsbToRgb(pixel.h, pixel.s, pixel.v);
        } else if (demo && this.matrixEffect === 'waves') {
          const wave = .5 + .5 * Math.sin(col * .53 + row * .35 - this.matrixPhase * 2.6);
          color = this.matrixHsbToRgb(hsb[0] + row * .022 + wave * .19, .87, (.2 + wave * .8) * hsb[2]);
        } else if (rgb) color = rgb;
      }
      for (let i = 0; i < 3; i++) totals[i] += color[i];
      for (let y = 0; y < 3; y++) for (let x = 0; x < 3; x++) {
        const offset = ((63 - row * 4 - y) * 64 + col * 4 + x) * 4;
        data[offset] = Math.max(12, color[0]);
        data[offset + 1] = Math.max(19, color[1]);
        data[offset + 2] = Math.max(26, color[2]);
      }
    }
    const peak = Math.max(...totals);
    if (peak > 0) this.matrixGlow.color.setRGB(...totals.map(channel => channel / peak), this.THREE.SRGBColorSpace);
    this.matrixGlow.intensity = 3.5 * peak / (256 * 255);
    this.matrixTexture.needsUpdate = true;
  }

  material(color, extra = {}) {
    const m = new this.THREE.MeshStandardMaterial({color, roughness: .78, metalness: .04, ...extra});
    this.materials.add(m);return m;
  }
  geometry(g) { this.geometries.add(g);return g; }
  mesh(geometry, material, x, y, z, parent = this.scene) {
    const m = new this.THREE.Mesh(geometry, material);m.position.set(x, y, z);parent.add(m);return m;
  }
  box(material, x, y, z, sx, sy, sz, parent) {
    const m = this.mesh(this.cube, material, x, y, z, parent);m.scale.set(sx, sy, sz);return m;
  }

  buildRoom() {
    const T = this.THREE;
    this.cube = this.geometry(new T.BoxGeometry(1, 1, 1));
    const wall = this.material(0x486575), floor = this.material(0x9f876d), wood = this.material(0xbba78b);
    const trim = this.material(0x678491), ivory = this.material(0xe9dfce), fabric = this.material(0xca947b);
    const dark = this.material(0x314955), brass = this.material(0xb29865, {metalness: .5, roughness: .35});
    this.box(dark, 0, -.19, 0, 8.25, .32, 6.25);
    this.box(floor, 0, .01, 0, 8, .12, 6);
    for (let n = 0; n < 14; n++) this.box(n % 2 ? wood : floor, -3.72 + n * .57, .083, 0, .55, .025, 5.96);
    this.box(wall, 0, 1.62, -3, 8.2, 3.25, .15);
    this.box(wall, -4, 1.62, 0, .15, 3.25, 6.12);
    this.box(trim, 0, .23, -2.89, 8.02, .18, .045);
    this.box(trim, -3.89, .23, 0, .045, .18, 5.92);
    this.box(ivory, -.4, .116, .65, 4.7, .035, 3.1);
    // Sofa, sideboard and a low coffee table.
    this.box(dark, -1.25, .33, -1.92, 3.5, .28, 1.3);
    this.box(fabric, -1.25, .61, -1.85, 3.55, .38, 1.45);
    this.box(fabric, -1.25, 1.04, -2.41, 3.55, .67, .35);
    this.box(fabric, -3.02, .88, -1.86, .3, .64, 1.45);
    this.box(fabric, .52, .88, -1.86, .3, .64, 1.45);
    for (let n = 0; n < 3; n++) this.box(ivory, -2.34 + n * 1.08, .83, -1.79, 1.02, .1, 1.05);
    this.box(this.material(0x7aaca9), -2.5, 1.07, -2.1, .55, .46, .2).rotation.z = -.15;
    this.box(this.material(0xd8c299), .02, 1.07, -2.1, .55, .46, .2).rotation.z = .15;
    this.box(wood, -.3, .66, .55, 2.15, .13, 1.15);
    for (const x of [-1.13, .53]) for (const z of [.16, .94]) this.box(dark, x, .36, z, .08, .55, .08);
    this.box(this.material(0x6a899d), -.85, .79, .55, .5, .1, .58);
    this.box(ivory, -.8, .865, .55, .5, .04, .58).rotation.y = .12;
    this.box(wood, 2.55, .58, -2.38, 1.9, .95, .65);
    this.box(dark, 2.55, .53, -2.02, 1.72, .72, .025);
    for (const x of [2.1, 3.02]) this.box(brass, x, .59, -1.995, .08, .08, .04);
    // Back-wall window and a framed LED panel above the sideboard.
    const glass = this.material(0x8bc8c3, {emissive: 0x5eaaa7, emissiveIntensity: .28});this.windowMaterial = glass;
    this.box(ivory, -1.05, 2.24, -2.88, 3.2, 1.44, .17);
    this.box(glass, -1.05, 2.24, -2.77, 3.02, 1.26, .035);
    this.box(ivory, -1.05, 2.24, -2.73, .07, 1.31, .07);
    this.box(ivory, -1.05, 2.24, -2.73, 3.08, .065, .07);
    this.buildMatrix(dark);
    // A standing lamp; its emission and pool of light come only from the bound device state.
    const cylinder = this.geometry(new T.CylinderGeometry(1, 1, 1, 20));
    this.mesh(cylinder, dark, 2.78, .16, .4).scale.set(.4, .11, .4);
    this.mesh(cylinder, brass, 2.78, 1.1, .4).scale.set(.035, 1.9, .035);
    this.lampShadeMaterial = this.material(0xe5d5b5, {emissive: 0xffbd6d, emissiveIntensity: 0});
    const shade = this.mesh(this.geometry(new T.CylinderGeometry(.28, .56, .55, 32, 1, true)), this.lampShadeMaterial, 2.78, 2.13, .4);
    this.lampBulb = this.mesh(this.geometry(new T.SphereGeometry(.13, 16, 12)), this.material(0xe1d8ba, {emissive: 0xffbf7b, emissiveIntensity: 0}), 2.78, 2.02, .4);
    this.lampParts = [shade, this.lampBulb];
    this.lampLight = new T.PointLight(0xffc07c, 0, 6, 2);this.lampLight.position.set(2.78, 1.93, .4);this.scene.add(this.lampLight);
    this.poolMaterial = new T.MeshBasicMaterial({color: 0xffc780, transparent: true, opacity: 0, depthWrite: false});this.materials.add(this.poolMaterial);
    const pool = this.mesh(this.geometry(new T.CircleGeometry(1.48, 48)), this.poolMaterial, 2.78, .115, .4);pool.rotation.x = -Math.PI / 2;
    // Plant and a small floating sensor marker.
    this.mesh(this.geometry(new T.CylinderGeometry(.32, .24, .55, 24)), ivory, -3.12, .39, 1.83);
    const green = this.material(0x76a894);
    const leaf = this.geometry(new T.SphereGeometry(1, 12, 8));
    for (let n = 0; n < 7; n++) {
      const angle = n * Math.PI * 2 / 7;
      const l = this.mesh(leaf, green, -3.12 + Math.cos(angle) * .25, .98 + (n % 3) * .14, 1.83 + Math.sin(angle) * .24);
      l.scale.set(.15, .56, .14);l.rotation.z = Math.cos(angle) * .45;l.rotation.x = Math.sin(angle) * .45;
      this.plantLeaves.push({mesh: l, x: l.rotation.x, z: l.rotation.z, phase: angle});
    }
    this.sensorMarker = new T.Group();
    this.sensorMarker.position.set(...this.markerPositions[0]);
    this.scene.add(this.sensorMarker);
    const mint = this.material(0x9be5cf, {emissive: 0x386e67, emissiveIntensity: .35});
    this.mesh(this.geometry(new T.OctahedronGeometry(.18)), mint, 0, .15, 0, this.sensorMarker);
    const ring = this.mesh(this.geometry(new T.TorusGeometry(.28, .015, 6, 32)), mint, 0, 0, 0, this.sensorMarker);ring.rotation.x = Math.PI / 2;
    this.markerMaterial = mint;
    // A larger hit area makes the small marker easier to tap on touchscreens.
    const hitMaterial = new T.MeshBasicMaterial({visible: false});
    this.materials.add(hitMaterial);
    this.markerHit = this.mesh(this.geometry(new T.SphereGeometry(.36, 12, 8)), hitMaterial, 0, .12, 0, this.sensorMarker);
  }

  hopMarker() {
    const next = this.markerPosition % (this.markerPositions.length - 1) + 1;
    this.moveMarker(next);
    clearTimeout(this.markerReturnTimer);
    this.markerReturnTimer = setTimeout(() => {
      this.markerReturnTimer = null;
      this.moveMarker(0);
    }, 5000);
  }

  moveMarker(index) {
    this.markerPosition = index;
    const position = this.sensorMarker.position;
    if (this.motion && this.canRender()) {
      this.markerFlight = {from: position.clone(), start: this.phase};
      return;
    }
    this.markerFlight = null;
    const [x, y, z] = this.markerPositions[index];
    position.set(x, y + Math.sin(this.phase * 1.4) * .18, z);
    this.renderOnce();
  }

  updateSceneState() {
    if (!this.renderer || !this.lampLight) return;
    const on = this.isOn === true;
    this.lampShadeMaterial.emissiveIntensity = on ? .95 : 0;
    this.lampBulb.material.emissiveIntensity = on ? 3 : 0;
    this.lampLight.intensity = on ? 32 : 0;
    this.poolMaterial.opacity = on ? .22 : 0;
    this.markerMaterial.emissiveIntensity = this.watts !== null && this.watts > 0 ? .65 : .25;
    this.renderOnce();
  }

  resize() {
    if (!this.renderer || !this.canRender()) return;
    const box = this.field('scene_area').get().getBoundingClientRect();
    const width = Math.floor(box.width), height = Math.floor(box.height);
    if (width < 1 || height < 1) return;
    const ratio = Math.min(window.devicePixelRatio || 1, 1.5, Math.sqrt(1500000 / (width * height)));
    if (this.width !== width || this.height !== height || this.pixelRatio !== ratio) {
      this.width = width;this.height = height;this.pixelRatio = ratio;
      this.renderer.setPixelRatio(ratio);this.renderer.setSize(width, height, false);
      this.updateCameraProjection();
    }
    this.positionGenie();
    this.renderOnce();
  }

  positionGenie() {
    if (!this.sceneGenie || !this.width || !this.height) return;
    const point = this.genieProjection.copy(this.genieAnchor).project(this.camera);
    this.sceneGenie.hidden = Math.abs(point.x) >= 1 || Math.abs(point.y) >= 1;
    if (this.sceneGenie.hidden) return;
    const size = this.THREE.MathUtils.clamp(
      this.height * this.camera.zoom * 1.16 / (this.camera.top - this.camera.bottom), 46, 114);
    this.sceneGenie.style.left = `${(point.x + 1) * 50}%`;
    this.sceneGenie.style.top = `${(1 - point.y) * 50}%`;
    this.sceneGenie.style.setProperty('--genie-size', `${size}px`);
  }

  updateCameraProjection() {
    if (!this.width || !this.height) return;
    const aspect = this.width / this.height, bounds = this.roomFrame;
    const span = Math.max(bounds.height, bounds.width / aspect) * 1.12;
    this.camera.left = bounds.x - span * aspect / 2;this.camera.right = bounds.x + span * aspect / 2;
    this.camera.top = bounds.y + span / 2;this.camera.bottom = bounds.y - span / 2;this.camera.updateProjectionMatrix();
  }

  renderOnce() { if (this.renderer && this.width > 0 && this.canRender()) this.renderer.render(this.scene, this.camera); }
  draw = time => {
    this.frame = null;
    if (!this.renderer || !this.canRender() || !this.motion) return;
    if (this.lastTime === null || time - this.lastTime >= 1000 / 30) {
      const dt = this.lastTime === null ? 0 : Math.min((time - this.lastTime) / 1000, .08);this.lastTime = time;this.phase += dt;
      const [x, y, z] = this.markerPositions[this.markerPosition];
      const hoverY = y + Math.sin(this.phase * 1.4) * .18;
      if (this.markerFlight) {
        const {from, start} = this.markerFlight;
        const progress = Math.min((this.phase - start) / .24, 1);
        const eased = progress * progress * (3 - 2 * progress);
        this.sensorMarker.position.set(
          from.x + (x - from.x) * eased,
          from.y + (hoverY - from.y) * eased + Math.sin(Math.PI * progress) * .26,
          from.z + (z - from.z) * eased
        );
        if (progress === 1) this.markerFlight = null;
      } else {
        this.sensorMarker.position.y = hoverY;
      }
      this.sensorMarker.rotation.y = this.phase * .8;
      for (const leaf of this.plantLeaves) {
        leaf.mesh.rotation.x = leaf.x + Math.sin(this.phase * 1.2 + leaf.phase) * .1;
        leaf.mesh.rotation.z = leaf.z + Math.sin(this.phase * .9 + leaf.phase) * .08;
      }
      this.windowMaterial.emissiveIntensity = .28 + Math.sin(this.phase * .7) * .10;
      if (!this.rgbLight && this.matrixDemoOn && this.matrixEffect !== 'solid') this.advanceMatrix(dt);
      this.renderOnce();
    }
    this.frame = requestAnimationFrame(this.draw);
  };

  sync() {
    const visible = this.canRender();
    if (this.sceneGenie) {
      this.sceneGenie.style.setProperty('--genie-motion', visible && this.motion ? 'running' : 'paused');
      if (!visible) this.sceneGenie.hidden = true;
    }
    if (visible) this.resize();
    if (this.renderer && visible && this.motion) {
      if (this.frame === null) this.frame = requestAnimationFrame(this.draw);
    } else {
      if (this.frame !== null) cancelAnimationFrame(this.frame);
      this.frame = null;this.lastTime = null;
    }
  }

  releaseScene() {
    if (this.sceneGenie) this.sceneGenie.hidden = true;
    clearTimeout(this.markerReturnTimer);
    this.markerReturnTimer = null;
    this.markerFlight = null;
    if (this.frame !== null) cancelAnimationFrame(this.frame);this.frame = null;this.lastTime = null;
    this.resizeObserver?.disconnect();this.intersectionObserver?.disconnect();
    const canvas = this.renderer?.domElement;
    this.sceneControls?.abort();
    if (canvas) {canvas.removeEventListener('webglcontextlost', this.lost);canvas.removeEventListener('webglcontextrestored', this.restored);}
    for (const g of this.geometries) g.dispose();for (const m of this.materials) m.dispose();
    this.matrixTexture?.dispose();
    this.matrixTexture = null;
    this.geometries.clear();this.materials.clear();this.plantLeaves = [];
    this.renderer?.dispose();this.renderer?.forceContextLoss();canvas?.remove();
    this.renderer = null;this.scene = null;this.camera = null;
  }

  onDispose() {
    this.disposed = true;
    this.dashboardProgramSubscription?.unsubscribe();
    this.matrixControls?.abort();
    this.updateMatrixPanel = null;
    clearTimeout(this.matrixCommand?.timer);
    this.matrixCommand?.subscription?.unsubscribe();
    document.removeEventListener('visibilitychange', this.visibility);
    this.reduce?.removeEventListener?.('change', this.motionChanged);
    this.releaseScene();
  }
}
