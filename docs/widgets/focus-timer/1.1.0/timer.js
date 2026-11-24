class FocusTimer extends ControllerInstance {
  settings = {defaultSize: '', sizeOptions: [], boundModules: {
    light: {label: 'Focus light / switch', typeFilter: 'Light,Dimmer,Color,Switch'}
  }};
  onCreate() {
    const el = key => this.field(key).get();
    const it = (document.documentElement.lang || navigator.language).startsWith('it');
    const lengths = {focus:1500, short:300, long:900};
    const names = it ? ['Concentrazione','Pausa breve','Pausa lunga'] : ['Focus','Short break','Long break'];
    let mode = 'focus', seconds = lengths.focus, deadline = 0, completed = 0;
    const paint = () => {
      const light=this.getBoundModule?.('light');
      el('lamp_row').hidden=!light;
      el('lamp').disabled=!!deadline;
      el('lamp_name').textContent=(it?'Controlla ':'Control ')+(light?.name || (it?'luce':'light'));
      el('clock').dataset.mode = mode;
      el('count').textContent = (it ? 'Sessione ' : 'Session ') + (completed % 4 + 1) + ' / 4';
      el('time').textContent = String(Math.floor(seconds/60)).padStart(2,'0') + ':' + String(seconds%60).padStart(2,'0');
      el('dial').style.setProperty('--progress', (seconds/lengths[mode]*100)+'%');
      el('start').textContent = deadline ? (it?'Pausa':'Pause') : seconds === lengths[mode] ? (it?'Avvia':'Start') : (it?'Riprendi':'Resume');
      for (const key of Object.keys(lengths)) el(key).setAttribute('aria-pressed',String(key===mode));
    };
    const stop = () => {
      deadline = 0;
      if (this.activeLight) {
        this.command(this.activeLight, false);
        this.activeLight = null;
      }
    };
    const select = key => { stop(); mode=key; seconds=lengths[key]; paint(); };
    const tick = () => {
      if (!deadline) return;
      seconds = Math.max(0, Math.ceil((deadline-Date.now())/1000));
      if (seconds===0) {
        this.chime();
        if (mode==='focus') { completed++; select(completed%4===0 ? 'long' : 'short'); }
        else select('focus');
      } else paint();
    };
    Object.keys(lengths).forEach((key,i) => {
      el(key).textContent=names[i]; el(key).onclick=()=>select(key);
    });
    el('reset').textContent=it?'Azzera':'Reset';
    el('reset').onclick=()=>select(mode);
    el('start').onclick=()=>{
      el('notice').textContent='';
      if (deadline) { tick(); stop(); }
      else {
        if (window.AudioContext) {
          try { this.audio ||= new AudioContext(); this.audio.resume().catch(()=>{}); } catch {}
        }
        deadline=Date.now()+seconds*1000;
        if(mode==='focus' && el('lamp').checked) {
          this.activeLight=this.getBoundModule?.('light');
          if(this.activeLight) this.command(this.activeLight,true);
        }
      }
      paint();
    };
    this.stop=stop;
    paint();
    this.interval=setInterval(tick,250);
  }
  command(module,on) {
    const failed=()=>{ if (!this.closed) this.field('notice').get().textContent=(document.documentElement.lang || navigator.language).startsWith('it')?'La luce non risponde':'Light did not respond'; };
    try { module.control(on?'Control.On':'Control.Off').subscribe({next:r=>{if(r?.code>=400)failed();},error:failed}); } catch { failed(); }
  }
  chime() {
    if(this.audio?.state!=='running')return;
    const t=this.audio.currentTime;
    for(const [i,hz] of [523,784].entries()){
      const osc=this.audio.createOscillator(),gain=this.audio.createGain(),at=t+i*0.14;
      osc.frequency.value=hz;gain.gain.setValueAtTime(0.06,at);gain.gain.exponentialRampToValueAtTime(0.001,at+0.24);
      osc.connect(gain);gain.connect(this.audio.destination);osc.start(at);osc.stop(at+0.25);
    }
  }
  onDispose(){this.closed=true;clearInterval(this.interval);this.stop?.();this.audio?.close();}
}
