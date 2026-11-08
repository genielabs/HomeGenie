class IsometricLight extends ControllerInstance {
  settings = {
    defaultSize: 'medium',
    sizeOptions: ['medium', 'big'],
    boundModules: {
      light: {label: 'Light', labelKey: 'bind_light', typeFilter: 'Light,Dimmer,Color'}
    }
  };
  labels = {
    "light_heading": "Lighting",
    "no_light": "No light selected",
    "not_configured": "Not configured",
    "unavailable": "Unavailable",
    "light_on": "On",
    "light_off": "Off",
    "device_state": "Device state",
    "waiting": "Waiting…",
    "turn_off": "Turn light off",
    "turn_on": "Turn light on",
    "sending": "Sending command…",
    "command_timeout": "No confirmation received. Check the device state.",
    "command_failed": "The command failed. Try again.",
    "command_sent": "Command sent. Waiting for the device…",
    "command_unavailable": "Command unavailable.",
    "bind_light": "Light"
  };
  disposed = false;
  commandId = 0;
  pending = false;
  commandTimer = null;
  commandSubscription = null;
  desired = null;

  onInit() {
    this.declare({toggleLight: () => this.toggleLight()});
  }

  onCreate() {
    this.light = this.getBoundModule('light');
    const view = this.view().get();
    this.host = view.closest('zx-widget') ?? view.getRootNode().host;
    this.sceneToggle = () => this.toggleLight();
    this.host?.addEventListener('isometric:toggle-light', this.sceneToggle);
    this.applyLabels();
    this.loadLabels();
    if (this.light) this.subscribe(this.light, () => this.renderData());
  }

  applyLabels() {
    this.model().light_heading = this.labels.light_heading;
    this.showCommandFeedback(this.commandMessage);
    this.renderData();
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
        }
      });
    }
  }

  showCommandFeedback(key) {
    this.commandMessage = key;
    this.model().command_feedback = key ? this.labels[key] : '';
  }

  numeric(module, key) {
    if (!module || module.isOnline === false) return null;
    const field = module.field(key), raw = field?.decimalValue ?? field?.value;
    if (raw == null || typeof raw === 'string' && !raw.trim()) return null;
    const number = typeof raw === 'string' ? Number(raw.replace(',', '.')) : raw;
    return typeof number === 'number' && Number.isFinite(number) ? number : null;
  }

  renderData() {
    const model = this.model();
    const level = this.numeric(this.light, 'Status.Level');
    this.isOn = level === null ? null : level > 0;
    model.light_name = this.light?.name || this.labels.no_light;
    model.light_state = this.isOn === null ? this.labels[this.light ? 'unavailable' : 'not_configured']
      : this.labels[this.isOn ? 'light_on' : 'light_off'];
    model.light_detail = this.isOn === null ? '' : this.labels.device_state;
    if (this.pending && this.isOn !== null && this.isOn === this.desired) this.finishCommand('', false);
    this.field('light_card').get().classList.toggle('is-on', this.isOn === true);
    this.updateLightButton();
  }

  updateLightButton() {
    this.model().light_action = this.pending ? this.labels.waiting : this.isOn ? this.labels.turn_off : this.labels.turn_on;
    const button = this.field('light_toggle').get();
    button.disabled = this.isOn === null || this.pending;
    button.setAttribute('aria-pressed', String(this.isOn === true));
    button.setAttribute('aria-busy', String(this.pending));
  }

  toggleLight() {
    if (this.disposed || !this.light || this.isOn === null || this.pending) return;
    const id = ++this.commandId;
    this.pending = true;this.desired = !this.isOn;
    this.showCommandFeedback('sending');
    this.field('command_feedback').get().classList.remove('is-error');
    this.updateLightButton();
    this.commandTimer = setTimeout(() => {
      if (!this.disposed && this.pending && this.commandId === id)
        this.finishCommand('command_timeout', true);
    }, 8000);
    try {
      const request = this.light.control(this.desired ? 'Control.On' : 'Control.Off');
      if (!request?.subscribe) throw new Error('Command unavailable');
      this.commandSubscription = request.subscribe({next: receipt => {
        if (this.disposed || !this.pending || this.commandId !== id) return;
        const status = receipt?.response?.Status;
        if (receipt?.code != null && (receipt.code < 200 || receipt.code >= 300) || status != null && status !== 0 && status !== '0' && status !== 'Ok') {
          this.finishCommand('command_failed', true);return;
        }
        this.showCommandFeedback('command_sent');
      }, error: () => {
        if (!this.disposed && this.pending && this.commandId === id)
          this.finishCommand('command_failed', true);
      }});
      // A synchronous local adapter may confirm state before returning its subscription.
      if (!this.pending) {this.commandSubscription?.unsubscribe();this.commandSubscription = null;}
    } catch (error) { if (this.pending) this.finishCommand('command_unavailable', true); }
  }

  finishCommand(message, error) {
    clearTimeout(this.commandTimer);this.commandTimer = null;
    this.commandSubscription?.unsubscribe();this.commandSubscription = null;
    this.pending = false;this.desired = null;
    this.showCommandFeedback(message);
    this.field('command_feedback').get().classList.toggle('is-error', error);
    this.updateLightButton();
  }

  onDispose() {
    this.disposed = true;
    this.host?.removeEventListener('isometric:toggle-light', this.sceneToggle);
    clearTimeout(this.commandTimer);
    this.commandSubscription?.unsubscribe();
  }
}
