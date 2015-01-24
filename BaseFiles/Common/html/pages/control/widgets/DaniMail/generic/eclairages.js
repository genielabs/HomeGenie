[{
  Name: "Light Virtual Widget",
  Author: "DaniMail",
  Version: "2015-01-15",
  GroupName : '',
  IconImage : '',
  StatusText : '',
  Description : '',
  
  RenderView: function (cuid, module) {
    var moduleCalendrier='510';
    var moduleTabHOR='511';
    var nameLight1;
    var nameLight2;
	var typeDay=' ';
	var hourC,minC,index=0;

    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    var controlpopup = widget.data('ControlPopUp');
    var _this = this;
    
    if (!controlpopup) {
      container.find('[data-ui-field=controlpopup]').trigger('create');
      controlpopup = container.find('[data-ui-field=controlpopup]').popup();
      widget.data('ControlPopUp', controlpopup);

	  widget.find('[data-ui-field=options]').on('click', function() {
	        if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp'))
	        {
			  var dateTime=new Date();
			  var numMonth=dateTime.getMonth();
			  var fullYear=dateTime.getFullYear();
	          var numDay=dateTime.getDate();
			  var offYear;
		
			  if( fullYear%2 == 0 )
				offYear=0 ;
			  else
				offYear=1;
	          hourC=dateTime.getHours();
	          minC=dateTime.getMinutes();
	          var moduleCAL = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleCalendrier);
	          if((moduleCAL != null) && (moduleCAL.Name == 'Calendrier'))
	          {
		     	  var tabMois=HG.WebApp.Utility.GetModulePropertyByName(moduleCAL, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
		     	  typeDay = tabMois.Value.charAt(numDay-1);
		          var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
	          	  if((moduleTH != null) && (moduleTH.Name == 'Tables horaires'))
		          {
					  var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.W');
					  var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+tabHOR.Value);
				      drawTable(controlpopup.find('[id=tableW]')[0].getContext('2d'),tabletherm.Value,'Week-End - Table horaire '+tabHOR.Value,(typeDay=='W'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.O');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+tabHOR.Value);
				      drawTable(controlpopup.find('[id=tableO]')[0].getContext('2d'),tabletherm.Value,'Ouvré - Table horaire '+tabHOR.Value,(typeDay=='O'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.F');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+tabHOR.Value);
				      drawTable(controlpopup.find('[id=tableF]')[0].getContext('2d'),tabletherm.Value,'Férié - Table horaire '+tabHOR.Value,(typeDay=='F'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.S');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+tabHOR.Value);
				      drawTable(controlpopup.find('[id=tableS]')[0].getContext('2d'),tabletherm.Value,'Spécial - Table horaire '+tabHOR.Value,(typeDay=='S'),hourC,minC);
			      }
		      }
	          $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	        }
	  // light mode buttons events
	  controlpopup.find('[data-ui-field=mode_off]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_on]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[id=tableW]').hide();
	        controlpopup.find('[id=tableO]').hide();
	        controlpopup.find('[id=tableF]').hide();
	        controlpopup.find('[id=tableS]').hide();
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'Off', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_on]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_on]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[id=tableW]').hide();
	        controlpopup.find('[id=tableO]').hide();
	        controlpopup.find('[id=tableF]').hide();
	        controlpopup.find('[id=tableS]').hide();
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'On', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_prog]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_on]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').addClass('ui-btn-active');
	        controlpopup.find('[id=tableW]').show();
	        controlpopup.find('[id=tableO]').show();
	        controlpopup.find('[id=tableF]').show();
	        controlpopup.find('[id=tableS]').show();
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'Program', function (data) { });
	  });
	    
	  controlpopup.find('[id=tableW]').on("click",function(event){
			_this.analyseClick($(this).offset(),event);
			if( _this.index != 0 )
			{
	    	    var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module,'Table.W');
	    	    var newtabHOR = tabHOR.Value;
	    	    if(_this.index == 1 && newtabHOR < 9)
	    	    	newtabHOR++ ;
	    	    if(_this.index == 2 && newtabHOR > 0)
	    	    	newtabHOR-- ;
			    var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+newtabHOR);
				drawTable(controlpopup.find('[id=tableW]')[0].getContext('2d'),tabletherm.Value,'Week-End - Table horaire '+newtabHOR,(typeDay=='W'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Light.SetTable/W',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[id=tableO]').on("click",function(event){
			_this.analyseClick($(this).offset(),event);
			if( _this.index != 0 )
			{
	    	    var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module,'Table.O');
	    	    var newtabHOR = tabHOR.Value;
	    	    if(_this.index == 1 && newtabHOR < 5)
	    	    	newtabHOR++ ;
	    	    if(_this.index == 2 && newtabHOR > 0)
	    	    	newtabHOR-- ;
			    var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+newtabHOR);
				drawTable(controlpopup.find('[id=tableO]')[0].getContext('2d'),tabletherm.Value,'Ouvré - Table horaire '+newtabHOR,(typeDay=='O'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Light.SetTable/O',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[id=tableF]').on("click",function(event){
			_this.analyseClick($(this).offset(),event);
			if( _this.index != 0 )
			{
	    	    var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module,'Table.F');
	    	    var newtabHOR = tabHOR.Value;
	    	    if(_this.index == 1 && newtabHOR < 9)
	    	    	newtabHOR++ ;
	    	    if(_this.index == 2 && newtabHOR > 0)
	    	    	newtabHOR-- ;
			    var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+newtabHOR);
				drawTable(controlpopup.find('[id=tableF]')[0].getContext('2d'),tabletherm.Value,'Férié - Table horaire '+newtabHOR,(typeDay=='F'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Light.SetTable/F',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[id=tableS]').on("click",function(event){
			_this.analyseClick($(this).offset(),event);
			if( _this.index != 0 )
			{
	    	    var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module,'Table.S');
	    	    var newtabHOR = tabHOR.Value;
	    	    if(_this.index == 1 && newtabHOR < 9)
	    	    	newtabHOR++ ;
	    	    if(_this.index == 2 && newtabHOR > 0)
	    	    	newtabHOR-- ;
			    var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Light.'+newtabHOR);
				drawTable(controlpopup.find('[id=tableS]')[0].getContext('2d'),tabletherm.Value,'Special - Table horaire '+newtabHOR,(typeDay=='S'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Light.SetTable/S',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
	  });
	  // settings button
	  widget.find('[data-ui-field=settings]').on('click', function(){
	        if (module.Domain == 'HomeAutomation.HomeGenie.Automation')
	        {
	          HG.WebApp.ProgramEdit._CurrentProgram.Domain = module.Domain;
	          HG.WebApp.ProgramEdit._CurrentProgram.Address = module.Address;
	          HG.WebApp.ProgramsList.UpdateOptionsPopup();
	        }
	        else
	        {
	          HG.WebApp.Control.EditModule(module);
	        }
	  });
	  	
	  // light mode buttons events
	  widget.find('[data-ui-field=off]').on('click', function(){
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'Off', function (data) { });
	  });
	  widget.find('[data-ui-field=on]').on('click', function(){
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'On', function (data) { });
	  });
	  widget.find('[data-ui-field=prog]').on('click', function(){
	        HG.Control.Modules.ServiceCall('Light.ModeSet', module.Domain, module.Address, 'Program', function (data) { });
	  });

	  controlpopup.on('popupbeforeposition', function(evt, ui){
	        // reset buttons' state
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_on]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        // set current buttons' state from module properties 
	        var thermostatMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Light.Mode');
	        if (thermostatMode != null)
	        {
	          controlpopup.find('[id=tableW]').hide();
	          controlpopup.find('[id=tableO]').hide();
	          controlpopup.find('[id=tableF]').hide();
	          controlpopup.find('[id=tableS]').hide();
	          controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_on]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	          if (thermostatMode.Value == 'Off') 
	            controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	          else if (thermostatMode.Value == 'On') 
	            controlpopup.find('[data-ui-field=mode_on]').addClass('ui-btn-active');
	          else if (thermostatMode.Value == 'Program') 
	          {
	            controlpopup.find('[data-ui-field=mode_prog]').addClass('ui-btn-active');
	            controlpopup.find('[id=tableW]').show();
	            controlpopup.find('[id=tableO]').show();
	            controlpopup.find('[id=tableF]').show();
	            controlpopup.find('[id=tableS]').show();
	          }
	        }
	        else
	          controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	  });
    }
	this.GroupName = container.attr('data-context-group');
    this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1))+' '+module.Address;
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);
    controlpopup.find('[data-ui-field=group]').html(this.GroupName);
    controlpopup.find('[data-ui-field=name]').html(module.Name);
    controlpopup.find('[data-ui-field=light1]').html("Zone(s) : "+this.getNameModule(module,"LightModule1"));
    controlpopup.find('[data-ui-field=light2]').html(this.getNameModule(module,"LightModule2"));

    // get module watts prop
    var watts = HG.WebApp.Utility.GetModulePropertyByName(module, "Meter.Watts");
    if (watts != null) {
        var w = Math.round(watts.Value.replace(',', '.'));
        if (w > 0) {
            watts = w + 'W';
        } else watts = '';
    } else watts = '';

    // get module operatingstate prop for status text
    var operatingMode = HG.WebApp.Utility.GetModulePropertyByName(module, "Light.OperatingState");
    if(operatingMode != null) {
        var updatetime = operatingMode.UpdateTime;
        if(typeof updatetime != 'undefined') {
            updatetime = updatetime.replace(' ','T'); // fix for IE and FF
            var d = new Date(updatetime);
            this.UpdateTime = HG.WebApp.Utility.FormatDate(d) + ' ' + HG.WebApp.Utility.FormatDateTime(d);
//          this.UpdateTime = $.datepicker.formatDate('D, mm/dd/yy',d,{dayNamesShort:$.datepicker.regional["fr"].dayNamesShort})+' '+HG.WebApp.Utility.FormatDateTime(d);
        }
        if(operatingMode.Value == 'On')
            this.StatusText = '<img width="15" height="15" src="images/common/led_green.png" style="vertical-align:middle" />';
        else if(operatingMode.Value == 'Off')
            this.StatusText = '<img width="15" height="15" src="images/common/led_black.png" style="vertical-align:middle" />';
        else if(operatingMode.Value == 'Sun')
            this.StatusText = '<img width="15" height="15" src="images/common/led_yellow.png" style="vertical-align:middle" />';
        else if( operatingMode.Value.charAt(0) == '!' )
            this.StatusText = operatingMode.Value+'&nbsp;&nbsp;<img width="15" height="15" src="images/common/led_red.png" style="vertical-align:middle" />';
        else
            this.StatusText = operatingMode.Value+'&nbsp;&nbsp;<img width="15" height="15" src="images/common/led_brown.png" style="vertical-align:middle" />';
    } else 	this.StatusText = 'Erreur Status';
    // test light operatingstate for icon image
    var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
    if((widgeticon != null) && (widgeticon.Value != ''))
       this.IconImage = widgeticon.Value;
    else
       this.IconImage = 'pages/control/widgets/homegenie/generic/images/light_off.png';
    if (operatingMode.Value == 'On')
       this.IconImage = this.IconImage.replace('_off', '_on');
    var displayMode = '---';
    var lightMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Light.Mode');
    if (lightMode != null) displayMode = lightMode.Value;
    if( displayMode == 'Program' )
    	displayMode = 'Programmé';
    widget.find('[id=img_icon]').attr('src',this.IconImage);
    widget.find('[data-ui-field=updatetime]').html(this.UpdateTime);
    widget.find('[data-ui-field=mode_value]').html(displayMode+'&nbsp;&nbsp;&nbsp;');  
    widget.find('[data-ui-field=status]').html(this.StatusText);  
  },

  getNameModule: function(module,property) {
     try {
	   var nameModule=HG.WebApp.Utility.GetModulePropertyByName(module,property).Value;
	   if((nameModule != null) && (nameModule != ""))
	   { 
	     var lIndex= nameModule.lastIndexOf(':');
	     var domain=nameModule.substring(0,lIndex);
	     var address=nameModule.substring(lIndex+1);
	     var moduleT=HG.WebApp.Utility.GetModuleByDomainAddress(domain,address);
	     return moduleT.Name;
	   }
       return "";
	 }
	 catch(e) {
	 	return e.Message;
	 }
  },
  
  analyseClick: function(rect,evt) {
	var posX = evt.pageX-rect.left;
	var posY = evt.pageY-rect.top;
    this.index=0;
    if((posX > 319) && (posX < 333))
    {
    	if( (posY > 5) && (posY < 30) )
    		this.index=1;
     	if( (posY > 30) && (posY < 45) )
     		this.index=2;
     }
  }

}]
