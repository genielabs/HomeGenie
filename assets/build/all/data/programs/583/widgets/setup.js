class AiGenieSetupWidget extends ControllerInstance {
  onCreate() {
    const fields = ['intro', 'configure',
      'cloud', 'providers', 'local', 'localproviders', 'feedback', 'retry'];
    this.fields = Object.fromEntries(fields.map(name => [name, this.field(name).get()]));
    this.messages = {};
    this.moduleSubscriptions = new Map();
    this.listeners = new AbortController();
    const {signal} = this.listeners;
    this.fields.configure.addEventListener('click', () => this.openSettings(this.data?.SettingsModuleId), {signal});
    this.fields.retry.addEventListener('click', () => this.loadProviders(), {signal});
    const defaults = {
      intro: "Use HomeGenie chat to control devices, schedule actions, and create automations, widgets, and dashboards.",
      configure: 'Configure AI Genie',
      cloud: 'Cloud providers',
      local: 'Local provider',
      hasKey: 'Key configured',
      missingKey: 'Configure API key',
      disabled: 'Program disabled',
      modelReady: 'Model ready',
      modelDownload: 'Downloading model',
      modelStarting: 'Starting model',
      modelError: 'Model error',
      modelSelected: 'Model selected',
      modelMissing: 'Choose a model',
      noProviders: 'No cloud providers installed.',
      noLocalProviders: 'Lailama is not installed.',
      settings: 'Settings',
      settingsUnavailable: 'The program settings are not available yet.',
      loading: 'Loading providers…',
      listFailed: 'Could not load the providers.',
      retry: 'Retry'
    };
    this.translations = Object.entries(defaults).map(([key, fallback]) =>
      this.translate(key, {scope: 'widget', fallback}).subscribe(message => {
        this.messages[key] = message;
        if (key !== 'providers' && this.fields[key]) this.fields[key].textContent = message;
        if (this.data) this.renderProviders();
      }));
    this.feedback = 'loading';
    this.loadProviders();
  }

  loadProviders() {
    this.request?.unsubscribe();
    clearTimeout(this.requestTimer);
    const receive = data => {
      clearTimeout(this.requestTimer);
      if (data?.Status === 'Ok') {
        this.data = data;
        this.feedback = null;
        this.watchPrograms([...data.Providers, ...data.LocalProviders]);
      } else {
        this.feedback = 'listFailed';
      }
      this.renderProviders();
    };
    this.requestTimer = setTimeout(() => {
      this.request?.unsubscribe();
      receive(null);
    }, 12000);
    this.request = this.apiCall('System.Utility/AiGenie/Setup.Providers', undefined, {}).subscribe({
      next: ({response}) => receive(response),
      error: () => receive(null)
    });
    this.renderFeedback();
  }

  watchPrograms(providers) {
    for (const provider of providers) {
      const id = provider.ModuleId;
      if (!id || this.moduleSubscriptions.has(id)) continue;
      const module = this.getModule(id);
      if (!module) continue;
      const subscription = this.subscribe(module, field => {
        if (!['Status.Init', 'Program.Status', 'ConfigureOptions.ApiKey', 'ConfigureOptions.ModelFile'].includes(field.key)) return;
        clearTimeout(this.refreshTimer);
        this.refreshTimer = setTimeout(() => this.loadProviders(), 200);
      });
      this.moduleSubscriptions.set(id, subscription);
    }
  }

  openSettings(id) {
    if (id && this.getModule(id)) {
      this.feedback = null;
      this.showSettings(id);
    } else {
      this.feedback = 'settingsUnavailable';
    }
    this.renderFeedback();
  }

  renderProviders() {
    const data = this.data;
    this.fields.configure.hidden = !data?.SettingsModuleId;
    this.renderList(this.fields.providers, data?.Providers, false);
    this.renderList(this.fields.localproviders, data?.LocalProviders, true);
    this.renderFeedback();
  }

  renderFeedback() {
    this.fields.feedback.hidden = !this.feedback;
    this.fields.feedback.textContent = this.messages[this.feedback] || '';
    this.fields.retry.hidden = this.feedback !== 'listFailed' && this.feedback !== 'settingsUnavailable';
  }

  renderList(container, providers, local) {
    const rows = (providers || []).map(provider => {
      const state = local
        ? {Ready: 'modelReady', Download: 'modelDownload', Starting: 'modelStarting', WarmingUp: 'modelStarting',
          Error: 'modelError'}[provider.ModelState] || (provider.HasModel ? 'modelSelected' : 'modelMissing')
        : provider.HasKey ? 'hasKey' : 'missingKey';
      const statusText = [this.messages[state], provider.Enabled ? '' : this.messages.disabled].filter(Boolean).join(' · ');
      const ready = state === 'modelReady' || state === 'hasKey';
      const icon = state === 'modelError' ? 'error_outline' : state === 'modelDownload' ? 'downloading' :
        state === 'modelStarting' ? 'hourglass_top' : ready ? 'check_circle' : state === 'missingKey' ? 'vpn_key_alert' : 'info';
      const row = document.createElement('li');
      row.className = 'setup-provider';
      const name = document.createElement('span');
      name.className = 'setup-provider-name';
      name.textContent = provider.Name;
      const status = document.createElement('span');
      status.className = 'material-symbols-outlined setup-provider-status';
      status.classList.toggle('ready', ready && provider.Enabled);
      status.classList.toggle('error', state === 'modelError');
      status.setAttribute('role', 'img');
      status.title = statusText;
      status.setAttribute('aria-label', statusText);
      status.textContent = icon;
      const settings = document.createElement('button');
      settings.type = 'button';
      settings.title = this.messages.settings || '';
      settings.setAttribute('aria-label', settings.title + ' · ' + provider.Name);
      settings.addEventListener('click', () => this.openSettings(provider.ModuleId));
      const gear = document.createElement('span');
      gear.className = 'material-symbols-outlined';
      gear.setAttribute('aria-hidden', 'true');
      gear.textContent = 'settings';
      settings.appendChild(gear);
      row.append(name, status, settings);
      return row;
    });
    if (providers && !rows.length) {
      const empty = document.createElement('li');
      empty.className = 'setup-empty';
      empty.textContent = this.messages[local ? 'noLocalProviders' : 'noProviders'] || '';
      rows.push(empty);
    }
    container.replaceChildren(...rows);
  }

  onDispose() {
    this.listeners?.abort();
    clearTimeout(this.requestTimer);
    clearTimeout(this.refreshTimer);
    this.request?.unsubscribe();
    this.moduleSubscriptions?.forEach(subscription => subscription.unsubscribe());
    this.translations?.forEach(subscription => subscription.unsubscribe());
  }
}
