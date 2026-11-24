// AI-generated with HomeGenie Widget Genie (https://homegenie.it)
class Breakout3DWidget extends ControllerInstance {
  settings = {defaultSize:'dashboard', sizeOptions:[]};

  state = { score: 0, lives: 3, playing: false };
  scene = null; camera = null; renderer = null; paddle = null; ball = null; ballLight = null;
  bricks = []; particles = [];

  physics = {
    ballRadius: 1.5, paddleWidth: 16, paddleHeight: 2, paddleDepth: 3,
    bounds: { x: 40, y: 44 }, ballVel: { x: 0, y: 0 }, baseSpeed: 55
  };

  animationId = null; lastTime = 0; audioCtx = null; resizeObserver = null;

  onInit() {
    const self = this;
    this.declare({
      startGame: () => self.startGame(),
      movePaddle: (e) => self.movePaddle(e)
    });
  }

  onCreate() {
    this.setupTilt(axis=>{
      const rect=this.field('canvas_container').get().getBoundingClientRect();
      this.movePaddle({clientX:rect.left+(axis+1)*rect.width/2});
    });
    import(new URL('three.module.js', new URL(componentId, document.baseURI)).href).then(three=>{
      if(this.closed)return;
      this.three=three;this.initThreeJS();this.buildLevel();
    }).catch(()=>{if(!this.closed)this.field('overlay_sub').html('3D graphics unavailable');});
  }

  initAudio() {
    if (!this.audioCtx) this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (this.audioCtx.state === 'suspended') this.audioCtx.resume();
  }

  playSound(type) {
    if (!this.audioCtx) return;
    const osc = this.audioCtx.createOscillator();
    const gain = this.audioCtx.createGain();
    osc.connect(gain); gain.connect(this.audioCtx.destination);
    const now = this.audioCtx.currentTime;
    if (type === 'paddle') {
      osc.type = 'square'; osc.frequency.setValueAtTime(400, now); osc.frequency.exponentialRampToValueAtTime(200, now + 0.1);
      gain.gain.setValueAtTime(0.1, now); gain.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
      osc.start(); osc.stop(now + 0.1);
    } else if (type === 'brick') {
      osc.type = 'sawtooth'; osc.frequency.setValueAtTime(800, now); osc.frequency.exponentialRampToValueAtTime(1200, now + 0.1);
      gain.gain.setValueAtTime(0.1, now); gain.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
      osc.start(); osc.stop(now + 0.1);
    } else if (type === 'wall') {
      osc.type = 'sine'; osc.frequency.setValueAtTime(300, now);
      gain.gain.setValueAtTime(0.05, now); gain.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
      osc.start(); osc.stop(now + 0.1);
    } else if (type === 'die') {
      osc.type = 'sawtooth'; osc.frequency.setValueAtTime(200, now); osc.frequency.exponentialRampToValueAtTime(50, now + 0.5);
      gain.gain.setValueAtTime(0.2, now); gain.gain.exponentialRampToValueAtTime(0.01, now + 0.5);
      osc.start(); osc.stop(now + 0.5);
    }
  }

  initThreeJS() {
    const container = this.field('canvas_container').get();
    const width = container.clientWidth, height = container.clientHeight;
    this.scene = new this.three.Scene();
    this.scene.background = new this.three.Color(0x010103);
    this.camera = new this.three.PerspectiveCamera(50, width / height, 0.1, 1000);
    this.camera.position.set(0, -45, 95); this.camera.lookAt(0, 0, 0);
    this.renderer = new this.three.WebGLRenderer({ antialias: true });
    this.renderer.setSize(width, height); container.appendChild(this.renderer.domElement);

    // DARK LIGHTING
    this.scene.add(new this.three.AmbientLight(0xffffff, 0.2));
    const dirLight = new this.three.DirectionalLight(0xffffff, 0.4);
    dirLight.position.set(0, 20, 50); this.scene.add(dirLight);

    const paddleGeo = new this.three.BoxGeometry(this.physics.paddleWidth, this.physics.paddleHeight, this.physics.paddleDepth);
    const paddleMat = new this.three.MeshPhongMaterial({ color: 0x00ffff, emissive: 0x002222, shininess: 100 });
    this.paddle = new this.three.Mesh(paddleGeo, paddleMat);
    this.paddle.position.set(0, -this.physics.bounds.y + 5, 0); this.scene.add(this.paddle);

    const ballGeo = new this.three.SphereGeometry(this.physics.ballRadius, 16, 16);
    const ballMat = new this.three.MeshPhongMaterial({ color: 0xffffff, emissive: 0xffffff, emissiveIntensity: 1 });
    this.ball = new this.three.Mesh(ballGeo, ballMat);
    this.scene.add(this.ball);

    // THE SHINY BALL LIGHT
    this.ballLight = new this.three.PointLight(0xffffff, 4, 60);
    this.ball.add(this.ballLight);

    const grid = new this.three.GridHelper(160, 20, 0x111122, 0x050510);
    grid.rotation.x = Math.PI / 2; grid.position.z = -5; this.scene.add(grid);

    const borderMat = new this.three.MeshPhongMaterial({ color: 0xff0055, emissive: 0x220011, wireframe: true });
    const bV = new this.three.BoxGeometry(1, this.physics.bounds.y * 2, 2);
    const bH = new this.three.BoxGeometry(this.physics.bounds.x * 2 + 2, 1, 2);
    const lb = new this.three.Mesh(bV, borderMat); lb.position.set(-this.physics.bounds.x - 0.5, 0, 0); this.scene.add(lb);
    const rb = new this.three.Mesh(bV, borderMat); rb.position.set(this.physics.bounds.x + 0.5, 0, 0); this.scene.add(rb);
    const tb = new this.three.Mesh(bH, borderMat); tb.position.set(0, this.physics.bounds.y + 0.5, 0); this.scene.add(tb);

    this.resetBall();
    this.resizeObserver = new ResizeObserver(() => {
      const w = container.clientWidth, h = container.clientHeight;
      this.renderer.setSize(w, h); if(!w || !h)return;
      this.camera.aspect = w / h; this.camera.zoom=Math.min(1, w/h * 0.92); this.camera.updateProjectionMatrix();
    });
    this.resizeObserver.observe(container);
    this.lastTime = performance.now();
    this.animationId = requestAnimationFrame((t) => this.gameLoop(t));
  }

