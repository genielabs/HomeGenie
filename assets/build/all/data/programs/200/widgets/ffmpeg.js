class FfmpegSetup extends ControllerInstance {
  onCreate() {
    this.messages = {};
    this.translations = [];
    this.elements = Object.fromEntries(['title', 'description', 'status', 'progress', 'details', 'command', 'action', 'note'].map(
      key => [key, this.field(key).get()]));
    this.state = 'Checking';
    this.declare({prepare: () => this.request(true)});
    const defaults = {
      title: 'VisionCore NVR',
      description: 'Install FFmpeg to enable live camera streams and video recording in HomeGenie.',
      later: 'You can install it later from the Programs page. Use the Configure icon next to Video Input.',
      wait: 'Wait for FFmpeg to finish installing.',
      Checking: 'Checking for FFmpeg…',
      Ready: 'FFmpeg is ready.',
      Manual: 'Install FFmpeg on the HomeGenie server using its package manager, then select Check again.',
      Downloading: 'Downloading FFmpeg…',
      Installing: 'Installing FFmpeg…',
      Paused: 'The download is paused.',
      Error: 'FFmpeg could not be installed.',
      Unavailable: 'The video service is temporarily unavailable.',
      install: 'Install FFmpeg',
      resume: 'Resume download',
      retry: 'Try again',
      check: 'Check again'
    };
    Object.entries(defaults).forEach(([key, fallback]) => {
      const subscription = this.translate(key, {scope: 'widget', fallback}).subscribe(text => {
        this.messages[key] = text;
        this.render();
      });
      this.translations.push(subscription);
    });
    this.request(false);
  }

  request(prepare) {
    if (this.pending) return;
    clearTimeout(this.pollTimer);
    this.pending = true;
    this.render();
    const schedule = () => {
      this.pending = false;
      this.render();
      if (['Checking', 'Downloading', 'Installing', 'Unavailable'].includes(this.state)) {
        this.pollTimer = setTimeout(() => this.request(false), 1000);
      }
    };
    const unavailable = () => {
      clearTimeout(this.requestTimer);
      this.state = 'Unavailable';
      this.details = '';
      schedule();
    };
    const method = prepare ? 'FFmpeg.Prepare' : 'FFmpeg.Status';
    this.requestSubscription = this.apiCall('Media.Video/service/' + method, prepare ? {} : undefined, {}).subscribe({
      next: ({response}) => {
        clearTimeout(this.requestTimer);
        if (!response || typeof response.State !== 'string') {
          unavailable();
          return;
        }
        this.state = response.Available ? 'Ready' : response.State;
        this.details = this.state === 'Error' ? response.ErrorMessage : '';
        this.installCommand = response.InstallCommand;
        this.progress = response.ProgressPercentage;
        schedule();
      },
      error: unavailable
    });
    this.requestTimer = setTimeout(() => {
      this.requestSubscription?.unsubscribe();
      unavailable();
    }, 5000);
  }

  render() {
    const {title, description, status, progress, details, command, action, note} = this.elements;
    title.textContent = this.messages.title || '';
    description.textContent = this.messages.description || '';
    status.hidden = this.state === 'Missing';
    status.textContent = this.messages[this.state] || '';
    progress.hidden = this.state !== 'Downloading';
    if (!progress.hidden) {
      if (this.progress > 0) progress.value = Math.min(this.progress, 100);
      else progress.removeAttribute('value');
    }
    details.hidden = !this.details;
    details.textContent = this.details || '';
    command.hidden = this.state !== 'Manual' || !this.installCommand;
    command.textContent = this.installCommand || '';
    const label = {Missing: 'install', Manual: 'check', Paused: 'resume', Error: 'retry'}[this.state];
    action.hidden = !label;
    action.disabled = this.pending;
    action.textContent = this.messages[label] || '';
    note.hidden = this.state === 'Ready';
    note.textContent = this.messages[['Checking', 'Downloading', 'Installing'].includes(this.state) ? 'wait' : 'later'] || '';
  }

  onDispose() {
    clearTimeout(this.pollTimer);
    clearTimeout(this.requestTimer);
    this.requestSubscription?.unsubscribe();
    this.translations?.forEach(subscription => subscription.unsubscribe());
  }
}
