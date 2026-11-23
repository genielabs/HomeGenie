class ChristmasDashboard extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard',
    sizeOptions: [],
    boundModules: {
      lightOne: { label: "Light 1", labelKey: "light_1", typeFilter: 'light,dimmer,color', defaultModule: 'HomeAutomation.PhilipsHue:1' },
      lightTwo: { label: "Light 2", labelKey: "light_2", typeFilter: 'light,dimmer,color', defaultModule: 'HomeAutomation.PhilipsHue:2' }
    }
  };

  labels = {
    "homegenie_party_edition": "HOMEGENIE · PARTY EDITION",
    "the_magic_of_christmas": "The magic of Christmas",
    "a_small_wait_a_great_desire": "A small wait, a great desire.",
    "days": "DAYS",
    "hours": "HOURS",
    "minutes": "MINUTES",
    "seconds": "SECONDS",
    "holiday_lights": "Holiday lights",
    "choose_the_two_lights_from_associated_modules": "Choose the two lights from Associated Modules in the dashboard bar.",
    "light_1": "Light 1",
    "not_available": "Not available",
    "turn_on": "Turn on",
    "turn_off": "Turn off",
    "brightness": "Brightness",
    "light_2": "Light 2",
    "simulate_midnight_fires": "Simulate midnight fires",
    "pause_animations": "Pause animations",
    "december_25_midnight_local_time": "December 25 · Midnight · Local time",
    "the_simulation_lasts_15_seconds_and_activates": "The simulation lasts 15 seconds and activates animations. The countdown does not change.",
    "bright_christmas_tree": "Bright Christmas tree",
    "christmas_tree_with_star_and_bright_decorations": "Christmas tree with star and bright decorations",
    "countdown_to_christmas": "Countdown to Christmas",
    "control_of_two_lights": "Control of two lights",
    "light_brightness_1": "Light brightness 1",
    "light_brightness_2": "Light brightness 2",
    "every_second_brings_us_closer_to_the": "Every second brings us closer to the magic.",
    "select_a_light_in_the_associated_modules": "Select a light in the associated Modules",
    "status_not_available": "Status not available",
    "on": "On",
    "off": "Off",
    "sending_the_request": "Sending the request…",
    "okay": "Okay",
    "the_command_failed": "The command failed.",
    "result_not_confirmed_please_check_your_device": "Result not confirmed. Please check your device before trying again.",
    "request_sent_check_the_device_status": "Request sent. Check the device status.",
    "sending_not_confirmed_please_check_your_device": "Sending not confirmed. Please check your device before trying again.",
    "merry_christmas_may_it_be_full_of": "Merry Christmas! May it be full of light and wonder.",
    "turn_on_animations": "Turn on animations",
    "christmas_year": "COUNTDOWN TO CHRISTMAS {year}",
    "device_brightness": "Brightness: {name}"
  };

  onLabelsReady() {
    this.model().target_label = this.labels.christmas_year.replace('{year}', () => this.target.getFullYear());
    for (const [role, light] of Object.entries(this.state)) light.fallback = role === 'lightOne' ? this.labels.light_1 : this.labels.light_2;
    this.refreshUI();
    this.synchronize();
  }

  onCreate() {
    import(new URL('localize.js', new URL(componentId, document.baseURI)).href)
      .then(({localize}) => localize(this, ["message"]))
      .catch(error => console.warn('Widget localization unavailable', error));
    this.disposed = false;
    this.viewportActive = true;
    this.pageVisible = !document.hidden;
    this.motionPaused = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.timer = null;
    this.reached = false;
    this.celebrateUntil = 0;
    this.commandSubscriptions = new Set();
    this.state = {};
    const now = new Date();
    this.target = new Date(now.getFullYear(), 11, 25, 0, 0, 0);
    if (now.getTime() > this.target.getTime()) {
      this.target = new Date(now.getFullYear() + 1, 11, 25, 0, 0, 0);
    }
    this.model().target_label = this.labels.christmas_year.replace('{year}', () => this.target.getFullYear());
    this.model().message = this.labels.every_second_brings_us_closer_to_the;
    this.declare({
      toggleMotion: () => this.setMotionPaused(!this.motionPaused),
      simulateFireworks: () => this.simulateFireworks(),
      switchLight: (role, on) => this.switchLight(role, on),
      setLightLevel: (role, event) => this.setLightLevel(role, event)
    });
    this.expose('simulateFireworks', () => this.simulateFireworks());
    this.expose('setAnimationsPaused', value => this.setMotionPaused(Boolean(value)));
    this.expose('setLightPower', (role, on) => this.switchLight(role, Boolean(on)));
    this.expose('setLightBrightness', (role, value) => this.sendLevel(role, value));
    this.view().on('refresh:inactive', () => {
      this.viewportActive = false;
      this.synchronize();
    });
    this.view().on('refresh:active', () => {
      this.viewportActive = true;
      this.readLights();
      this.synchronize();
    });
    this.visibilityHandler = () => {
      this.pageVisible = !document.hidden;
      if (this.pageVisible) this.readLights();
      this.synchronize();
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);
    for (const [role, prefix, fallback] of [
      ['lightOne', 'light_one', this.labels.light_1],
      ['lightTwo', 'light_two', this.labels.light_2]
    ]) {
      const module = this.getBoundModule(role);
      this.state[role] = { module, prefix, fallback, level: null, feedback: '', pending: false };
      if (module) this.subscribe(module, () => this.readLights());
    }
    this.readLights();
    this.buildDecorations();
    this.renderCountdown();
    this.synchronize();
  }

  simulateFireworks() {
    if (this.disposed) return;
    this.restartFireworks();
    this.celebrateUntil = Date.now() + 15000;
    this.setMotionPaused(false);
    this.trigger('fireworks-simulated', { durationSeconds: 15 });
  }

  restartFireworks() {
    const scene = this.field('scene').get();
    scene.classList.remove('celebrating');
    const layer = this.field('fireworks').get();
    for (const element of layer.querySelectorAll('.particle, .bloom-core, .bloom-ring')) {
      element.style.animationName = 'none';
    }
    void layer.offsetWidth;
    for (const element of layer.querySelectorAll('.particle, .bloom-core, .bloom-ring')) {
      element.style.animationName = '';
    }
  }

  readLights() {
    if (this.disposed) return;
    for (const item of Object.values(this.state)) {
      const field = item.module?.field('Status.Level');
      const level = field?.decimalValue;
      item.level = Number.isFinite(level) && level >= 0 && level <= 1 ? level : null;
      item.formatted = field ? utils.format.fieldValue(field) : '';
    }
    this.refreshUI();
  }

  refreshUI() {
    if (this.disposed) return;
    const model = this.model();
    for (const item of Object.values(this.state)) {
      const p = item.prefix;
      const available = item.level !== null;
      model[p + '_name'] = item.module?.name || item.fallback;
      model[p + '_state'] = !item.module ? this.labels.select_a_light_in_the_associated_modules : !available ? this.labels.status_not_available : item.level > 0 ? this.labels.on : this.labels.off;
      model[p + '_level'] = available ? Math.round(item.level * 100) + '%' : '—';
      model[p + '_feedback'] = item.feedback;
      const slider = this.field(p + '_slider').get();
      slider.disabled = !available || item.pending;
      slider.value = String(available ? Math.round(item.level * 100) : 0);
      slider.setAttribute('aria-label', this.labels.device_brightness.replace('{name}', () => item.module?.name || item.fallback));
      slider.title = available ? item.formatted : this.labels.not_available;
      this.field(p + '_on').get().disabled = !item.module || item.pending;
      this.field(p + '_off').get().disabled = !item.module || item.pending;
      const card = this.field(p + '_card').get();
      if (available && item.level > 0) card.classList.add('is-on');
      else card.classList.remove('is-on');
    }
  }

  switchLight(role, on) {
    this.sendCommand(role, on ? 'Control.On' : 'Control.Off', undefined);
  }

  setLightLevel(role, event) {
    this.sendLevel(role, event.target.value);
  }

  sendLevel(role, value) {
    const item = this.state[role];
    const level = Number(value);
    if (!item || item.level === null || !Number.isFinite(level)) return;
    this.sendCommand(role, 'Control.Level', Math.max(0, Math.min(100, Math.round(level))));
  }

  sendCommand(role, command, value) {
    const item = this.state[role];
    if (this.disposed || !item?.module || item.pending) return;
    item.pending = true;
    item.feedback = this.labels.sending_the_request;
    this.refreshUI();
    this.trigger('light-command', { role, moduleId: item.module.id, command, value: value ?? null });
    let subscription;
    const finish = message => {
      if (this.disposed) return;
      item.pending = false;
      item.feedback = message;
      this.readLights();
    };
    try {
      const request = value === undefined ? item.module.control(command) : item.module.control(command, value);
      subscription = request.subscribe({
        next: result => {
          const status = result?.response?.Status;
          const failed = (result?.code != null && (result.code < 200 || result.code >= 300))
            || (status != null && status !== this.labels.okay && status !== 0 && status !== '0');
          finish(failed ? this.labels.the_command_failed : '');
        },
        error: () => {
          finish(this.labels.result_not_confirmed_please_check_your_device);
          if (subscription) this.commandSubscriptions.delete(subscription);
        },
        complete: () => {
          if (!this.disposed && item.pending) finish(this.labels.request_sent_check_the_device_status);
          if (subscription) this.commandSubscriptions.delete(subscription);
        }
      });
      if (subscription && !subscription.closed) this.commandSubscriptions.add(subscription);
    } catch (error) {
      finish(this.labels.sending_not_confirmed_please_check_your_device);
    }
  }

  buildDecorations() {
    const snow = this.field('snow').get();
    for (let i = 0; i < 30; i++) {
      const flake = document.createElement('i');
      flake.className = 'flake';
      flake.style.left = (i * 37 % 100) + '%';
      flake.style.animationDuration = (12 + i % 13) + 's';
      flake.style.animationDelay = (-i * 1.7) + 's';
      flake.style.opacity = String(.15 + (i % 5) * .12);
      snow.appendChild(flake);
    }
    const fireworks = this.field('fireworks').get();
    const shows = [
      { x: 28, y: 25, delay: 0, duration: 4.7, kind: 'chrysanthemum', colors: ['#ff709e', '#ffd98b', '#ffb7db'] },
      { x: 72, y: 33, delay: .85, duration: 5.3, kind: 'rings', colors: ['#7dffe0', '#69caff', '#b19aff'] },
      { x: 50, y: 21, delay: 1.8, duration: 6.1, kind: 'willow', colors: ['#ffe6a0', '#ffbd5e', '#fff4d4'] },
      { x: 24, y: 47, delay: 2.8, duration: 5.8, kind: 'stars', colors: ['#db9aff', '#ff91c8', '#99eaff'] },
      { x: 77, y: 53, delay: 3.7, duration: 6.4, kind: 'chrysanthemum', colors: ['#a3ffc8', '#ffe894', '#83deff'] }
    ];
    for (const show of shows) {
      const group = document.createElement('div');
      group.className = 'firework';
      group.style.setProperty('--x', show.x + '%');
      group.style.setProperty('--y', show.y + '%');
      group.style.setProperty('--delay', show.delay + 's');
      group.style.setProperty('--duration', show.duration + 's');
      group.style.setProperty('--color', show.colors[0]);
      const core = document.createElement('i');
      core.className = 'bloom-core';
      group.appendChild(core);
      if (show.kind === 'rings') {
        const ring = document.createElement('i');
        ring.className = 'bloom-ring';
        group.appendChild(ring);
      }
      const count = show.kind === 'stars' ? 40 : 64;
      for (let i = 0; i < count; i++) {
        const particle = document.createElement('i');
        const outer = i % 2 === 0;
        const angle = 2 * Math.PI * Math.floor(i / 2) / (count / 2) + (outer ? 0 : .065);
        let radius = outer ? 46 : 28;
        if (show.kind === 'chrysanthemum') radius -= (i % 5) * 1.7;
        if (show.kind === 'willow') radius = outer ? 42 : 29;
        if (show.kind === 'stars') radius = 30 + 15 * Math.cos(5 * angle);
        const dx = Math.cos(angle) * radius;
        const dy = Math.sin(angle) * radius * (show.kind === 'rings' ? .66 : 1);
        particle.className = 'particle' + (show.kind === 'willow' ? ' willow' : show.kind === 'stars' ? ' starlet' : '');
        // Container query units keep the explosion proportional to its own square.
        particle.style.setProperty('--dx', dx + 'cqw');
        particle.style.setProperty('--dy', dy + 'cqw');
        particle.style.setProperty('--angle', (angle * 180 / Math.PI - 90) + 'deg');
        particle.style.setProperty('--color', show.colors[i % show.colors.length]);
        particle.style.setProperty('--size', (show.kind === 'stars' ? 10 + i % 4 : 3 + i % 4) + 'px');
        particle.style.setProperty('--delay', (show.delay + (outer ? 0 : .15)) + 's');
        group.appendChild(particle);
      }
      group.style.containerType = 'inline-size';
      fireworks.appendChild(group);
    }
  }

  setMotionPaused(value) {
    if (this.disposed) return;
    this.motionPaused = value;
    this.synchronize();
    this.trigger('animations-changed', { paused: value });
  }

  renderCountdown() {
    if (this.disposed) return;
    const now = Date.now();
    const seconds = Math.max(0, Math.ceil((this.target.getTime() - now) / 1000));
    const model = this.model();
    const locale = utils.preferences.ui.language || navigator.language;
    model.days = Math.floor(seconds / 86400).toLocaleString(locale, { minimumIntegerDigits: 2 });
    model.hours = String(Math.floor(seconds / 3600) % 24).padStart(2, '0');
    model.minutes = String(Math.floor(seconds / 60) % 60).padStart(2, '0');
    model.seconds = String(seconds % 60).padStart(2, '0');
    if (seconds === 0 && !this.reached) {
      this.reached = true;
      this.restartFireworks();
      this.celebrateUntil = now + 15000;
      model.message = this.labels.merry_christmas_may_it_be_full_of;
      this.trigger('countdown-completed', { target: this.target.toISOString() });
    }
    const scene = this.field('scene').get();
    if (now < this.celebrateUntil) scene.classList.add('celebrating');
    else scene.classList.remove('celebrating');
  }

  synchronize() {
    if (this.disposed) return;
    const active = this.viewportActive && this.pageVisible;
    const scene = this.field('scene').get();
    if (!active || this.motionPaused) scene.classList.add('paused');
    else scene.classList.remove('paused');
    this.model().motion_button = this.motionPaused ? this.labels.turn_on_animations : this.labels.pause_animations;
    if (!active) {
      if (this.timer !== null) clearInterval(this.timer);
      this.timer = null;
      return;
    }
    this.renderCountdown();
    if (this.timer === null) this.timer = setInterval(() => this.renderCountdown(), 1000);
  }

  onDispose() {
    this.labelsDisposed = true;
    this.disposed = true;
    if (this.timer !== null) clearInterval(this.timer);
    this.timer = null;
    document.removeEventListener('visibilitychange', this.visibilityHandler);
    for (const subscription of this.commandSubscriptions) subscription.unsubscribe();
    this.commandSubscriptions.clear();
    this.field('scene').get().classList.add('paused');
  }
}