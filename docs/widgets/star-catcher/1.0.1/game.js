class StarCatcher extends ControllerInstance {
  settings = {defaultSize:'dashboard', sizeOptions:[]};
    
    state = {
        playing: false,
        score: 0,
        highScore: 0,
        lives: 3,
        playerX: 0
    };

    items = [];
    gameLoopId = null;
    lastSpawn = 0;
    spawnRate = 1200; // ms

    onInit() {
        const self = this;
        this.declare({
            startGame: () => self.startGame(),
            movePlayer: (e) => self.movePlayer(e),
            noHighScore: () => self.state.highScore === 0
        });
    }

    onCreate() {
    this.setupTilt(axis=>{
      const rect=this.field('game_screen').get().getBoundingClientRect();
      this.movePlayer({clientX:rect.left+(axis+1)*rect.width/2});
    });
        this.state.playerX = this.field('game_screen').get().clientWidth / 2;
        this.refreshUI();
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
  tone(hz){
        if(this.audio?.state!=='running')return;
        const at=this.audio.currentTime, osc=this.audio.createOscillator(), level=this.audio.createGain();
        osc.frequency.setValueAtTime(hz,at);osc.frequency.exponentialRampToValueAtTime(hz*1.25,at+.12);
        level.gain.setValueAtTime(.045,at);level.gain.exponentialRampToValueAtTime(.001,at+.16);
        osc.connect(level);level.connect(this.audio.destination);osc.start(at);osc.stop(at+.17);
    }
    onDispose() {
        this.audio?.close();
    this.closed=true;this.removeTilt?.();
        this.stopLoop();
    }

    startGame() {
        if(window.AudioContext){try{this.audio ||= new AudioContext();this.audio.resume().catch(()=>{});}catch{}}
        if (this.state.playing) return;
        
        this.state.playing = true;this.lastFrame=0;
        this.state.score = 0;
        this.state.lives = 3;
        this.items.forEach(item => item.el.remove());
        this.items = [];
        this.spawnRate = 1200;
        
        this.field('overlay').hide();
        this.refreshUI();
        this.startLoop();
    }

    gameOver() {
        this.state.playing = false;
        this.stopLoop();
        
        if (this.state.score > this.state.highScore) {
            this.state.highScore = this.state.score;
        }

        this.model().message_title = "GAME OVER";
        this.model().message_sub = "Tap to play again";
        this.field('overlay').show();
        this.refreshUI();
    }

    startLoop() {
        const loop = (time) => {
            if(!this.state.playing)return;
            this.updatePhysics(time);
            this.gameLoopId = requestAnimationFrame(loop);
        };
        this.gameLoopId = requestAnimationFrame(loop);
    }

    stopLoop() {
        if (this.gameLoopId) cancelAnimationFrame(this.gameLoopId);
    }

    movePlayer(e) {
        if (!this.state.playing) return;
        const rect = this.field('game_screen').get().getBoundingClientRect();
        // Supports both mouse and touch
        const clientX = e.touches ? e.touches[0].clientX : e.clientX;
        let x = clientX - rect.left;

        // Limits
        if (x < 25) x = 25;
        if (x > rect.width - 25) x = rect.width - 25;
        
        this.state.playerX = x;
        this.field('player_node').css({
            'left': `${x}px`
        });
    }

    updatePhysics(time) {
        const step=Math.min(2, (time-(this.lastFrame || time))/16.67);this.lastFrame=time;
        // 1. Spawning
        if (time - this.lastSpawn > this.spawnRate) {
            this.spawnItem();
            this.lastSpawn = time;
            // Increase difficulty gradually
            this.spawnRate = Math.max(400, this.spawnRate - 10);
        }

        const screenHeight = this.field('game_screen').get().clientHeight;
        const player = this.field('player_node').get();
        const playerY = player.offsetTop + player.offsetHeight / 2;

        // 2. Movement and Collisions
        for (let i = this.items.length - 1; i >= 0; i--) {
            const item = this.items[i];
            item.y += item.speed * step;
            item.el.style.top = `${item.y}px`;

            // Collision check
            const dist = Math.hypot(item.x - this.state.playerX, item.y - playerY);
            
            if (dist < 40) {
                this.handleCollision(item);
                this.removeItem(i);
                continue;
            }

            // Offscreen
            if (item.y > screenHeight) {
                this.removeItem(i);
            }
        }
    }

    spawnItem() {
        const isStar = Math.random() > 0.3;
        const container = this.field('play_field').get();
        const el = document.createElement('div');
        
        el.className = isStar ? 'star' : 'void';
        el.innerHTML = isStar ? '⭐' : '🕳️';
        
        const x = Math.random() * (container.clientWidth - 40) + 20;
        const speed = Math.random() * 2 + (isStar ? 3 : 4);
        
        el.style.left = `${x}px`;
        el.style.top = `-40px`;
        
        container.appendChild(el);
        this.items.push({ el, x, y: -40, speed, isStar });
    }

    handleCollision(item) {
        this.tone(item.isStar?660:120);
        if (item.isStar) {
            this.state.score += 10;
        } else {
            this.state.lives -= 1;
            // Visual damage effect
            this.field('game_screen').addClass('shake');
            setTimeout(() => this.field('game_screen').removeClass('shake'), 300);
            
            if (this.state.lives <= 0) {
                this.gameOver();
            }
        }
        this.refreshUI();
    }

    removeItem(index) {
        const item = this.items[index];
        item.el.remove();
        this.items.splice(index, 1);
    }

    refreshUI() {
        this.model().score_val = this.state.score;
        this.model().lives_val = this.state.lives;
        this.model().high_score_val = this.state.highScore;
    }
}
