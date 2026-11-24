class TiltMaze extends ControllerInstance {
  settings = {defaultSize:'dashboard', sizeOptions:[]};
  async onCreate() {
    const board = this.field('board').get(), ctx = board.getContext('2d');
    const field = key => this.field(key).get();
    const italian = (document.documentElement.lang || navigator.language).startsWith('it');
    const status = field('status');
    let stages;
    try {
      const res = await fetch(new URL('levels.json', new URL(componentId, document.baseURI)));
      if (!res.ok) throw Error();
      stages = await res.json();
    } catch { status.textContent = italian ? 'Percorso non disponibile' : 'Course unavailable'; return; }
    if (this.closed) return;
    let stage = 0, x, y, calibration, last = 0, enabled = false;
    const size = 60;
    function draw() {
      const grid = stages[stage];
      const floor = ctx.createLinearGradient(0,0,600,420);
      floor.addColorStop(0,'#102f36');floor.addColorStop(1,'#081d2b');
      ctx.fillStyle=floor;ctx.fillRect(0,0,600,420);
      grid.forEach((row,j)=>Array.from(row).forEach((cell,i)=>{
        const px=i*size, py=j*size;
        if(cell==='#'){
          ctx.fillStyle='#04151e';ctx.fillRect(px+3,py+5,size,size-3);
          const face=ctx.createLinearGradient(px,py,px+size,py+size);
          face.addColorStop(0,'#548d8e');face.addColorStop(1,'#28515f');
          ctx.fillStyle=face;ctx.fillRect(px+1,py+1,size-2,size-9);
          ctx.fillStyle='#88bcab';ctx.fillRect(px+2,py+1,size-4,2);
          ctx.fillStyle='#183945';ctx.fillRect(px+1,py+size-8,size-2,7);
        }else if(cell==='G'){
          ctx.fillStyle='#06101b';ctx.beginPath();ctx.arc(px+30,py+30,21,0,Math.PI*2);ctx.fill();
          ctx.strokeStyle='#ffd080';ctx.lineWidth=3;ctx.stroke();
          ctx.fillStyle='#ffa96e';ctx.beginPath();ctx.arc(px+30,py+30,6,0,Math.PI*2);ctx.fill();
        }else{
          ctx.fillStyle='#42656c';ctx.beginPath();ctx.arc(px+30,py+30,1.5,0,Math.PI*2);ctx.fill();
        }
      }));
      const bx=x*size+30,by=y*size+28;
      ctx.fillStyle='#031019';ctx.beginPath();ctx.ellipse(bx+4,by+11,20,13,0,0,2*Math.PI);ctx.fill();
      const ball=ctx.createRadialGradient(bx-7,by-8,1,bx,by,20);
      ball.addColorStop(0,'#fff9dd');ball.addColorStop(.28,'#ffd694');ball.addColorStop(.72,'#c57941');ball.addColorStop(1,'#673d32');
      ctx.fillStyle=ball;ctx.beginPath();ctx.arc(bx,by,19,0,2*Math.PI);ctx.fill();
    }
    function reset() {
      y=stages[stage].findIndex(row=>row.includes('S'));x=stages[stage][y].indexOf('S');
      status.textContent=(italian?'Percorso ':'Course ')+(stage+1)+' / '+stages.length;
      draw();
    }
    const chime=()=>this.chime();
    function move(dx,dy) {
      const cell=stages[stage][y+dy]?.[x+dx];
      if(!cell || cell==='#') return;
      x+=dx;y+=dy;
      if(cell==='G'){chime();stage=(stage+1)%stages.length;reset();} else draw();
    }
    for(const [name,dx,dy] of [['left',-1,0],['right',1,0],['up',0,-1],['down',0,1]])
      field(name).onclick=()=>{this.unlockSound();move(dx,dy);};
    const tilt=field('tilt');
    tilt.textContent=italian?'Inclina':'Tilt';tilt.setAttribute('aria-pressed','false');
    this.onTilt=e=>{
      if(!enabled || document.hidden || e.beta==null || e.gamma==null) return;
      if(!calibration){calibration=[e.gamma,e.beta];return;}
      if(performance.now()-last<180) return;
      const angle=(screen.orientation?.angle || 0)*Math.PI/180;
      const h=e.gamma-calibration[0],v=e.beta-calibration[1];
      const dx=h*Math.cos(angle)+v*Math.sin(angle),dy=v*Math.cos(angle)-h*Math.sin(angle);
      if(Math.max(Math.abs(dx),Math.abs(dy))<9) return;
      last=performance.now();
      if(Math.abs(dx)>Math.abs(dy))move(Math.sign(dx),0);else move(0,Math.sign(dy));
    };
    tilt.onclick=async()=>{
      this.unlockSound();
      if(!enabled && typeof DeviceOrientationEvent==='undefined') return;
      if(!enabled && DeviceOrientationEvent.requestPermission){
        try { if(await DeviceOrientationEvent.requestPermission()!=='granted') return; } catch { return; }
      }
      if(this.closed)return;
      enabled=!enabled;calibration=null;tilt.setAttribute('aria-pressed',String(enabled));
    };
    window.addEventListener('deviceorientation',this.onTilt);
    field('reset').onclick=()=>{calibration=null;reset();};
    reset();
  }
  unlockSound(){if(window.AudioContext){try{this.audio ||= new AudioContext();this.audio.resume().catch(()=>{});}catch{}}}
  chime() {
    if(this.audio?.state!=='running')return;
    const t=this.audio.currentTime;
    for(const [i,hz] of [523,784].entries()){
      const osc=this.audio.createOscillator(),gain=this.audio.createGain(),at=t+i*0.14;
      osc.frequency.value=hz;gain.gain.setValueAtTime(0.06,at);gain.gain.exponentialRampToValueAtTime(0.001,at+0.24);
      osc.connect(gain);gain.connect(this.audio.destination);osc.start(at);osc.stop(at+0.25);
    }
  }
  onDispose(){this.audio?.close();this.closed=true;if(this.onTilt)window.removeEventListener('deviceorientation',this.onTilt);}
}
