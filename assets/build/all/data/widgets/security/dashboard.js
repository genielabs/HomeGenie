class SecurityDashboard extends ControllerInstance {
  settings = {
    defaultSize: 'dashboard', sizeOptions: [],
    boundModules: {
      alarm: {
        label: 'Alarm system', labelKey: 'alarm_label', fieldFilter: 'HomeGenie.SecurityArmed',
        defaultModule: 'HomeAutomation.SecuritySystem:Main'
      },
      camera1: {label: 'Camera 1', labelKey: 'camera1', typeFilter: 'VideoInput', defaultModule: 'Media.Video:1'},
      camera2: {label: 'Camera 2', labelKey: 'camera2', typeFilter: 'VideoInput', defaultModule: 'Media.Video:2'},
      contact1: {
        label: 'Door/window sensor 1', labelKey: 'contact1', typeFilter: 'DoorWindow',
        defaultModule: 'HomeAutomation.Demo:2'
      },
      contact2: {
        label: 'Door/window sensor 2', labelKey: 'contact2', typeFilter: 'DoorWindow'
      },
      motion: {
        label: 'Motion detection', labelKey: 'motion', fieldFilter: 'Sensor.MotionDetect',
        defaultModule: 'HomeAutomation.Demo:1'
      }
    }
  };

  labels = {
    cameras_label: 'Cameras', events_label: 'Recent events', alarm_label: 'Alarm system', sensors_label: 'Sensors',
    arm_home: 'Home', arm_away: 'Away', disarm: 'Deactivate', disarmed: 'Disarmed', armed: 'Armed',
    arming: 'Waiting', triggered: 'Alarmed', not_set: 'Not set', offline: 'Offline', error: 'Error!',
    door_open: 'Opened', door_closed: 'Closed', detected: 'Detected', active: 'Active', idle: 'Idle',
    no_events: 'No recent events'
  };

  history = [];
  disposed = false;

  onInit() {
    this.declare({sendAlarm: command => this.sendAlarm(command)});
  }

  onCreate() {
    const layout = this.element('layout');
    const body = layout.ownerDocument.body;
    const applyTheme = () => layout.toggleAttribute('data-dark', body.classList.contains('dark-theme'));
    this.themeObserver = new MutationObserver(applyTheme);
    this.themeObserver.observe(body, {attributes: true, attributeFilter: ['class']});
    applyTheme();
    this.alarm = this.getBoundModule('alarm');
    this.sensors = new Map();
    for (const [role, kind] of [['contact1', 'contact'], ['contact2', 'contact'], ['motion', 'motion']]) {
      const module = this.getBoundModule(role);
      if (module) this.sensors.set(module, kind);
    }
    for (const module of this.sensors.keys()) this.subscribe(module, () => this.renderSensors());
    if (this.alarm) this.subscribe(this.alarm, ({key}) => {
      if (key === 'Program.UiRefresh') this.scheduleHistory();
      this.renderAlarm();
    });
    const firstCamera = this.getBoundModule('camera1');
    const secondCamera = this.getBoundModule('camera2');
    this.element('cameras').hidden = !firstCamera && !secondCamera;
    this.element('camera_one').hidden = !firstCamera;
    this.element('camera_two').hidden = !secondCamera;
    if (firstCamera || secondCamera) import('/widgets/security/camera-view.module.js').catch(() => {
      if (!this.disposed) this.element('cameras').hidden = true;
    });
    this.view().on('refresh:active', () => this.scheduleHistory());
    this.renderLabels();
    this.scheduleHistory();
    this.loadLabels();
  }

  loadLabels() {
    for (const [key, fallback] of Object.entries(this.labels)) {
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        if (this.disposed || !value || value === fallback) return;
        this.labels[key] = value;
        this.renderLabels();
      });
    }
  }

  element(name) { return this.field(name).get(); }
  write(name, value) { this.element(name).textContent = value; }

  renderLabels() {
    for (const key of ['cameras_label', 'events_label', 'alarm_label', 'sensors_label', 'arm_home', 'arm_away', 'disarm']) {
      this.write(key, this.labels[key]);
    }
    this.renderAlarm();
    this.renderSensors();
    this.renderHistory();
  }

  get alarmState() {
    if (!this.alarm) return 'not_set';
    if (this.alarm.isOnline === false) return 'offline';
    if (Number(this.alarm.field('HomeGenie.SecurityTriggered')?.value) > 0) return 'triggered';
    const level = this.alarm.field('Status.Level')?.value;
    if (level == null || level === '' || !Number.isFinite(Number(level))) return 'offline';
    if (Number(level) === 0) return 'disarmed';
    const mode = this.alarm.field('HomeGenie.SecurityArmed')?.value;
    if (mode === 'Home' || mode === 'Away') return mode.toLowerCase();
    return 'arming';
  }

  alarmText(state) {
    if (state === 'home' || state === 'away') return this.labels.armed + ' · ' + this.labels['arm_' + state];
    return this.labels[state];
  }

  renderAlarm() {
    const state = this.alarmState;
    this.element('alarm').dataset.state = state;
    this.write('alarm_icon', state === 'triggered' ? 'notification_important'
      : ['home', 'away'].includes(state) ? 'verified_user' : 'shield');
    this.write('alarm_status', this.alarmText(state));
    const available = !['not_set', 'offline'].includes(state);
    this.element('alarm_actions').hidden = !available;
    for (const key of ['arm_home', 'arm_away', 'disarm']) {
      const button = this.element(key);
      button.hidden = key === 'disarm' ? state === 'disarmed' : state !== 'disarmed';
      button.disabled = !available || Boolean(this.command);
    }
    this.write('command_error', this.commandFailed ? this.labels.error : '');
  }

  sendAlarm(command) {
    const state = this.alarmState;
    const allowed = state === 'disarmed' ? ['Control.ArmHome', 'Control.ArmAway']
      : ['not_set', 'offline'].includes(state) ? [] : ['Control.Disarm'];
    if (this.disposed || this.command || !allowed.includes(command)) return;
    const request = {};
    this.command = request;
    this.commandFailed = false;
    this.renderAlarm();
    const finish = ok => {
      if (this.command !== request) return;
      this.command = undefined;
      clearTimeout(request.timer);
      request.subscription?.unsubscribe();
      this.commandFailed = !ok;
      this.renderAlarm();
    };
    request.timer = setTimeout(() => finish(false), 8000);
    try {
      const result = this.alarm.control(command);
      if (!result) return finish(false);
      request.subscription = result.subscribe({
        next: reply => finish(reply?.code === undefined || Number(reply.code) === 200),
        error: () => finish(false)
      });
      if (this.command !== request) request.subscription.unsubscribe();
    } catch {
      finish(false);
    }
  }

  sensorState(kind, value) {
    if (value == null || value === '' || !Number.isFinite(Number(value))) return ['offline', 'neutral'];
    if (kind === 'contact') return Number(value) > 0 ? ['door_open', 'attention'] : ['door_closed', 'good'];
    return Number(value) > 0 ? ['detected', 'attention'] : ['idle', 'neutral'];
  }

  row(list, icon, name, state) {
    const doc = list.ownerDocument;
    const row = doc.createElement('li');
    const glyph = doc.createElement('span');
    glyph.className = 'material-symbols-outlined';
    glyph.setAttribute('aria-hidden', 'true');
    glyph.textContent = icon;
    const title = doc.createElement('span');
    title.textContent = name;
    row.append(glyph, title, state);
    return row;
  }

  renderSensors() {
    const list = this.element('sensors');
    const rows = Array.from(this.sensors, ([module, kind]) => {
      const field = kind === 'contact' ? module.field('Sensor.DoorWindow') ?? module.field('Status.Level')
        : module.field('Sensor.MotionDetect');
      const [label, tone] = this.sensorState(kind, module.isOnline === false ? undefined : field?.value);
      const badge = list.ownerDocument.createElement('span');
      badge.className = 'sensor-state';
      badge.dataset.tone = tone;
      badge.textContent = this.labels[label];
      return this.row(list, kind === 'contact' ? 'door_front' : 'sensors', module.name, badge);
    });
    list.replaceChildren(...rows);
    this.write('sensors_status', rows.length ? '' : this.labels.not_set);
  }

  scheduleHistory() {
    clearTimeout(this.historyTimer);
    if (!this.disposed && this.alarm) this.historyTimer = setTimeout(() => this.loadHistory(), 250);
  }

  loadHistory() {
    if (this.historyRequest) {
      this.historyQueued = true;
      return;
    }
    const request = {};
    this.historyRequest = request;
    const finish = rows => {
      if (this.historyRequest !== request) return;
      this.historyRequest = undefined;
      clearTimeout(request.timer);
      request.subscription?.unsubscribe();
      this.historyFailed = !Array.isArray(rows);
      if (!this.historyFailed) this.history = this.recentEvents(rows);
      this.renderHistory();
      if (this.historyQueued) {
        this.historyQueued = false;
        this.scheduleHistory();
      }
    };
    request.timer = setTimeout(() => finish(), 8000);
    try {
      const result = this.alarm.control('Events.List');
      if (!result) return finish();
      request.subscription = result.subscribe({next: finish, error: () => finish()});
      if (this.historyRequest !== request) request.subscription.unsubscribe();
    } catch {
      finish();
    }
  }

  recentEvents(rows) {
    const ordered = rows.filter(event => event?.field && event.module && Number.isFinite(event.field.timestamp))
      .sort((a, b) => b.field.timestamp - a.field.timestamp);
    let previousAlarmField;
    const disarmed = field => field.key === 'HomeGenie.SecurityArmed' && field.value === 'Disarmed' ||
      field.key === 'Status.Level' && String(field.value) === '0';
    return ordered.filter(event => {
      if (event.module !== this.alarm) return true;
      const field = event.field;
      // Disarming emits both Status.Level and HomeGenie.SecurityArmed.
      if (previousAlarmField && field.key !== previousAlarmField.key && disarmed(field) &&
          disarmed(previousAlarmField) && previousAlarmField.timestamp - field.timestamp <= 15_000) return false;
      previousAlarmField = field;
      return true;
    }).slice(0, 6);
  }

  eventState(event) {
    const {key, value} = event.field;
    if (key === 'HomeGenie.SecurityArmed') {
      const mode = value === 'Home' || value === 'Away' || value === 'Disarmed' ? value.toLowerCase() : 'offline';
      return ['shield', this.alarmText(mode)];
    }
    if (key === 'HomeGenie.SecurityTriggered') return ['notification_important', this.labels[Number(value) > 0 ? 'triggered' : 'idle']];
    if (key === 'Status.Level' && event.module === this.alarm) {
      return ['shield', this.labels[Number(value) > 0 ? 'arming' : 'disarmed']];
    }
    if (key === 'Sensor.DoorWindow' || (key === 'Status.Level' && event.module.type?.toLowerCase() === 'doorwindow')) {
      return ['door_front', this.labels[this.sensorState('contact', value)[0]]];
    }
    const motionEvent = key === 'Sensor.MotionDetect' || key === 'Sensor.ObjectDetect';
    const binaryEvent = key === 'Status.Level' || key === 'Sensor.Generic';
    const motionModule = binaryEvent && (this.sensors?.get(event.module) === 'motion' ||
      event.module.field?.('Sensor.MotionDetect') != null || event.module.field?.('Sensor.ObjectDetect') != null);
    if (motionEvent || motionModule) return ['sensors', this.labels[this.sensorState('motion', value)[0]]];
    if (key === 'Status.Level' && event.module.type?.toLowerCase() === 'sensor') {
      const level = String(value).trim();
      if (level === '0' || level === '1') return ['sensors', this.labels[level === '1' ? 'active' : 'idle']];
    }
    return ['history', typeof value === 'string' || typeof value === 'number' ? String(value) : ''];
  }

  renderHistory() {
    const list = this.element('events');
    const doc = list.ownerDocument;
    const rows = this.history.map(event => {
      const date = new Date(event.field.timestamp);
      const time = doc.createElement('time');
      time.dateTime = date.toISOString();
      time.textContent = date.toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});
      time.title = date.toLocaleString();
      const [icon, text] = this.eventState(event);
      const row = this.row(list, icon, event.module.name, time);
      const title = row.children[1];
      title.className = 'event-title';
      const details = doc.createElement('small');
      details.className = 'event-state';
      details.textContent = text;
      title.append(details);
      row.prepend(time);
      return row;
    });
    list.replaceChildren(...rows);
    this.write('events_status', this.historyFailed ? this.labels.error
      : rows.length ? '' : this.labels[this.alarm ? 'no_events' : 'not_set']);
  }

  onDispose() {
    this.disposed = true;
    this.themeObserver?.disconnect();
    clearTimeout(this.historyTimer);
    for (const request of [this.historyRequest, this.command]) {
      clearTimeout(request?.timer);
      request?.subscription?.unsubscribe();
    }
    this.historyRequest = undefined;
    this.command = undefined;
  }
}
