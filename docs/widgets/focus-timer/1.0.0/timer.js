class FocusTimer extends ControllerInstance {
  settings = {defaultSize: '', sizeOptions: []};
  onCreate() {
    const italian = document.documentElement.lang.startsWith('it') || (!document.documentElement.lang && navigator.language.startsWith('it'));
    const words = italian ? ['Concentrazione','Pausa breve','Avvia','Pausa','Riprendi','Azzera'] : ['Focus','Short break','Start','Pause','Resume','Reset'];
    const time = this.field('time').get(), mode = this.field('mode').get();
    const start = this.field('start').get(), reset = this.field('reset').get();
    let rest = false, remaining = 1500, end = 0;
    function paint() {
      if (end) {
        remaining = Math.max(0, Math.ceil((end - Date.now()) / 1000));
        if (!remaining) {
          rest = !rest;
          remaining = rest ? 300 : 1500;
          end = 0;
        }
      }
      mode.textContent = words[rest ? 1 : 0];
      time.textContent = String(Math.floor(remaining / 60)).padStart(2,'0') + ':' + String(remaining % 60).padStart(2,'0');
      start.textContent = words[end ? 3 : remaining === (rest ? 300 : 1500) ? 2 : 4];
      reset.textContent = words[5];
    }
    start.onclick = () => { paint(); end = end ? 0 : Date.now() + remaining * 1000; paint(); };
    reset.onclick = () => { end = 0; rest = false; remaining = 1500; paint(); };
    paint();
    this.timer = setInterval(paint, 500);
  }
  onDispose() { clearInterval(this.timer); }
}
