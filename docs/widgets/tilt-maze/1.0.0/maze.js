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
      grid.forEach((row,j) => Array.from(row).forEach((cell,i) => {
        ctx.fillStyle = cell === '#' ? '#916949' : (i+j)%2 ? '#f1c999' : '#eac192';
        ctx.fillRect(i*size,j*size,size,size);
        if (cell === '#') {
          ctx.fillStyle='#bc9367';ctx.fillRect(i*size,j*size,size,5);
        }
        if(cell==='G'){
          ctx.fillStyle='#287969';ctx.beginPath();ctx.arc(i*size+30,j*size+30,19,0,2*Math.PI);ctx.fill();
        }
      }));
      const ball=ctx.createRadialGradient(x*size+24,y*size+23,1,x*size+30,y*size+30,20);
      ball.addColorStop(0,'#fffbe9');ball.addColorStop(0.5,'#91a8aa');ball.addColorStop(1,'#263a42');
      ctx.fillStyle=ball;ctx.beginPath();ctx.arc(x*size+30,y*size+30,19,0,2*Math.PI);ctx.fill();
    }
    function reset() {
      y=stages[stage].findIndex(row=>row.includes('S'));x=stages[stage][y].indexOf('S');
      status.textContent=(italian?'Percorso ':'Course ')+(stage+1)+' / '+stages.length;
      draw();
    }
    function move(dx,dy) {
      const cell=stages[stage][y+dy]?.[x+dx];
      if(!cell || cell==='#') return;
      x+=dx;y+=dy;
      if(cell==='G'){stage=(stage+1)%stages.length;reset();} else draw();
    }
    for(const [name,dx,dy] of [['left',-1,0],['right',1,0],['up',0,-1],['down',0,1]])
      field(name).onclick=()=>move(dx,dy);
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
  onDispose(){this.closed=true;if(this.onTilt)window.removeEventListener('deviceorientation',this.onTilt);}
}
