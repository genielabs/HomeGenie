class WeatherStation extends ControllerInstance {
  settings={defaultSize:'',sizeOptions:[],boundModules:{weather:{label:'Weather station',fieldFilter:'Conditions.IconType,Conditions.DisplayLocation,Sensor.Temperature'}}};
  onCreate(){
    const module=this.getBoundModule?.('weather') || this.boundModule;
    const display=(key,value)=>{this.field(key).get().textContent=value ?? '—';};
    const read=k=>module?.field(k)?.value;
    const metric=(k,unit)=>{const v=read(k);return v==null || v===''?'—':v+' '+unit;};
    const it=(document.documentElement.lang || navigator.language).startsWith('it');
    let observedAt = 0;
    const stamp = value => {
      if (!value) return 0;
      const n = Number(value);
      const time = Number.isFinite(n) ? (n < 1e11 ? n * 1000 : n) : Date.parse(value);
      return time > 0 ? time : 0;
    };
    const freshness=()=>{
      if(!module)return it?'Nessuna stazione':'No station';
      if(!observedAt)return it?'Orario sconosciuto':'Update time unknown';
      const mins=Math.floor(Math.max(0,Date.now()-observedAt)/60000);
      if(!mins)return it?'Appena aggiornato':'Updated just now';
      const amount=mins<60?mins:mins<1440?Math.floor(mins/60):Math.floor(mins/1440);
      const unit=mins<60?'min':mins<1440?'h':(it?'g':'d');
      return it ? `Aggiornato ${amount} ${unit} fa` : `Updated ${amount} ${unit} ago`;
    };
    const paint=()=>{
      observedAt=stamp(read('Conditions.Timestamp')) || Math.max(...[
        'Sensor.Temperature','Sensor.Humidity','Conditions.Description','Conditions.IconType'
      ].map(key=>stamp(module?.field(key)?.timestamp)));
      const temp=read('Sensor.Temperature');
      display('city_name',read('Conditions.DisplayLocation') || read('Conditions.City') || module?.name || 'Weather Station');
      display('status_code',freshness());
      display('temp_val',temp==null || temp===''?'—':utils.format.fieldValue({key:'Sensor.Temperature',value:temp}));
      display('unit',utils.preferences?.units?.temperature==='F'?'℉':'℃');
      display('condition_text',read('Conditions.Description'));
      const icon=String(read('Conditions.IconType') || '');
      const code=icon.slice(0,2);
      display('weather_icon',({'01':icon.endsWith('n')?'☾':'☀','02':icon.endsWith('n')?'☁☾':'⛅','03':'☁','04':'☁','09':'☂','10':'☂','11':'ϟ','13':'❄','50':'≋'})[code] || '◌');
      display('humidity_val',metric('Sensor.Humidity','%'));
      display('wind_val',metric('Sensor.Wind.Speed','km/h'));
      display('pressure_val',metric('Sensor.Pressure','hPa'));
      display('rain_val',metric('Sensor.Precipitation.Rain','mm'));
    };
    this.clock=setInterval(()=>display('status_code',freshness()),30000);
    paint();if(module)this.subscription=this.subscribe(module,paint);
  }
  onDispose(){clearInterval(this.clock);this.subscription?.unsubscribe();}
}
