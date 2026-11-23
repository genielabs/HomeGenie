class NeonBreakerWidget extends ControllerInstance {
  settings = {
    defaultSize: '',
    sizeOptions: [],
    fillWidth: true
  };

  gameState = {
    score: 0,
    highScore: 0,
    lives: 3,
    isRunning: false,
    isPaused: false,
    level: 1
  };

  paddle = { x: 200, y: 355, width: 84, height: 12, speed: 7, moveDir: 0 };
  ball = { x: 240, y: 340, radius: 6, vx: 3.5, vy: -3.5, speed: 5 };
  bricks = [];
  particles = [];
  rafId = null;
  isDisposed = false;
  isDocVisible = true;
  isViewportActive = true;

  labels = {
    "points": "POINTS",
    "high_score": "HIGH SCORE",
    "coffee_break": "COFFEE BREAK!",
    "smash_all_the_neon_bricks_to_relax": "Smash all the neon bricks to relax",
    "play": "PLAY",
    "drag_use_arrows_or_buttons": "Drag, use arrows or buttons",
    "move_left": "Move left",
    "move_right": "Move right",
    "game_over": "GAME OVER",
    "play_again": "PLAY AGAIN",
    "final_score_points": "Final score: {score} points"
  };

  onCreate() {
    import(new URL('localize.js', new URL(componentId, document.baseURI)).href)
      .then(({localize}) => localize(this, ["overlay_title", "btn_label"]))
      .catch(error => console.warn('Widget localization unavailable', error));
    this.canvas = this.field('game_canvas').get();
    this.ctx = this.canvas.getContext('2d');
    this.overlay = this.field('game_overlay');

    const saved = window.sessionStorage?.getItem('neon_breaker_high');
    if (saved) this.gameState.highScore = parseInt(saved, 10) || 0;
    this.model().high_text = this.gameState.highScore;
    this.model().score_text = 0;
    this.model().lives_text = 3;

    this.declare({
      togglePlay: () => this.startOrResume()
    });

    this.resizeField();
    this.resizeObserver = new ResizeObserver(() => this.resizeField());
    this.resizeObserver.observe(this.canvas);
    this.setupInputs();
    this.setupLifecycle();
    this.resetField();
    this.drawScene();
  }

  brickLayout() {
    const columns = 7;
    const gap = 8;
    const margin = 20;
    const width = (this.canvas.width - 2 * margin - (columns - 1) * gap) / columns;
    return { columns, gap, margin, width };
  }

  resizeField() {
    const rect = this.canvas.getBoundingClientRect();
    if (!rect.width || !rect.height) return;
    const scale = Math.min(rect.width / 480, rect.height / 380);
    const width = Math.round(rect.width / scale);
    const height = Math.round(rect.height / scale);
    const oldWidth = this.canvas.width;
    const oldHeight = this.canvas.height;
    if (width === oldWidth && height === oldHeight) return;

    this.canvas.width = width;
    this.canvas.height = height;
    const oldPaddleY = this.paddle.y;
    this.paddle.y = height - 25;
    this.paddle.x *= (width - this.paddle.width) / (oldWidth - this.paddle.width);
    this.ball.x *= width / oldWidth;
    this.ball.y *= this.paddle.y / oldPaddleY;
    const layout = this.brickLayout();
    this.bricks.forEach((brick, index) => {
      brick.w = layout.width;
      brick.x = layout.margin + (index % layout.columns) * (layout.width + layout.gap);
    });
    this.particles = [];
    this.drawScene();
  }

  setupInputs() {
    const c = this.canvas;
    let isDragging = false;

    const updatePaddlePos = (clientX) => {
      const rect = c.getBoundingClientRect();
      const scaleX = c.width / rect.width;
      const targetX = (clientX - rect.left) * scaleX - (this.paddle.width / 2);
      this.paddle.x = Math.max(0, Math.min(c.width - this.paddle.width, targetX));
    };

    c.addEventListener('pointerdown', (e) => {
      isDragging = true;
      c.setPointerCapture(e.pointerId);
      updatePaddlePos(e.clientX);
    });
    c.addEventListener('pointermove', (e) => {
      if (isDragging) updatePaddlePos(e.clientX);
    });
    const endDrag = (e) => {
      isDragging = false;
      try { c.releasePointerCapture(e.pointerId); } catch (_) {}
    };
    c.addEventListener('pointerup', endDrag);
    c.addEventListener('pointercancel', endDrag);

    this.keyHandler = (e) => {
      if (e.key === 'ArrowLeft' || e.key === 'a' || e.key === 'A') {
        this.paddle.moveDir = (e.type === 'keydown') ? -1 : 0;
      } else if (e.key === 'ArrowRight' || e.key === 'd' || e.key === 'D') {
        this.paddle.moveDir = (e.type === 'keydown') ? 1 : 0;
      } else if (e.key === ' ' && e.type === 'keydown') {
        this.startOrResume();
      }
    };
    window.addEventListener('keydown', this.keyHandler);
    window.addEventListener('keyup', this.keyHandler);

    const btnL = this.field('btn_left').get();
    const btnR = this.field('btn_right').get();
    const bindBtn = (btn, dir) => {
      const start = (ev) => { ev.preventDefault(); this.paddle.moveDir = dir; };
      const stop = (ev) => { ev.preventDefault(); if (this.paddle.moveDir === dir) this.paddle.moveDir = 0; };
      btn.addEventListener('pointerdown', start);
      btn.addEventListener('pointerup', stop);
      btn.addEventListener('pointercancel', stop);
      btn.addEventListener('pointerleave', stop);
    };
    bindBtn(btnL, -1);
    bindBtn(btnR, 1);
  }

  setupLifecycle() {
    this.visibilityHandler = () => {
      this.isDocVisible = !document.hidden;
      if (!this.isDocVisible) this.pauseLoop();
      else if (this.gameState.isRunning && this.isViewportActive) this.resumeLoop();
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);

    this.view().on('refresh:inactive', () => {
      this.isViewportActive = false;
      this.pauseLoop();
    });
    this.view().on('refresh:active', () => {
      this.isViewportActive = true;
      if (this.gameState.isRunning && this.isDocVisible) this.resumeLoop();
    });
  }

  resetField(initBricks = true) {
    this.paddle.x = (this.canvas.width - this.paddle.width) / 2;
    this.ball.x = this.canvas.width / 2;
    this.ball.y = this.paddle.y - 12;
    const angle = (Math.random() * 0.8 - 0.4) * Math.PI;
    const speed = 4.5 + Math.min(this.gameState.level * 0.4, 3);
    this.ball.vx = Math.sin(angle) * speed;
    this.ball.vy = -Math.abs(Math.cos(angle) * speed);
    this.particles = [];

    if (initBricks) {
      this.bricks = [];
      const rows = 4;
      const layout = this.brickLayout();
      const brickH = 16;
      const padY = 8;
      const startY = 36;
      const colors = ['#00f0ff', '#ff007f', '#a855f7', '#39ff14'];

      for (let r = 0; r < rows; r++) {
        for (let c = 0; c < layout.columns; c++) {
          this.bricks.push({
            x: layout.margin + c * (layout.width + layout.gap),
            y: startY + r * (brickH + padY),
            w: layout.width,
            h: brickH,
            color: colors[r % colors.length],
            points: (rows - r) * 15,
            alive: true
          });
        }
      }
    }
  }

  startOrResume() {
    if (!this.gameState.isRunning) {
      if (this.gameState.lives <= 0) {
        this.gameState.lives = 3;
        this.gameState.score = 0;
        this.gameState.level = 1;
        this.model().score_text = 0;
        this.model().lives_text = 3;
      }
      this.gameState.isRunning = true;
      this.overlay.addClass('hidden');
      this.resetField(this.bricks.filter(b => b.alive).length === 0);
      this.resumeLoop();
    }
  }

  pauseLoop() {
    if (this.rafId) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
  }

  resumeLoop() {
    if (this.rafId || this.isDisposed || !this.gameState.isRunning) return;
    const loop = () => {
      if (this.isDisposed || !this.gameState.isRunning) return;
      this.updateGame();
      this.drawScene();
      this.rafId = requestAnimationFrame(loop);
    };
    this.rafId = requestAnimationFrame(loop);
  }

  updateGame() {
    if (this.paddle.moveDir !== 0) {
      this.paddle.x += this.paddle.moveDir * this.paddle.speed;
      this.paddle.x = Math.max(0, Math.min(this.canvas.width - this.paddle.width, this.paddle.x));
    }

    const b = this.ball;
    b.x += b.vx;
    b.y += b.vy;

    if (b.x - b.radius <= 0) {
      b.x = b.radius;
      b.vx = Math.abs(b.vx);
      this.spawnSparks(b.x, b.y, '#00f0ff', 4);
    } else if (b.x + b.radius >= this.canvas.width) {
      b.x = this.canvas.width - b.radius;
      b.vx = -Math.abs(b.vx);
      this.spawnSparks(b.x, b.y, '#00f0ff', 4);
    }
    if (b.y - b.radius <= 0) {
      b.y = b.radius;
      b.vy = Math.abs(b.vy);
      this.spawnSparks(b.x, b.y, '#00f0ff', 4);
    }

    const p = this.paddle;
    if (
      b.y + b.radius >= p.y &&
      b.y - b.radius <= p.y + p.height &&
      b.x >= p.x - 4 &&
      b.x <= p.x + p.width + 4 &&
      b.vy > 0
    ) {
      b.y = p.y - b.radius;
      const hitPos = (b.x - (p.x + p.width / 2)) / (p.width / 2);
      const speed = Math.hypot(b.vx, b.vy);
      const maxAngle = (70 * Math.PI) / 180;
      const newAngle = hitPos * maxAngle;
      b.vx = speed * Math.sin(newAngle);
      b.vy = -speed * Math.cos(newAngle);
      this.spawnSparks(b.x, p.y, '#ffffff', 8);
    }

    for (const br of this.bricks) {
      if (!br.alive) continue;
      if (
        b.x + b.radius > br.x &&
        b.x - b.radius < br.x + br.w &&
        b.y + b.radius > br.y &&
        b.y - b.radius < br.y + br.h
      ) {
        br.alive = false;
        this.spawnSparks(br.x + br.w / 2, br.y + br.h / 2, br.color, 14);
        this.gameState.score += br.points;
        this.model().score_text = this.gameState.score;
        if (this.gameState.score > this.gameState.highScore) {
          this.gameState.highScore = this.gameState.score;
          this.model().high_text = this.gameState.highScore;
          try { window.sessionStorage?.setItem('neon_breaker_high', String(this.gameState.highScore)); } catch (_) {}
        }

        const overlapLeft = (b.x + b.radius) - br.x;
        const overlapRight = (br.x + br.w) - (b.x - b.radius);
        const overlapTop = (b.y + b.radius) - br.y;
        const overlapBottom = (br.y + br.h) - (b.y - b.radius);
        const minOverlap = Math.min(overlapLeft, overlapRight, overlapTop, overlapBottom);

        if (minOverlap === overlapLeft || minOverlap === overlapRight) b.vx = -b.vx;
        else b.vy = -b.vy;
        break;
      }
    }

    if (this.bricks.filter(br => br.alive).length === 0) {
      this.gameState.level++;
      this.gameState.score += 200;
      this.model().score_text = this.gameState.score;
      this.resetField(true);
    }

    if (b.y - b.radius > this.canvas.height) {
      this.gameState.lives--;
      this.model().lives_text = this.gameState.lives;
      this.spawnSparks(b.x, this.canvas.height - 10, '#ff3366', 20);
      if (this.gameState.lives > 0) {
        this.resetField(false);
      } else {
        this.gameOver();
      }
    }

    for (let i = this.particles.length - 1; i >= 0; i--) {
      const pt = this.particles[i];
      pt.x += pt.vx;
      pt.y += pt.vy;
      pt.life -= 0.035;
      if (pt.life <= 0) this.particles.splice(i, 1);
    }
  }

  spawnSparks(x, y, color, count) {
    for (let i = 0; i < count; i++) {
      const angle = Math.random() * Math.PI * 2;
      const spd = Math.random() * 4 + 1;
      this.particles.push({
        x,
        y,
        vx: Math.cos(angle) * spd,
        vy: Math.sin(angle) * spd,
        color,
        life: 1.0,
        size: Math.random() * 3 + 1.5
      });
    }
  }

  gameOver() {
    this.gameState.isRunning = false;
    this.pauseLoop();
    this.model().overlay_title = this.labels.game_over;
    this.model().overlay_subtitle = this.labels.final_score_points.replace('{score}', () => this.gameState.score);
    this.model().btn_label = this.labels.play_again;
    this.overlay.removeClass('hidden');
  }

  drawScene() {
    const ctx = this.ctx;
    const c = this.canvas;
    ctx.clearRect(0, 0, c.width, c.height);

    ctx.strokeStyle = 'rgba(255, 255, 255, 0.04)';
    ctx.lineWidth = 1;
    const gridSize = 24;
    for (let x = 0; x < c.width; x += gridSize) {
      ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, c.height); ctx.stroke();
    }
    for (let y = 0; y < c.height; y += gridSize) {
      ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(c.width, y); ctx.stroke();
    }

    for (const br of this.bricks) {
      if (!br.alive) continue;
      ctx.save();
      ctx.shadowColor = br.color;
      ctx.shadowBlur = 10;
      ctx.fillStyle = br.color;
      ctx.fillRect(br.x, br.y, br.w, br.h);
      ctx.fillStyle = 'rgba(255, 255, 255, 0.45)';
      ctx.fillRect(br.x + 2, br.y + 2, br.w - 4, 3);
      ctx.restore();
    }

    const p = this.paddle;
    ctx.save();
    ctx.shadowColor = '#00f0ff';
    ctx.shadowBlur = 12;
    ctx.fillStyle = '#00f0ff';
    ctx.beginPath();
    ctx.roundRect(p.x, p.y, p.width, p.height, 6);
    ctx.fill();
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(p.x + 10, p.y + 2, p.width - 20, 2);
    ctx.restore();

    const b = this.ball;
    ctx.save();
    ctx.shadowColor = '#ff007f';
    ctx.shadowBlur = 14;
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(b.x, b.y, b.radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();

    for (const pt of this.particles) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, pt.life);
      ctx.fillStyle = pt.color;
      ctx.shadowColor = pt.color;
      ctx.shadowBlur = 6;
      ctx.fillRect(pt.x, pt.y, pt.size, pt.size);
      ctx.restore();
    }
  }

  onDispose() {
    this.labelsDisposed = true;
    this.isDisposed = true;
    this.resizeObserver.disconnect();
    this.pauseLoop();
    if (this.visibilityHandler) {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
    }
    if (this.keyHandler) {
      window.removeEventListener('keydown', this.keyHandler);
      window.removeEventListener('keyup', this.keyHandler);
    }
  }
}