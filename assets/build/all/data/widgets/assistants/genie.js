class GenieAvatar extends ControllerInstance {
  settings = {
    defaultSize: 'small',
    sizeOptions: ['small', 'medium'],
    transparentCard: true
  };

  onInit() {
    this.declare({openChat: () => this.openAiAvatar?.()});
  }

  onCreate() {
    const avatar = this.field('avatar').get();
    const button = this.field('open_button').get();
    avatar.dataset.state = 'idle';
    button.disabled = typeof this.openAiAvatar !== 'function';
    this.watchGenieActivity?.(state => { avatar.dataset.state = state; });
    this.translation = this.translate?.('open', {scope: 'widget', fallback: 'Open AI Genie'})
      ?.subscribe(text => button.setAttribute('aria-label', text));
  }

  onDispose() {
    this.translation?.unsubscribe();
  }
}
