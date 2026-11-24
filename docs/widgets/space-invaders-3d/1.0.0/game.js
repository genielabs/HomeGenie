/* Controller (JavaScript) */
class SpaceInvaders3D extends ControllerInstance {
  settings = {defaultSize:'dashboard', sizeOptions:[]};

  state = { score: 0, lives: 3, playing: false };
  
  // ThreeJS Core
  scene = null; camera = null; renderer = null;
  
  // Entities
  player = null; 
  alienGroup = null; 
  aliens = []; 
  playerBullets = []; 
  alienBullets = [];
  particles = [];
  
  // Settings & Physics
  config = {
    boundsX: 45,
    playerY: -35,
    bulletSpeed: 60,
    alienBaseSpeed: 10,
    alienDrop: 3,
    maxPlayerBullets: 3,
    alienFireRate: 0.015 // Probability per frame
  };

  alienDir = 1;
  alienSpeed = 10;

  animationId = null; 
  lastTime = 0; 
  audioCtx = null; 
  resizeObserver = null;

  onInit() {
    const self = this;
    this.declare({
      startGame: () => self.startGame(),
      movePlayer: (e) => self.movePlayer(e),
      shootPlayer: (e) => self.shootPlayer(e)
    });
  }

  onCreate() {
    this.setupTilt(axis=>{
      const rect=this.field('canvas_container').get().getBoundingClientRect();
      this.movePlayer({clientX:rect.left+(axis+1)*rect.width/2});
    });
    import(new URL('three.module.js', new URL(componentId, document.baseURI)).href).then(three=>{
      if(this.closed)return;
      this.three=three;this.initThreeJS();this.buildLevel();
    }).catch(()=>{if(!this.closed)this.field('overlay_sub').html('3D graphics unavailable');});
  }