  buildLevel() {
    this.bricks.forEach(b => this.scene.remove(b.mesh)); this.bricks = [];
    const rows = 5, cols = 8, bW = 8, bH = 3, bD = 3, pad = 1.5;
    const startX = -(cols * (bW + pad)) / 2 + (bW / 2) + (pad / 2);
    const colors = [0xff0055, 0x00f2ff, 0x39ff14, 0xffff00, 0xff00ff];
    const brickGeo = new this.three.BoxGeometry(bW, bH, bD);
    for (let r = 0; r < rows; r++) {
      const color = colors[r % colors.length];
      const mat = new this.three.MeshPhongMaterial({ color: color, emissive: color, emissiveIntensity: 0.15, shininess: 150 });
      for (let c = 0; c < cols; c++) {
        const mesh = new this.three.Mesh(brickGeo, mat);
        mesh.position.set(startX + c * (bW + pad), 15 + (rows - r) * (bH + pad), 0);
        this.scene.add(mesh); this.bricks.push({ mesh, active: true, box: new this.three.Box3().setFromObject(mesh), color });
      }
    }
  }

  resetBall() {
    this.ball.position.set(0, -this.physics.bounds.y + 10, 0);
    this.physics.ballVel.x = 0; this.physics.ballVel.y = 0;
  }

  launchBall() {
    const angle = (Math.random() * 0.6) - 0.3;
    this.physics.ballVel.x = Math.sin(angle) * this.physics.baseSpeed;
    this.physics.ballVel.y = Math.cos(angle) * this.physics.baseSpeed;
  }

  startGame() {
    if(!this.renderer)return;
    this.initAudio(); if (this.state.playing) return;
    if (this.state.lives <= 0 || this.bricks.filter(b => b.active).length === 0) {
      this.state.score = 0; this.state.lives = 3; this.field('score_val').html('0'); this.field('lives_val').html('3'); this.buildLevel();
    }
    this.state.playing = true; this.field('overlay').hide(); this.resetBall(); this.launchBall();
  }

  gameOver(win) {
    this.state.playing = false; this.field('overlay').css({ display: 'flex' });
    this.field('overlay_title').html(win ? 'LEVEL CLEARED' : 'SYSTEM FAILURE');
    this.field('overlay_sub').html(win ? 'CLICK TO PLAY' : 'CLICK TO REBOOT'); this.resetBall();
  }

  movePaddle(e) {
    if (!this.paddle || !this.state.playing) return;
    const rect = this.field('canvas_container').get().getBoundingClientRect();
    const normX = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    const limit = this.physics.bounds.x - (this.physics.paddleWidth / 2);
    this.paddle.position.x = Math.max(-limit, Math.min(limit, normX * (this.physics.bounds.x + 5)));
  }

  createExplosion(x, y, color) {
    const geo = new this.three.BoxGeometry(1, 1, 1), mat = new this.three.MeshBasicMaterial({ color });
    for (let i = 0; i < 12; i++) {
      const m = new this.three.Mesh(geo, mat); m.position.set(x, y, 0); this.scene.add(m);
      this.particles.push({ m, vx: (Math.random() - 0.5) * 50, vy: (Math.random() - 0.5) * 50, vz: (Math.random() - 0.5) * 50, life: 1.0 });
    }
  }

