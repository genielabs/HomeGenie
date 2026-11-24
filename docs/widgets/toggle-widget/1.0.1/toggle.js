class ToggleWidget extends ControllerInstance {
  settings={defaultSize:'',sizeOptions:[],boundModules:{switch:{label:'Light or switch',typeFilter:'Light,Dimmer,Color,Switch'}}};
  onCreate(){
    const module=this.getBoundModule?.('switch') || this.boundModule;
    const button=this.field('toggle_btn').get(),label=this.field('status_text').get();
    let local=false;
    const feedback=this.field('feedback').get();
    const it=(document.documentElement.lang || navigator.language).startsWith('it');
    const state=()=>{
      if(!module)return local;
      if(module.isOnline===false)return null;
      const field=module.field('Status.Level');
      const raw=field?.value;
      if(raw==null || String(raw).trim()==='')return null;
      const level=Number(String(raw).replace(',','.'));
      return Number.isFinite(level) ? level>0 : null;
    };
    let pending=null;
    let unconfirmed=null;
    const paint=()=>{
      const value=state();
      button.classList.toggle('active',value===true);
      button.dataset.unknown=String(value===null);
      button.setAttribute('aria-checked',String(value===true));
      button.disabled=value===null || pending!==null;
      label.textContent=(module?.name || (it?'Interruttore locale':'Local switch'))+' · '+
        (value===null?(it?'Stato sconosciuto':'Unknown state'):value?'ON':'OFF');
      button.setAttribute('aria-label',label.textContent);
    };
    const finish=(operation,message)=>{
      if(this.closed || pending!==operation)return;
      clearTimeout(this.timeout);pending=null;feedback.textContent=message;paint();
    };
    const refresh=()=>{
      if(this.closed)return;
      if(unconfirmed && state()===unconfirmed.target){
        unconfirmed=null;feedback.textContent='';
      }
      if(pending && state()===pending.target)finish(pending,'');
      else paint();
    };
    paint();if(module)this.subscription=this.subscribe(module,refresh);
    button.onclick=()=>{
      if(!module){local=!local;paint();return;}
      if(pending || state()===null)return;
      const operation={target:!state()};
      unconfirmed=null;
      pending=operation;feedback.textContent=it?'In attesa di conferma…':'Awaiting confirmation…';paint();
      this.timeout=setTimeout(()=>{
        unconfirmed=operation;
        finish(operation,it?'Stato non confermato':'State not confirmed');
      },8000);
      const failed=()=>finish(operation,it?'Comando non riuscito':'Command failed');
      try{this.request=module.control(operation.target?'Control.On':'Control.Off').subscribe({
        next:r=>{if(r?.code>=400)failed();else refresh();},error:failed
      });}catch{failed();}
    };
  }
  onDispose(){this.closed=true;clearTimeout(this.timeout);this.subscription?.unsubscribe();this.request?.unsubscribe();}
}
