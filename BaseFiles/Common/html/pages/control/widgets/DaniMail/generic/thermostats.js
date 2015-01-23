[{
  Name: "Thermostat Virtual Widget",
  Author: "DaniMail",
  Version: "2015-01-02",

  GroupName : '',
  IconImage : 'pages/control/widgets/homegenie/generic/images/temperature.png',
  StatusText : '',
  Description : '',
  
  RenderView: function (cuid, module) {
    var imgTherm='pages/control/widgets/DaniMail/generic/images/therm_';
    var moduleCalendrier='510';
    var moduleTabHOR='511';
    var nameSensor;
    var nameSwitch;
    var displayUnit='Celsuis';
	var levelKnobBindValue='';
	var typeDay=' ';
	var hourC,minC,index=0;

    var container = $(cuid);
    var widget = container.find('[data-ui-field=widget]');
    var controlpopup = widget.data('ControlPopUp');
    var _this = this;
    
    if (HG.WebApp.Locales.GetDateEndianType() == 'M') 
    	displayUnit = 'Fahrenheit';

    if (!controlpopup) {
      container.find('[data-ui-field=controlpopup]').trigger('create');
      controlpopup = container.find('[data-ui-field=controlpopup]').popup();
      widget.data('ControlPopUp', controlpopup);

	  if (displayUnit == 'Celsius')
	  {
	    controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
	      min: 5,
	      max: 35
	    });
	  }
	  else
	  {
	    controlpopup.find('[data-ui-field=level_knob]').trigger('configure', {
	      min: 40,
	      max: 100
	    });
	  }
	
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
	          if(moduleCAL != null)
	          {
		     	  var tabMois=HG.WebApp.Utility.GetModulePropertyByName(moduleCAL, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
		     	  typeDay = tabMois.Value.charAt(numDay-1);
		          var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
	          	  if(moduleTH != null)
		          {
					  var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.W');
					  var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+tabHOR.Value);
				      _this.drawTable(controlpopup.find('[data-ui-field=tableW]')[0].getContext('2d'),tabletherm.Value,'Week-End - Table horaire '+tabHOR.Value,(typeDay=='W'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.O');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+tabHOR.Value);
				      _this.drawTable(controlpopup.find('[data-ui-field=tableO]')[0].getContext('2d'),tabletherm.Value,'Ouvré - Table horaire '+tabHOR.Value,(typeDay=='O'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.F');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+tabHOR.Value);
				      _this.drawTable(controlpopup.find('[data-ui-field=tableF]')[0].getContext('2d'),tabletherm.Value,'Férié - Table horaire '+tabHOR.Value,(typeDay=='F'),hourC,minC);
					  tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module, 'Table.S');
					  tabletherm = HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+tabHOR.Value);
				      _this.drawTable(controlpopup.find('[data-ui-field=tableS]')[0].getContext('2d'),tabletherm.Value,'Spécial - Table horaire '+tabHOR.Value,(typeDay=='S'),hourC,minC);
			      }
		      }
	          $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	        }
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
	  	
	  controlpopup.on('popupbeforeposition', function(evt, ui){
	        // reset buttons' state
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	        // set current buttons' state from module properties 
	        var thermostatMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.Mode');
	        if (thermostatMode != null)
	        {
	          controlpopup.find('[data-ui-field=tableW]').hide();
	          controlpopup.find('[data-ui-field=tableO]').hide();
	          controlpopup.find('[data-ui-field=tableF]').hide();
	          controlpopup.find('[data-ui-field=tableS]').hide();
	          controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-btn-active');
	          controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-btn-active');
	          if (thermostatMode.Value == 'NoFrost') 
	          {
	            controlpopup.find('[data-ui-field=mode_nofrost]').addClass('ui-btn-active');
	            _this.EditNoFrostSetPoint(controlpopup, module);
	          }
	          else if (thermostatMode.Value == 'Eco') 
	          {
	            controlpopup.find('[data-ui-field=mode_eco]').addClass('ui-btn-active');
	            _this.EditEcoSetPoint(controlpopup, module);
	          }
	          else if (thermostatMode.Value == 'Comfort') 
	          {
	            controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
	            _this.EditHeatSetPoint(controlpopup, module);
	          }
	          else if (thermostatMode.Value == 'Program') 
	          {
	            controlpopup.find('[data-ui-field=mode_prog]').addClass('ui-btn-active');
	            controlpopup.find('[data-ui-field=tableW]').show();
	            controlpopup.find('[data-ui-field=tableO]').show();
	            controlpopup.find('[data-ui-field=tableF]').show();
	            controlpopup.find('[data-ui-field=tableS]').show();
	          }
	          else if (thermostatMode.Value == 'Off')
	          {
	            controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	          }
	        }
	        else
	          controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	  });
	  
	  controlpopup.find('[data-ui-field=level_knob]').knob({
	        'release' : function (v) 
	        {
	          v = Math.round(v);
	          var setPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'SetPoint.' + _this.levelKnobBindValue);
	          if (setPoint != null) setPoint.Value = v;
	          HG.Control.Modules.ServiceCall('Thermostat.SetPointSet/' + _this.levelKnobBindValue, module.Domain, module.Address, v, function (data) { });
	        },
	        'change' : function (v)
	        {
	          v = Math.round(v);
	          controlpopup.find('[data-ui-field=status]').html(v + '&deg;');
	        }
	            
	  });
	  // set point buttons events
	  controlpopup.find('[data-ui-field=nofrost_setpoint]').on('click', function(){
	    _this.EditNoFrostSetPoint(controlpopup, module);
	  });
	  controlpopup.find('[data-ui-field=eco_setpoint]').on('click', function(){
	    _this.EditEcoSetPoint(controlpopup, module);
	  });
	  controlpopup.find('[data-ui-field=heat_setpoint]').on('click', function(){
	    _this.EditHeatSetPoint(controlpopup, module);
	  });
	      
	  // thermostat mode buttons events
	  controlpopup.find('[data-ui-field=mode_off]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=tableW]').hide();
	        controlpopup.find('[data-ui-field=tableO]').hide();
	        controlpopup.find('[data-ui-field=tableF]').hide();
	        controlpopup.find('[data-ui-field=tableS]').hide();
	        HG.Control.Modules.ServiceCall('Thermostat.ModeSet', module.Domain, module.Address, 'Off', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_nofrost]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=tableW]').hide();
	        controlpopup.find('[data-ui-field=tableO]').hide();
	        controlpopup.find('[data-ui-field=tableF]').hide();
	        controlpopup.find('[data-ui-field=tableS]').hide();
	        _this.EditNoFrostSetPoint(controlpopup, module);
	        HG.Control.Modules.ServiceCall('Thermostat.ModeSet', module.Domain, module.Address, 'NoFrost', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_eco]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=tableW]').hide();
	        controlpopup.find('[data-ui-field=tableO]').hide();
	        controlpopup.find('[data-ui-field=tableF]').hide();
	        controlpopup.find('[data-ui-field=tableS]').hide();
	        _this.EditEcoSetPoint(controlpopup, module);
	        HG.Control.Modules.ServiceCall('Thermostat.ModeSet', module.Domain, module.Address, 'Eco', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_heat]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_prog]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=tableW]').hide();
	        controlpopup.find('[data-ui-field=tableO]').hide();
	        controlpopup.find('[data-ui-field=tableF]').hide();
	        controlpopup.find('[data-ui-field=tableS]').hide();
	        _this.EditHeatSetPoint(controlpopup, module);
	        HG.Control.Modules.ServiceCall('Thermostat.ModeSet', module.Domain, module.Address, 'Comfort', function (data) { });
	  });
	  controlpopup.find('[data-ui-field=mode_prog]').on('click', function(){
	        controlpopup.find('[data-ui-field=mode_prog]').addClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_heat]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=mode_off]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-btn-active');
	        controlpopup.find('[data-ui-field=tableW]').show();
	        controlpopup.find('[data-ui-field=tableO]').show();
	        controlpopup.find('[data-ui-field=tableF]').show();
	        controlpopup.find('[data-ui-field=tableS]').show();
	        HG.Control.Modules.ServiceCall('Thermostat.ModeSet', module.Domain, module.Address, 'Program', function (data) { });
	  });
	    
	  controlpopup.find('[data-ui-field=tableW]').on("click",function(event){
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
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+newtabHOR);
				_this.drawTable(controlpopup.find('[data-ui-field=tableW]')[0].getContext('2d'),tabletherm.Value,'Week-End - Table horaire '+newtabHOR,(typeDay=='W'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Thermostat.SetTable/W',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[data-ui-field=tableO]').on("click",function(event){
			_this.analyseClick($(this).offset(),event);
			if( _this.index != 0 )
			{
	    	    var tabHOR = HG.WebApp.Utility.GetModulePropertyByName(module,'Table.O');
	    	    var newtabHOR = tabHOR.Value;
	    	    if(_this.index == 1 && newtabHOR < 9)
	    	    	newtabHOR++ ;
	    	    if(_this.index == 2 && newtabHOR > 0)
	    	    	newtabHOR-- ;
			    var moduleTH = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation',moduleTabHOR);
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+newtabHOR);
				_this.drawTable(controlpopup.find('[data-ui-field=tableO]')[0].getContext('2d'),tabletherm.Value,'Ouvré - Table horaire '+newtabHOR,(typeDay=='O'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Thermostat.SetTable/O',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[data-ui-field=tableF]').on("click",function(event){
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
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+newtabHOR);
				_this.drawTable(controlpopup.find('[data-ui-field=tableF]')[0].getContext('2d'),tabletherm.Value,'Férié - Table horaire '+newtabHOR,(typeDay=='F'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Thermostat.SetTable/F',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
		controlpopup.find('[data-ui-field=tableS]').on("click",function(event){
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
				var tabletherm=  HG.WebApp.Utility.GetModulePropertyByName(moduleTH, 'ConfigureOptions.Table.Therm.'+newtabHOR);
				_this.drawTable(controlpopup.find('[data-ui-field=tableS]')[0].getContext('2d'),tabletherm.Value,'Special - Table horaire '+newtabHOR,(typeDay=='S'),hourC,minC);
	            HG.Control.Modules.ServiceCall('Thermostat.SetTable/S',module.Domain,module.Address,newtabHOR,function(data){ });
			}
	    });
    }
	this.GroupName = container.attr('data-context-group');
    this.Description = (module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);
    controlpopup.find('[data-ui-field=group]').html(this.GroupName);
    controlpopup.find('[data-ui-field=name]').html(module.Name);
    controlpopup.find('[data-ui-field=sensor]').html("Sonde : "+this.getNameModule(module,"ModuleTemperature"));
    controlpopup.find('[data-ui-field=switch]').html("Zone : "+this.getNameModule(module,"SwitchModule1"));

    var imagesrc = 'pages/control/widgets/homegenie/generic/images/temperature.png';

    // display Temperature
    var temperatureField = HG.WebApp.Utility.GetModulePropertyByName(module, 'Sensor.Temperature');
    var temperature = 0;
    if (temperatureField != null)
    {
      temperature = Math.round(temperatureField.Value.replace(',', '.') * 10) / 10;
      if (displayUnit == 'Fahrenheit') temperature = (temperature * 1.8) + 32;
    }
    widget.find('[data-ui-field=temperature_value]').html(temperature+'&deg;');
    // display humidity
    var humidityField = HG.WebApp.Utility.GetModulePropertyByName(module, 'Sensor.Humidity');
    if (humidityField != null)
    {
        var humidity = '&nbsp;&nbsp;&nbsp;'+Math.round(humidityField.Value.replace(',', '.')*10)/10+'%';
    	widget.find('[data-ui-field=humidity_value]').html(humidity);
    }
    // display current Heating SetPoint
    var heatTo = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Heating');
    if (heatTo == null || heatTo.Value == '')
    {
      widget.find('[data-ui-field=heat_field]').hide();
    }
    else
    {
      widget.find('[data-ui-field=heat_field]').show();
      var temperature = Math.round(heatTo.Value.replace(',', '.') * 100) / 100;
      if (displayUnit == 'Fahrenheit') temperature = (temperature * 1.8) + 32;
      widget.find('[data-ui-field=set_value]').html('&nbsp;'+temperature+'&deg;');
    }

    // display current Eco SetPoint
    widget.find('[data-ui-field=eco_field]').hide();
    var ecoTo = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Eco');
    if (ecoTo == null || ecoTo.Value == '')
    {
      widget.find('[data-ui-field=eco_field]').hide();
    }
    else
    {
      widget.find('[data-ui-field=eco_field]').show();
      var temperature = Math.round(ecoTo.Value.replace(',', '.') * 100) / 100;
      if (displayUnit == 'Fahrenheit') temperature = (temperature * 1.8) + 32;
      widget.find('[data-ui-field=ecoset_value]').html('&nbsp;'+temperature+'&deg;');
    }
    // enable/disable Eco Set Point feature (not every thermostat support it)
    if (ecoTo == null)
    {
      controlpopup.find('[data-ui-field=mode_eco]').addClass('ui-disabled');
      controlpopup.find('[data-ui-field=eco_setpoint]').addClass('ui-disabled');
    }
    else
    {
      controlpopup.find('[data-ui-field=mode_eco]').removeClass('ui-disabled');
      controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-disabled');
    }
    
    // display current NoFrost SetPoint
    widget.find('[data-ui-field=nofrost_field]').hide();
    var nofrostTo = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.NoFrost');
    if (nofrostTo == null || nofrostTo.Value == '')
    {
      widget.find('[data-ui-field=nofrost_field]').hide();
    }
    else
    {
      widget.find('[data-ui-field=nofrost_field]').show();
      var temperature = Math.round(nofrostTo.Value.replace(',', '.') * 100) / 100;
      if (displayUnit == 'Fahrenheit') temperature = (temperature * 1.8) + 32;
      widget.find('[data-ui-field=nofrostset_value]').html('&nbsp;'+temperature+'&deg;');
    }
    // enable/disable NoFrost Set Point feature (not every thermostat support it)
    if (nofrostTo == null)
    {
      controlpopup.find('[data-ui-field=mode_nofrost]').addClass('ui-disabled');
      controlpopup.find('[data-ui-field=nofrost_setpoint]').addClass('ui-disabled');
    }
    else
    {
      controlpopup.find('[data-ui-field=mode_nofrost]').removeClass('ui-disabled');
      controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-disabled');
    }

    // display status line (operating state + mode)
    var displayState='---';
    var operatingState = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.OperatingState');
    if (operatingState != null) displayState = operatingState.Value;
    widget.find('[data-ui-field=img_heat]').hide();
    widget.find('[data-ui-field=img_eco]').hide();
    widget.find('[data-ui-field=img_nofrost]').hide();
    if( displayState.charAt(0) == '(' )
    {
        var displayIcon='';
    	if(displayState.indexOf('On') != -1)
    	{
           this.StatusText = '<img width="15" height="15" src="images/common/led_green.png" style="vertical-align:middle" />';
    	   displayIcon = imgTherm+'on.gif';
    	}
    	else
    	{
           this.StatusText = '<img width="15" height="15" src="images/common/led_black.png" style="vertical-align:middle" />';
           displayIcon = imgTherm+'off.png';
        }
    	if( displayState.charAt(1) == 'C' )
    	{
 		   widget.find('[data-ui-field=img_heat]').attr('src',displayIcon);
     	   widget.find('[data-ui-field=img_heat]').show();
		}
    	if( displayState.charAt(1) == 'E' )
    	{
		   widget.find('[data-ui-field=img_eco]').attr('src',displayIcon);
		   widget.find('[data-ui-field=img_eco]').show();
		}
    	if( displayState.charAt(1) == 'H' )
    	{
		   widget.find('[data-ui-field=img_nofrost]').attr('src',displayIcon);
    	   widget.find('[data-ui-field=img_nofrost]').show();
		}
   	}
   	else
   	{
   		if( displayState.charAt(0) == '!' )
        	this.StatusText = displayState+'&nbsp;&nbsp;<img width="15" height="15" src="images/common/led_red.png" style="vertical-align:middle" />';
        else
        	this.StatusText = displayState+'&nbsp;&nbsp;<img width="15" height="15" src="images/common/led_yellow.png" style="vertical-align:middle" />';
    }
   	widget.find('[data-ui-field=operating_value]').html(this.StatusText);
    //
    var displayMode = '---';
    var operatingMode = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.Mode');
    if (operatingMode != null) displayMode = operatingMode.Value;
    if( displayMode == 'Off' )
    	displayMode = 'Arrêt';
    else
    if( displayMode == 'NoFrost' )
    	displayMode = 'Hors Gel';
    else
    if( displayMode == 'Eco' )
    	displayMode = 'Eco';
    else
    if( displayMode == 'Comfort' )
    	displayMode = 'Confort';
    else
    if( displayMode == 'Program' )
    	displayMode = 'Programmé';
    widget.find('[data-ui-field=mode_value]').html(displayMode+'&nbsp;&nbsp;&nbsp;');  
	// Display battery level of Sansor
    var batteryLevel =HG.WebApp.Utility.GetModulePropertyByName(module, 'Status.Battery');
    if (batteryLevel != null) {
        var value = Math.round(batteryLevel.Value.replace(',', '.') * 10) / 10;
        var blevel = 0;
        blevel = parseFloat(value);
        if (blevel == 255) blevel = 0;
        else if (blevel > 80 && blevel <= 100) blevel = 100;
        else if (blevel > 60) blevel = 80;
        else if (blevel > 40) blevel = 60;
        else if (blevel > 20) blevel = 40;
        else if (blevel > 10) blevel = 20;
        else if (blevel > 0) blevel = 10;
        //
        var infotext = blevel+'%  <img style="vertical-align:middle" src="pages/control/widgets/homegenie/generic/images/battery_level_'+blevel+'.png"/>';
	    widget.find('[data-ui-field=status]').html('&nbsp;'+infotext);  
    }
 },

  EditNoFrostSetPoint: function(controlpopup, module) {
    var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
    controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
    controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-btn-active');
    controlpopup.find('[data-ui-field=nofrost_setpoint]').addClass('ui-btn-active');
    // show current nofrost setpoint
    var nofrostSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.NoFrost');
    if (nofrostSetPoint != null) 
    {
      levelKnob.val(nofrostSetPoint.Value).trigger('change');
      controlpopup.find('[data-ui-field=status]').html(nofrostSetPoint.Value + '&deg;');
    }
    this.levelKnobBindValue = 'NoFrost';
  },

  EditEcoSetPoint: function(controlpopup, module) {
    var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
    controlpopup.find('[data-ui-field=heat_setpoint]').removeClass('ui-btn-active');
    controlpopup.find('[data-ui-field=eco_setpoint]').addClass('ui-btn-active');
    controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-btn-active');
    // show current eco setpoint
    var ecoSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Eco');
    if (ecoSetPoint != null) 
    {
      levelKnob.val(ecoSetPoint.Value).trigger('change');
      controlpopup.find('[data-ui-field=status]').html(ecoSetPoint.Value + '&deg;');
    }
    this.levelKnobBindValue = 'Eco';
  },

  EditHeatSetPoint: function(controlpopup, module) {
    var levelKnob = controlpopup.find('[data-ui-field=level_knob]');
    controlpopup.find('[data-ui-field=heat_setpoint]').addClass('ui-btn-active');
    controlpopup.find('[data-ui-field=eco_setpoint]').removeClass('ui-btn-active');
    controlpopup.find('[data-ui-field=nofrost_setpoint]').removeClass('ui-btn-active');
    // show current heat setpoint
    var heatSetPoint = HG.WebApp.Utility.GetModulePropertyByName(module, 'Thermostat.SetPoint.Heating');
    if (heatSetPoint != null) 
    {
      levelKnob.val(heatSetPoint.Value).trigger('change');
      controlpopup.find('[data-ui-field=status]').html(heatSetPoint.Value + '&deg;');
    }
    this.levelKnobBindValue = 'Heating';    
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
  },
  
  drawTable: function (context,etat,name,select,heure,minute) {
    var x=10;
    var y=28;
    var nb=0;
    var fgcolor ;

    context.clearRect(0,0,310,50) ;
    if( select == true )
    {
        context.beginPath();
        context.lineWidth="2";   
        context.strokeStyle="white";   
        context.rect(0,0,309,49);
        context.stroke();               
    }
    context.font = "8pt Arial";
    context.fillStyle = "black";
    context.fillText(name,8,10);
    context.font = "6pt Arial";
    for(var i=0 , c = etat.length ; i < 96 ; i++ )
    {
        if( i < c ) {
            switch( etat[i] )
            {
               case 'A' :
                fgcolor = "#bcbcbc" ;
                break ;
               case 'H' :
                fgcolor = "#2387dc" ;
                break ;
               case 'E' :
                fgcolor = "#53a840" ;
                break ;
               case 'C' :
                fgcolor = "#da4336" ;
                break ;
               default :
                fgcolor = "black" ;
                break ;
            }
        }
        else
            fgcolor = "black" ;
        nb++ ;
        if( (i == 95) || (etat[i+1] != etat[i])) 
        {
            context.fillStyle=fgcolor;
            context.fillRect(x,y,nb*3,16);
            x += nb*3 ;
            nb = 0 ;
        }
        if( i%4 == 0 )
        {
            context.fillStyle = "black";
            context.fillText(i/4,((i/4)*12)+7,24);
        }
    }
    context.fillStyle = "black";
    context.fillText(i/4,((i/4)*12)+7,24);
    if( select == true )
    {
        var posq=(heure*4)+Math.floor(minute/15);
        context.beginPath();
        context.lineWidth="1";
        context.strokeStyle="white";   
        context.rect(10+((posq-1)*3),y-2,9,20);
        context.stroke();               
    }
    context.beginPath();
    context.strokeStyle="#343434";   
    context.lineWidth="2";
    
    context.moveTo(326,11);
    context.lineTo(326,19);
    context.moveTo(322,15);
    context.lineTo(330,15);
    
    context.moveTo(322,35);
    context.lineTo(330,35);
    context.stroke();               
    context.strokeStyle="#343434";   
    context.lineWidth="1";
    context.rect(318,5,16,40);
    context.moveTo(318,25);
    context.lineTo(334,25);
    context.stroke();               
  }   

}]
