// AI-generated with HomeGenie Widget Genie (https://homegenie.it)
class SimonWidget extends ControllerInstance {
  settings = {defaultSize:'dashboard', sizeOptions:[]};

  state = {
    sequence: [],
    userSequence: [],
    isPlaying: false,
    isUserTurn: false,
    score: 0
  };

  timers = [];
  audioCtx = null;
  tones = [261.63, 329.63, 392.00, 523.25];
  animationTimeout = null;

  onInit() {
    // Flex Layout Attributes
    // Material Symbols Outlined
  }

  onCreate() {
    const self = this;
    this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();

    this.declare({
      startGame: () => self.startGame(),
      handlePadClick: (id) => self.handlePadClick(id),
      isPlaying: () => {
        return self.state.isPlaying;
      }
    });
    // remove entrance effect to allow custom animations on main container
    this.animationTimeout = setTimeout(() => {
      this.view()
          .removeClass('animate__zoomIn');
    }, 2000);
  }

  playTone(id) {
    if (this.audioCtx.state === 'suspended') {
      this.audioCtx.resume();
    }
    const osc = this.audioCtx.createOscillator();
    const gain = this.audioCtx.createGain();

    osc.connect(gain);
    gain.connect(this.audioCtx.destination);

    osc.frequency.value = this.tones[id];
    gain.gain.setValueAtTime(0.1, this.audioCtx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, this.audioCtx.currentTime + 0.4);

    osc.start();
    osc.stop(this.audioCtx.currentTime + 0.4);
  }

  lightUpPad(id) {
    this.playTone(id);
    const pad = this.field('pad_' + id);
    pad.addClass('active');
    const t = setTimeout(() => pad.removeClass('active'), 400);
    this.timers.push(t);
  }

  startGame() {
    if (this.state.isPlaying) return;

    this.view()
        .removeClass('animate__headShake');

    this.state.isPlaying = true;
    this.state.sequence = [];
    this.state.score = 0;
    this.model().score_text = '0';
    this.model().message = 'Watch the sequence...';

    this.timers.push(setTimeout(() => this.nextRound(), 800));
  }

  nextRound() {
    this.state.userSequence = [];
    this.state.isUserTurn = false;
    this.state.sequence.push(Math.floor(Math.random() * 4));

    let delay = 600;
    this.state.sequence.forEach((padId, index) => {
      const t = setTimeout(() => {
        this.lightUpPad(padId);
        if (index === this.state.sequence.length - 1) {
          const t2 = setTimeout(() => {
            this.state.isUserTurn = true;
            this.model().message = 'Your turn!';
          }, 600);
          this.timers.push(t2);
        }
      }, delay * (index + 1));
      this.timers.push(t);
    });
  }

  handlePadClick(id) {
    if (!this.state.isUserTurn) return;

    this.lightUpPad(id);
    this.state.userSequence.push(id);

    const currentIndex = this.state.userSequence.length - 1;
    if (this.state.userSequence[currentIndex] !== this.state.sequence[currentIndex]) {
      this.gameOver();
      return;
    }

    if (this.state.userSequence.length === this.state.sequence.length) {
      this.state.score++;
      this.model().score_text = this.state.score.toString();
      this.state.isUserTurn = false;
      this.model().message = 'Correct!';
      this.timers.push(setTimeout(() => this.nextRound(), 1000));
    }
  }

  gameOver() {
    this.state.isPlaying = false;
    this.state.isUserTurn = false;
    this.model().message = 'Game Over!';
    this.view()
        .addClass('animate__headShake');

    // Play a low error tone
    if (this.audioCtx) {
      const osc = this.audioCtx.createOscillator();
      const gain = this.audioCtx.createGain();
      osc.connect(gain);
      gain.connect(this.audioCtx.destination);
      osc.frequency.value = 110;
      gain.gain.setValueAtTime(0.2, this.audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, this.audioCtx.currentTime + 0.6);
      osc.start();
      osc.stop(this.audioCtx.currentTime + 0.6);
    }
  }

  onDispose() {
    this.timers.forEach((t) => clearTimeout(t));
    clearTimeout(this.animationTimeout);
    // re-add entrance effect
    this.view()
        .addClass('animate__zoomIn');
    if (this.audioCtx) {
      this.audioCtx.close();
    }
  }

}
