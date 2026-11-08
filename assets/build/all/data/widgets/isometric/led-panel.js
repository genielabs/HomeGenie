class LedPanel extends ControllerInstance {
  state = {on: null, effect: null, colorDisabled: true};
  labels = {
    matrix_heading: 'LED panel',
    matrix_demo: 'Local demo · 16×16',
    matrix_effect: 'Effect',
    matrix_waves: 'Waves',
    matrix_solid: 'Solid color',
    matrix_ghost: 'Pac-Man ghost',
    matrix_heart: 'Pulsing heart',
    matrix_color: 'Color',
    matrix_on: 'Turn panel on',
    matrix_off: 'Turn panel off',
    matrix_wait: 'Waiting for the light…',
    matrix_failed: 'The light did not confirm the change.',
    matrix_load_error: 'This effect could not be loaded.',
    light_on: 'On',
    light_off: 'Off',
    unknown_state: 'Unknown state'
  };

  onCreate() {
    this.fields = Object.fromEntries([
      'card', 'heading', 'source', 'status', 'color_label', 'color', 'effect_field',
      'effect_label', 'effect', 'waves', 'solid', 'ghost', 'heart', 'toggle', 'action', 'feedback'
    ].map(name => [name, this.field(name).get()]));
    this.events = new AbortController();
    const {signal} = this.events;
    const {toggle, color, effect} = this.fields;
    toggle.addEventListener('click', () => this.emit('toggle'), {signal});
    color.addEventListener('change', () => this.emit('color', color.value), {signal});
    effect.addEventListener('change', () => this.emit('effect', effect.value), {signal});
    this.renderLabels();
    this.render();
    this.labelSubscriptions = Object.entries(this.labels).map(([key, fallback]) =>
      this.translate?.(key, {scope: 'widget', fallback})?.subscribe(value => {
        this.labels[key] = value;
        this.renderLabels();
        this.render();
      }));

    // The host owns the state. Its ready listener supplies a snapshot and retains the update callback.
    this.emit('ready', {setState: state => this.setState(state)});
  }

  emit(action, detail) {
    this.fields.card.dispatchEvent(new CustomEvent('led-panel-' + action, {bubbles: true, composed: true, detail}));
  }

  setState(state) {
    if (this.events.signal.aborted) return;
    this.state = state;
    this.render();
  }

  renderLabels() {
    const {heading, color_label, effect_label, waves, solid, ghost, heart} = this.fields;
    const labels = this.labels;
    heading.textContent = labels.matrix_heading;
    color_label.textContent = labels.matrix_color;
    effect_label.textContent = labels.matrix_effect;
    waves.textContent = labels.matrix_waves;
    solid.textContent = labels.matrix_solid;
    ghost.textContent = labels.matrix_ghost;
    heart.textContent = labels.matrix_heart;
  }

  render() {
    const {card, source, status, color, effect_field, effect, toggle, action, feedback} = this.fields;
    const state = this.state, labels = this.labels;
    source.textContent = state.source || (state.demo ? labels.matrix_demo : '');
    status.textContent = labels[state.on == null ? 'unknown_state' : state.on ? 'light_on' : 'light_off'];
    action.textContent = labels[state.busy ? 'matrix_wait' : state.on ? 'matrix_off' : 'matrix_on'];
    feedback.textContent = labels[state.feedback] || '';
    card.classList.toggle('is-on', state.on === true);
    effect_field.hidden = state.effect == null;
    if (!effect_field.hidden) effect.value = state.effect;
    toggle.disabled = state.on == null || !!state.busy;
    toggle.setAttribute('aria-pressed', String(state.on === true));
    toggle.setAttribute('aria-busy', String(!!state.busy));
    color.disabled = state.colorDisabled === true;
    if (state.color) color.value = state.color;
  }

  onDispose() {
    this.events?.abort();
    this.labelSubscriptions?.forEach(subscription => subscription?.unsubscribe());
  }
}