  gameLoop(time) {
    this.animationId = requestAnimationFrame((t) => this.gameLoop(t));
    const dt = Math.min((time - this.lastTime) / 1000, 0.1); this.lastTime = time;
    if (this.state.playing) this.updatePhysics(dt);
    this.updateParticles(dt);
    if (this.renderer && this.scene && this.camera) this.renderer.render(this.scene, this.camera);
  }

  updatePhysics(dt) {
    const b = this.ball.position, v = this.physics.ballVel, r = this.physics.ballRadius;
    b.x += v.x * dt; b.y += v.y * dt;
    if (Math.abs(b.x) + r > this.physics.bounds.x) { b.x = Math.sign(b.x) * (this.physics.bounds.x - r); v.x *= -1; this.playSound('wall'); }
    if (b.y + r > this.physics.bounds.y) { b.y = this.physics.bounds.y - r; v.y *= -1; this.playSound('wall'); }
    if (b.y - r < -this.physics.bounds.y) {
      this.playSound('die'); this.state.lives--; this.field('lives_val').html(this.state.lives);
      if (this.state.lives <= 0) this.gameOver(false);
      else { this.state.playing = false; this.resetBall(); setTimeout(() => { if (this.state.lives > 0) { this.state.playing = true; this.launchBall(); } }, 1000); }
      return;
    }
    const p = this.paddle.position, pw = this.physics.paddleWidth / 2, ph = this.physics.paddleHeight / 2;
    if (v.y < 0 && b.y - r < p.y + ph && b.y + r > p.y - ph && Math.abs(b.x - p.x) < pw + r) {
      b.y = p.y + ph + r; v.y *= -1; v.x = ((b.x - p.x) / pw) * this.physics.baseSpeed * 0.8;
      const s = Math.sqrt(v.x * v.x + v.y * v.y); v.x = (v.x / s) * this.physics.baseSpeed; v.y = (v.y / s) * this.physics.baseSpeed;
      this.playSound('paddle');
    }
    const ballBox = new this.three.Box3().setFromObject(this.ball);
    for (let i = 0; i < this.bricks.length; i++) {
      const br = this.bricks[i]; if (!br.active) continue;
      if (ballBox.intersectsBox(br.box)) {
        br.active = false; this.scene.remove(br.mesh); v.y *= -1; this.playSound('brick');
        this.createExplosion(br.mesh.position.x, br.mesh.position.y, br.color);
        this.state.score += 10; this.field('score_val').html(this.state.score);
        this.physics.baseSpeed = Math.min(90, this.physics.baseSpeed + 0.5);
        break;
      }
    }
    if (this.bricks.filter(b => b.active).length === 0) this.gameOver(true);
  }

  updateParticles(dt) {
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i]; p.life -= dt * 2;
      if (p.life <= 0) { this.scene.remove(p.m); p.m.geometry.dispose(); p.m.material.dispose(); this.particles.splice(i, 1); }
      else { p.m.position.x += p.vx * dt; p.m.position.y += p.vy * dt; p.m.position.z += p.vz * dt; p.m.scale.setScalar(p.life); }
    }
  }

    setupTilt(move) {
    const button=this.field('tilt_button').get();
    if(!window.DeviceOrientationEvent){button.hidden=true;return;}
    let enabled=false,zero=null,orientation=null;
    const handle=e=>{
      if(!enabled || !this.state.playing || document.hidden || e.gamma==null || e.beta==null)return;
      const angle=screen.orientation?.angle || window.orientation || 0;
      const r=angle*Math.PI/180;
      const axis=e.gamma*Math.cos(r)+e.beta*Math.sin(r);
      if(zero===null || angle!==orientation){zero=axis;orientation=angle;}
      move(Math.max(-1,Math.min(1,(axis-zero)/24)));
    };
    button.onclick=async()=>{
      if(!enabled && DeviceOrientationEvent.requestPermission){
        try{if(await DeviceOrientationEvent.requestPermission()!=='granted')return;}catch{return;}
      }
      if(this.closed)return;
      enabled=!enabled;zero=null;button.setAttribute('aria-pressed',String(enabled));
    };
    window.addEventListener('deviceorientation',handle);
    this.removeTilt=()=>window.removeEventListener('deviceorientation',handle);
  }
  onDispose() {
    this.closed=true;this.removeTilt?.();
    if (this.animationId) cancelAnimationFrame(this.animationId);
    if (this.resizeObserver) this.resizeObserver.disconnect();
    if (this.audioCtx) this.audioCtx.close();
    if (this.renderer) {
      this.renderer.dispose();
      const dom = this.renderer.domElement; if (dom && dom.parentNode) dom.parentNode.removeChild(dom);
    }
    if (this.scene) {
      this.scene.traverse((obj) => {
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) { if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose()); else obj.material.dispose(); }
      });
    }
  }

}
