class WeatherStation extends ControllerInstance {
  settings={defaultSize:'',sizeOptions:[],boundModules:{weather:{label:'Weather station',fieldFilter:'Conditions.IconType,Conditions.DisplayLocation,Sensor.Temperature'}}};
  onCreate(){
    const module=this.getBoundModule?.('weather') || this.boundModule;
    const display=(key,value)=>{this.field(key).get().textContent=value ?? '—';};
    const read=k=>module?.field(k)?.value;
    const metric=(k,unit)=>{const v=read(k);return v==null || v===''?'—':v+' '+unit;};
    const paint=()=>{
      const temp=read('Sensor.Temperature');
      display('city_name',read('Conditions.DisplayLocation') || read('Conditions.City') || module?.name || 'Weather Station');
      display('status_code',module?'CONNECTED':'NO STATION');
      display('temp_val',temp==null || temp===''?'—':utils.format.fieldValue({key:'Sensor.Temperature',value:temp}));
      display('unit',utils.preferences?.units?.temperature==='F'?'℉':'℃');
      display('condition_text',read('Conditions.Description'));
      const code=String(read('Conditions.IconType') || '').slice(0,2);
      display('weather_icon',({'01':'☀','02':'⛅','03':'☁','04':'☁','09':'☂','10':'☂','11':'ϟ','13':'❄','50':'≋'})[code] || '◌');
      display('humidity_val',metric('Sensor.Humidity','%'));
      display('wind_val',metric('Sensor.Wind.Speed','km/h'));
      display('pressure_val',metric('Sensor.Pressure','hPa'));
      display('rain_val',metric('Sensor.Precipitation.Rain','mm'));
    };
    paint();if(module)this.subscription=this.subscribe(module,paint);
  }
  onDispose(){this.subscription?.unsubscribe();}
}