  initAudio() {
    if (!this.audioCtx) {
      this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (this.audioCtx.state === 'suspended') {
      this.audioCtx.resume();
    }
  }

  playSound(type) {
    if (!this.audioCtx) return;
    const osc = this.audioCtx.createOscillator();
    const gain = this.audioCtx.createGain();
    osc.connect(gain);
    gain.connect(this.audioCtx.destination);
    const now = this.audioCtx.currentTime;

    if (type === 'shoot') {
      osc.type = 'square';
      osc.frequency.setValueAtTime(800, now);
      osc.frequency.exponentialRampToValueAtTime(100, now + 0.15);
      gain.gain.setValueAtTime(0.05, now);
      gain.gain.exponentialRampToValueAtTime(0.01, now + 0.15);
      osc.start(now); osc.stop(now + 0.15);
    } else if (type === 'hit') {
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(200, now);
      osc.frequency.exponentialRampToValueAtTime(50, now + 0.2);
      gain.gain.setValueAtTime(0.1, now);
      gain.gain.exponentialRampToValueAtTime(0.01, now + 0.2);
      osc.start(now); osc.stop(now + 0.2);
    } else if (type === 'player_die') {
      osc.type = 'square';
      osc.frequency.setValueAtTime(100, now);
      osc.frequency.exponentialRampToValueAtTime(20, now + 0.8);
      gain.gain.setValueAtTime(0.2, now);
      gain.gain.exponentialRampToValueAtTime(0.01, now + 0.8);
      osc.start(now); osc.stop(now + 0.8);
    }
  }

  initThreeJS() {
    const container = this.field('canvas_container').get();
    const width = container.clientWidth;
    const height = container.clientHeight;

    this.scene = new this.three.Scene();
    this.scene.background = new this.three.Color(0x020205);
    // Slightly tilted perspective for 3D effect
    this.camera = new this.three.PerspectiveCamera(55, width / height, 0.1, 1000);
    this.camera.position.set(0, -50, 70);
    this.camera.lookAt(0, -10, 0);

    this.renderer = new this.three.WebGLRenderer({ antialias: true });
    this.renderer.setSize(width, height);
    container.appendChild(this.renderer.domElement);

    // Lights
    this.scene.add(new this.three.AmbientLight(0xffffff, 0.3));
    const dirLight = new this.three.DirectionalLight(0xffffff, 0.6);
    dirLight.position.set(0, 20, 50);
    this.scene.add(dirLight);

    // Player Ship (Constructed from basic geometries)
    this.player = new this.three.Group();
    const shipMat = new this.three.MeshPhongMaterial({ color: 0x00f2ff, emissive: 0x003344, shininess: 100 });
    const baseGeo = new this.three.BoxGeometry(6, 2, 4);
    const cannonGeo = new this.three.BoxGeometry(1.5, 4, 1.5);
    const base = new this.three.Mesh(baseGeo, shipMat);
    const cannon = new this.three.Mesh(cannonGeo, shipMat);
    cannon.position.y = 2;
    this.player.add(base);
    this.player.add(cannon);
    this.player.position.set(0, this.config.playerY, 0);
    this.scene.add(this.player);

    // Grid floor for spatial awareness
    const grid = new this.three.GridHelper(200, 40, 0x111133, 0x050510);
    grid.rotation.x = Math.PI / 2;
    grid.position.z = -5;
    this.scene.add(grid);

    // Alien Group Setup
    this.alienGroup = new this.three.Group();
    this.scene.add(this.alienGroup);

    // Responsive Handling
    this.resizeObserver = new ResizeObserver(() => {
      const w = container.clientWidth;
      const h = container.clientHeight;
      this.renderer.setSize(w, h);
      if(!w || !h)return;
      this.camera.aspect = w / h; this.camera.zoom=Math.min(1, w/h * 0.92);
      this.camera.updateProjectionMatrix();
    });
    this.resizeObserver.observe(container);

    this.lastTime = performance.now();
    this.animationId = requestAnimationFrame((t) => this.gameLoop(t));
  }

  buildLevel() {
    // Clear old aliens
    for (let i = this.aliens.length - 1; i >= 0; i--) {
      this.alienGroup.remove(this.aliens[i].mesh);
    }
    this.aliens = [];
    this.alienGroup.position.set(0, 20, 0);
    this.alienDir = 1;
    this.alienSpeed = this.config.alienBaseSpeed;

    const rows = 4;
    const cols = 9;
    const spacingX = 6;
    const spacingY = 5;
    const startX = -(cols * spacingX) / 2 + (spacingX / 2);
    
    const colors = [0xff0055, 0x39ff14, 0xffff00, 0x00f2ff];
    const geo = new this.three.BoxGeometry(3, 3, 3);

    for (let r = 0; r < rows; r++) {
      const colHex = colors[r % colors.length];
      const mat = new this.three.MeshPhongMaterial({ color: colHex, emissive: colHex, emissiveIntensity: 0.2 });
      for (let c = 0; c < cols; c++) {
        const mesh = new this.three.Mesh(geo, mat);
        mesh.position.set(startX + c * spacingX, r * spacingY, 0);
        this.alienGroup.add(mesh);
        this.aliens.push({ mesh, active: true, color: colHex });
      }
    }
  }

  startGame() {
    if(!this.renderer)return;
    this.initAudio();
    if (this.state.playing) return;
    
    if (this.state.lives <= 0 || this.aliens.filter(a => a.active).length === 0) {
      this.state.score = 0;
      this.state.lives = 3;
      this.field('score_val').html('0');
      this.field('lives_val').html('3');
      this.buildLevel();
      // Clear bullets
      this.clearBullets();
    }
    
    this.state.playing = true;
    this.field('overlay').hide();
    this.player.position.x = 0;
  }

  gameOver(win) {
    this.state.playing = false;
    this.field('overlay').css({ display: 'flex' });
    this.field('overlay_title').html(win ? 'VICTORY' : 'GAME OVER');
    this.field('overlay_sub').html(win ? 'CLICK TO CONTINUE' : 'CLICK TO RESTART');
    if (!win) this.playSound('player_die');
  }

  clearBullets() {
    this.playerBullets.forEach(b => this.scene.remove(b.mesh));
    this.alienBullets.forEach(b => this.scene.remove(b.mesh));
    this.playerBullets = [];
    this.alienBullets = [];
  }

  movePlayer(e) {
    if (!this.player || !this.state.playing) return;
    const rect = this.field('canvas_container').get().getBoundingClientRect();
    const normX = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    this.player.position.x = Math.max(-this.config.boundsX, Math.min(this.config.boundsX, normX * (this.config.boundsX + 10)));
  }

  shootPlayer(e) {
    if (!this.state.playing) return;
    // Limit on-screen bullets
    if (this.playerBullets.length >= this.config.maxPlayerBullets) return;

    this.playSound('shoot');
    const geo = new this.three.BoxGeometry(0.8, 3, 0.8);
    const mat = new this.three.MeshBasicMaterial({ color: 0x39ff14 });
    const mesh = new this.three.Mesh(geo, mat);
    mesh.position.copy(this.player.position);
    mesh.position.y += 3;
    
    this.scene.add(mesh);
    this.playerBullets.push({ mesh });
  }

  shootAlien(alienPos) {
    const geo = new this.three.SphereGeometry(1, 8, 8);
    const mat = new this.three.MeshBasicMaterial({ color: 0xff0055 });
    const mesh = new this.three.Mesh(geo, mat);
    mesh.position.copy(alienPos);
    
    this.scene.add(mesh);
    this.alienBullets.push({ mesh });
  }

  createExplosion(x, y, color) {
    const geo = new this.three.BoxGeometry(0.8, 0.8, 0.8);
    const mat = new this.three.MeshBasicMaterial({ color });
    for (let i = 0; i < 15; i++) {
      const m = new this.three.Mesh(geo, mat);
      m.position.set(x, y, 0);
      this.scene.add(m);
      this.particles.push({ 
        m, 
        vx: (Math.random() - 0.5) * 40, 
        vy: (Math.random() - 0.5) * 40, 
        vz: (Math.random() - 0.5) * 40, 
        life: 1.0 
      });
    }
  }

  gameLoop(time) {
    this.animationId = requestAnimationFrame((t) => this.gameLoop(t));
    const dt = Math.min((time - this.lastTime) / 1000, 0.1);
    this.lastTime = time;

    if (this.state.playing) {
      this.updatePhysics(dt);
    }
    this.updateParticles(dt);
    
    if (this.renderer && this.scene && this.camera) {
      this.renderer.render(this.scene, this.camera);
    }
  }

  updatePhysics(dt) {
    // 1. Update Player Bullets
    for (let i = this.playerBullets.length - 1; i >= 0; i--) {
      const b = this.playerBullets[i];
      b.mesh.position.y += this.config.bulletSpeed * dt;
      
      // Collision with aliens
      let hit = false;
      const activeAliens = this.aliens.filter(a => a.active);
      for (let a of activeAliens) {
        // Calculate global alien position
        const gPos = new this.three.Vector3();
        a.mesh.getWorldPosition(gPos);
        
        if (b.mesh.position.distanceTo(gPos) < 2.5) {
          hit = true;
          a.active = false;
          this.alienGroup.remove(a.mesh);
          this.createExplosion(gPos.x, gPos.y, a.color);
          this.playSound('hit');
          
          this.state.score += 10;
          this.field('score_val').html(this.state.score);
          this.alienSpeed += 0.5; // Increase difficulty
          break;
        }
      }

      if (hit || b.mesh.position.y > 60) {
        this.scene.remove(b.mesh);
        b.mesh.geometry.dispose();
        b.mesh.material.dispose();
        this.playerBullets.splice(i, 1);
      }
    }

    // Win check
    const activeAliens = this.aliens.filter(a => a.active);
    if (activeAliens.length === 0) {
      this.gameOver(true);
      return;
    }

    // 2. Update Alien Group Movement
    let minX = Infinity, maxX = -Infinity;
    let lowestY = Infinity;
    
    activeAliens.forEach(a => {
      const gPos = new this.three.Vector3();
      a.mesh.getWorldPosition(gPos);
      if (gPos.x < minX) minX = gPos.x;
      if (gPos.x > maxX) maxX = gPos.x;
      if (gPos.y < lowestY) lowestY = gPos.y;
    });

    if (maxX > this.config.boundsX && this.alienDir === 1) {
      this.alienDir = -1;
      this.alienGroup.position.y -= this.config.alienDrop;
    } else if (minX < -this.config.boundsX && this.alienDir === -1) {
      this.alienDir = 1;
      this.alienGroup.position.y -= this.config.alienDrop;
    }
    
    this.alienGroup.position.x += this.alienDir * this.alienSpeed * dt;

    // Invasion condition
    if (lowestY <= this.config.playerY) {
      this.state.lives = 0;
      this.field('lives_val').html('0');
      this.gameOver(false);
      return;
    }

    // 3. Alien Shooting Logic
    if (Math.random() < this.config.alienFireRate + (0.001 * (50 - activeAliens.length))) {
      const randomAlien = activeAliens[Math.floor(Math.random() * activeAliens.length)];
      const gPos = new this.three.Vector3();
      randomAlien.mesh.getWorldPosition(gPos);
      this.shootAlien(gPos);
    }

    // 4. Update Alien Bullets
    for (let i = this.alienBullets.length - 1; i >= 0; i--) {
      const b = this.alienBullets[i];
      b.mesh.position.y -= (this.config.bulletSpeed * 0.6) * dt;

      // Collision with player
      if (b.mesh.position.distanceTo(this.player.position) < 3.5) {
        this.scene.remove(b.mesh);
        this.alienBullets.splice(i, 1);
        
        this.createExplosion(this.player.position.x, this.player.position.y, 0x00f2ff);
        this.playSound('player_die');
        this.state.lives--;
        this.field('lives_val').html(this.state.lives);
        
        if (this.state.lives <= 0) {
          this.gameOver(false);
        }
        continue;
      }

      if (b.mesh.position.y < -60) {
        this.scene.remove(b.mesh);
        b.mesh.geometry.dispose();
        b.mesh.material.dispose();
        this.alienBullets.splice(i, 1);
      }
    }
  }

  updateParticles(dt) {
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i]; 
      p.life -= dt * 1.5;
      if (p.life <= 0) { 
        this.scene.remove(p.m); 
        p.m.geometry.dispose(); 
        p.m.material.dispose(); 
        this.particles.splice(i, 1); 
      } else { 
        p.m.position.x += p.vx * dt; 
        p.m.position.y += p.vy * dt; 
        p.m.position.z += p.vz * dt; 
        p.m.scale.setScalar(p.life); 
      }
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
      const dom = this.renderer.domElement; 
      if (dom && dom.parentNode) dom.parentNode.removeChild(dom);
    }
    
    // Hard cleanup of Three.js objects to prevent Memory Leaks
    if (this.scene) {
      this.scene.traverse((obj) => {
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) { 
          if (Array.isArray(obj.material)) {
            obj.material.forEach(m => m.dispose());
          } else {
            obj.material.dispose();
          }
        }
      });
    }
  }

}
