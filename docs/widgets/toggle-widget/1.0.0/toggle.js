class ToggleWidget extends ControllerInstance {
  settings={defaultSize:'',sizeOptions:[],boundModules:{switch:{label:'Light or switch',typeFilter:'Light,Dimmer,Color,Switch'}}};
  onCreate(){
    const module=this.getBoundModule?.('switch') || this.boundModule;
    const button=this.field('toggle_btn').get(),label=this.field('status_text').get();
    let local=false;
    const state=()=>module?Number(module.field('Status.Level')?.value)>0:local;
    const paint=()=>{
      button.classList.toggle('active',state());button.setAttribute('aria-checked',String(state()));
      label.textContent=(module?.name || 'Local switch')+' · '+(state()?'ON':'OFF');
    };
    paint();if(module)this.subscription=this.subscribe(module,paint);
    button.onclick=()=>{
      if(!module){local=!local;paint();return;}
      button.disabled=true;
      const finish=()=>{button.disabled=false;paint();};
      const fail=()=>{finish();label.textContent='Command failed';};
      try{this.request=module.control(state()?'Control.Off':'Control.On').subscribe({
        next:r=>{if(r?.code>=400){fail();}else finish();},error:fail,complete:()=>{button.disabled=false;}
      });}catch{fail();}
    };
  }
  onDispose(){this.subscription?.unsubscribe();this.request?.unsubscribe();}
}
