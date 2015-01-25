[{
  Name: 'Table horaire Widget',
  Author: 'DaniMail',
  Version: '2014-12-15',

  GroupName : '',
  IconImage : 'images/scheduler.png',
  StatusText : '',
  Description : '',
  Initialized: false,
  TimeTableUI: '',
  ThermLocalUI : ["off","nofrost","eco","heat"],
	
  InitView: function (container) {
    var _this = this;
   	var widget=container.find('[data-ui-field=widget]');
    setTimeout(function () {
      _this.TimeTableUI = widget.find('[data-ui-label=id_timetable]').html();
      for(var i = 0; i < _this.ThermLocalUI.length; i++)
        _this.ThermLocalUI[i] = widget.find('[data-ui-label=id_'+_this.ThermLocalUI[i]+']').html();
      _this.drawChoice(widget.find('[data-ui-field=legend0]')[0].getContext('2d'),"AHEC",' ',18);
	  _this.drawChoice(widget.find('[data-ui-field=legend1]')[0].getContext('2d'),"FRSO",' ',18);
	  _this.drawChoice(widget.find('[data-ui-field=legend2]')[0].getContext('2d'),"012345",' ',18);
    },1000);
   this.Initialized = true;
  },
  
  RenderView: function (cuid, module) {
    var container=$(cuid);
    var widget=container.find('[data-ui-field=widget]');
    var controlpopup=widget.data('ControlPopUp');
    var index,hDeb,mDeb,hFin,mFin,xDeb,xFin,cetat;
    var szCfTrm="ConfigureOptions.Table.Therm.";
    var szImgTrm="pages/control/widgets/homegenie/generic/images/temperature.png";
    var szCfOnOff="ConfigureOptions.Table.OnOff.";
    var szImgLight="pages/control/widgets/homegenie/generic/images/light_on.png";
    var szCfLevel="ConfigureOptions.Table.Level.";
    var szImgShut="pages/control/widgets/homegenie/generic/images/shutters_open.png";
    var context,choice,numId;
    var _this, etat;

    if (!this.Initialized) this.InitView($(cuid));
    
    if (!controlpopup)
    {
      _this=this;
      container.find('[data-ui-field=controlpopup]').trigger('create');
      controlpopup=container.find('[data-ui-field=controlpopup]').popup();
      widget.data('ControlPopUp',controlpopup);

      widget.find('[data-ui-field=options]').on('click',function() {
        if ($(cuid).find('[data-ui-field=widget]').data('ControlPopUp'))
        {
          $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
        }
      });
      // settings button
      widget.find('[data-ui-field=settings]').on('click', function(){
        if (module.Domain == 'HomeAutomation.HomeGenie.Automation')
        {
          HG.WebApp.ProgramEdit._CurrentProgram.Domain=module.Domain;
          HG.WebApp.ProgramEdit._CurrentProgram.Address=module.Address;
          HG.WebApp.ProgramsList.UpdateOptionsPopup();
        }
        else
        {
          HG.WebApp.Control.EditModule(module);
        }
      });		
      // tabs switching buttons
      widget.find('[data-ui-field=btn_page_thermostats]').on('click',function() {
        widget.find('[data-ui-field=div_page_thermostats]').show();
        widget.find('[data-ui-field=div_page_lights]').hide();
        widget.find('[data-ui-field=div_page_shutters]').hide();
      });
      widget.find('[data-ui-field=btn_page_lights]').on('click',function() {
        widget.find('[data-ui-field=div_page_thermostats]').hide();
        widget.find('[data-ui-field=div_page_lights]').show();
        widget.find('[data-ui-field=div_page_shutters]').hide();
      });
      widget.find('[data-ui-field=btn_page_shutters]').on('click',function() {
        widget.find('[data-ui-field=div_page_thermostats]').hide();
        widget.find('[data-ui-field=div_page_lights]').hide();
        widget.find('[data-ui-field=div_page_shutters]').show();
      });
      
      // popup values on open
      controlpopup.on('popupbeforeposition', function(evt, ui){
      });
	   	controlpopup.find('[data-ui-field=choice]').on("click",function(event){
			var rect = $(this).offset();
			_this.analyseChoice(event.pageX-rect.left);
			_this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    });
	    
	    // Validation tranche Horaire events
	    controlpopup.find('[data-ui-field=on]').on("click", function() {
	      	var hDeb=controlpopup.find('[data-ui-field=heure_deb]').val();
	      	var hFin=controlpopup.find('[data-ui-field=heure_fin]').val();

			if( _this.cetat != ' ' )
			{
				var str96b="                                                                                                ";
				var posMin=hDeb.indexOf(':');
				var heure=hDeb.substr(0,posMin);
				var minute=hDeb.substr(posMin+1,2);
				var indexD=(heure*4)+(minute/15);

				posMin=hFin.indexOf(':');
				heure=hFin.substr(0,posMin);
				minute=hFin.substr(posMin+1,2);
				var indexF=(heure*4)+(minute/15);
				var subStr,newStr;

		    	var lgStr=_this.etat.Value.length;
		    	if( lgStr < 96 )
		    	{
		    		if( lgStr == 0 )
		    			subStr=str96b;
		    		else
			    		subStr=_this.etat.Value.substr(0,lgStr)+str96b.substr(0,96-lgStr);
			    	_this.etat.Value=subStr;
		    	}
		    	
		    	if( _this.cetat == _this.etat.Value.charAt(_this.index) )
		    	{
		    		newStr=_this.etat.Value.substr(0,_this.xDeb)+str96b.substr(0,indexD-_this.xDeb);
			   		for( var i=indexD;i<indexF;i++ )
		    			newStr += _this.cetat;
		    		newStr += str96b.substr(0,_this.xFin-indexF)+_this.etat.Value.substr(_this.xFin,96-_this.xFin);
		    	}
		    	else
		    	{
		    		newStr=_this.etat.Value.substr(0,indexD);
			   		for( var i=indexD;i<indexF;i++ )
		    			newStr += _this.cetat;
		    		newStr += _this.etat.Value.substr(indexF,96-indexF);
		    	}
		    	_this.etat.NeedsUpdate='true';
	        	HG.WebApp.Utility.SetModulePropertyByName(module,_this.etat.Name,newStr);
		    	HG.WebApp.GroupModules.UpdateModule(module);
				HG.Control.Modules.ServiceCall('Table.Set',module.Domain,module.Address,_this.etat.Name,function (data) { });
	  	    	_this.drawChoice(_this.context,newStr,_this.nTab);
		    }
	    	$(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('close');
	    });
		
	    // Annulation modification tranche Horaire
		controlpopup.find('[data-ui-field=off]').on("click", function() {
	    	$(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('close');
		});
		
		widget.find('[data-ui-field=therm0]').on("click",function(event){
			_this.initSelectCanvas("AHEC",0);
			_this.context=widget.find('[data-ui-field=therm0]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"0");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"0");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
	    widget.find('[data-ui-field=therm1]').on( "click", function(event) {
			_this.initSelectCanvas("AHEC",1);
			_this.context=widget.find('[data-ui-field=therm1]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"1");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"1");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm2]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",2);
			_this.context=widget.find('[data-ui-field=therm2]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"2");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"2");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm3]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",3);
			_this.context=widget.find('[data-ui-field=therm3]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"3");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"3");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm4]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",4);
			_this.context=widget.find('[data-ui-field=therm4]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"4");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"4");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm5]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",5);
			_this.context=widget.find('[data-ui-field=therm5]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"5");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"5");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm6]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",0);
			_this.context=widget.find('[data-ui-field=therm6]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"6");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"6");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm7]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",7);
			_this.context=widget.find('[data-ui-field=therm7]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"7");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"7");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm8]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",8);
			_this.context=widget.find('[data-ui-field=therm8]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"8");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"8");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=therm9]').on("click",function(event) {
			_this.initSelectCanvas("AHEC",9);
			_this.context=widget.find('[data-ui-field=therm9]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgTrm);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_therm]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"9");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfTrm+"9");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light0]').on("click",function(event){
			_this.initSelectCanvas("FRSO",0);
			_this.context=widget.find('[data-ui-field=light0]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"0");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"0");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light1]').on("click",function(event){
			_this.initSelectCanvas("FRSO",1);
			_this.context=widget.find('[data-ui-field=light1]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"1");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"1");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light2]').on("click",function(event){
			_this.initSelectCanvas("FRSO",2);
			_this.context=widget.find('[data-ui-field=light2]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"2");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"2");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light3]').on("click",function(event){
			_this.initSelectCanvas("FRSO",3);
			_this.context=widget.find('[data-ui-field=light3]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"3");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"3");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light4]').on("click",function(event){
			_this.initSelectCanvas("FRSO",4);
			_this.context=widget.find('[data-ui-field=light4]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"4");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"4");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=light5]').on("click",function(event){
			_this.initSelectCanvas("FRSO",5);
			_this.context=widget.find('[data-ui-field=light5]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgLight);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_light]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"5");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfOnOff+"5");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
	    widget.find('[data-ui-field=shutter0]').on("click",function(event){
  			_this.initSelectCanvas("012345",0);
			_this.context=widget.find('[data-ui-field=shutter0]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"0");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"0");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=shutter1]').on("click",function(event){
			_this.initSelectCanvas("012345",1);
			_this.context=widget.find('[data-ui-field=shutter1]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"1");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"1");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=shutter2]').on("click",function(event){
			_this.initSelectCanvas("012345",2);
			_this.context=widget.find('[data-ui-field=shutter2]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"2");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"2");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=shutter3]').on("click",function(event){
			_this.initSelectCanvas("012345",3);
			_this.context=widget.find('[data-ui-field=shutter3]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"3");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"3");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=shutter4]').on("click",function(event){
			_this.initSelectCanvas("012345",4);
			_this.context=widget.find('[data-ui-field=shutter4]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"4");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"4");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
		widget.find('[data-ui-field=shutter5]').on("click",function(event){
			_this.initSelectCanvas("012345",5);
			_this.context=widget.find('[data-ui-field=shutter5]')[0].getContext('2d');
			controlpopup.find('[data-ui-field=icon]').attr('src',szImgShut);
			controlpopup.find('[data-ui-field=group]').html(widget.find('[data-ui-field=id_title_shut]').html());
	  		controlpopup.find('[data-ui-field=name]').html(_this.TimeTableUI+"5");
		 	_this.etat=HG.WebApp.Utility.GetModulePropertyByName(module,szCfLevel+"5");
			if(_this.analyseClick($(cuid),_this.etat.Value,$(this).offset(),event) != -1)
			{
			  _this.drawChoice(controlpopup.find('[data-ui-field=choice]')[0].getContext('2d'),_this.choice,_this.cetat,30);
	    	  $(cuid).find('[data-ui-field=widget]').data('ControlPopUp').popup('open');
	    	}
	    });
    }
    else
      _this=this;
    
    this.Description=(module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);

	for( var i=0 ; i<10 ; i++ )
	{
	 	etat=HG.WebApp.Utility.GetModulePropertyByName(module,'ConfigureOptions.Table.Therm.'+i);
	    this.drawTable(widget.find('[data-ui-field=therm'+i+']')[0].getContext('2d'),etat.Value,i);
	}
	for( var i=0 ; i<6 ; i++ )
	{
	 	etat=HG.WebApp.Utility.GetModulePropertyByName(module,'ConfigureOptions.Table.OnOff.'+i);
	    this.drawTable(widget.find('[data-ui-field=light'+i+']')[0].getContext('2d'),etat.Value,i);
	}
	for( var i=0 ; i<6 ; i++ )
	{
	 	etat=HG.WebApp.Utility.GetModulePropertyByName(module,'ConfigureOptions.Table.Level.'+i);
	    this.drawTable(widget.find('[data-ui-field=shutter'+i+']')[0].getContext('2d'),etat.Value,i);
	}

  },
  
initSelectCanvas: function(choice,nTab) {
	this.choice=choice;
	this.nTab=nTab;
  },
   
analyseClick: function(container,etat,rect,event) {
    var posX,posY;
	posX=event.pageX-rect.left;
	posY=event.pageY-rect.top;
	var c=etat.length;
    this.index=Math.floor((posX-10)/3);
    var widget=container.find('[data-ui-field=widget]');
    var controlpopup=widget.data('ControlPopUp');
 
    if( (posY > 30) && (posY < 44) && (this.index >=0) && (this.index<96) )
    {
      for( var i=this.index ; i>=0 ; i-- )
      {   
        if( etat[i] != etat[this.index] )
          break;
          this.xDeb=i;
      }
      if( this.index < c )
      { 
      	this.cetat=etat[this.index];
       	var j=this.index,c=etat.length;
        for(  ; j<c ; j++ )
        {   
          this.xFin=j;
          if( etat[j] != etat[this.index] )
            break;
        }
        if( j == c )
          this.xFin++;
      }
      else
      {
      	this.cetat=' ';
       	this.xDeb=c;
       	this.xFin=96;
      }
      this.hDeb=Math.floor(this.xDeb/4);
      this.mDeb=((this.xDeb/4)-this.hDeb)*60;
      this.hFin=Math.floor(this.xFin/4);
      this.mFin=((this.xFin/4)-this.hFin)*60;
      this.timeDeb=this.hDeb + ':' + this.mDeb;
      this.timeFin=this.hFin + ':' + this.mFin;
 	  controlpopup.find('[data-ui-field=heure_deb]').empty();
	  controlpopup.find('[data-ui-field=heure_fin]').empty();
	  for( var i=this.hDeb,j=this.mDeb/15 ; i <= this.hFin ; i++ )
	  {
		for( ; j<4 ; j++ )
		{
	 	  if( j == 0 )
			szHeure=i+':00';
		  else
			szHeure=i+':'+(j*15);
		  controlpopup.find('[data-ui-field=heure_deb]').append('<option value="'+ szHeure +'">'+szHeure+'</option>');
		  controlpopup.find('[data-ui-field=heure_fin]').append('<option value="'+ szHeure +'">'+szHeure+'</option>');
		  if( (i == this.hDeb) && (j == (this.mDeb/15)) )
		  {
			controlpopup.find('[data-ui-field=heure_deb]').val( szHeure );
			controlpopup.find('[data-ui-field=heure_deb]').selectmenu('refresh', true);
		  }
		  if( (i == this.hFin) && (j == (this.mFin/15)) )
		  {
			controlpopup.find('[data-ui-field=heure_fin]').val( szHeure );
			controlpopup.find('[data-ui-field=heure_fin]').selectmenu('refresh', true);
			break;
		  }
		}
		j=0;
	  }
    }
    else
      this.index=-1;
    return this.index;
  },
  
  analyseChoice: function(posX)
  {
	var c=this.choice.length;
	var xlen=216/c;
	var index=Math.floor(posX/xlen);
    
    this.cetat=this.choice[index];
  },
  
  drawTable: function(context,etat,name) 
  {
    var x=10;
    var y=28;
    var nb=0;
    var fgcolor ;

    context.font = "8pt Arial";
    context.fillStyle = "black";
    context.fillText(name,8,10);
    context.font = "6pt Arial";
    for(var i=0 , c = etat.length ; i < 96 ; i++ )
    {
        if( i < c )
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
               case 'O' :
                fgcolor = "green" ;
                break ;
               case 'S' :
                fgcolor = "#666600" ;
                break ;
               case 'R' :
                fgcolor = "#999900" ;
                break ;
               case 'F' :
                fgcolor = "#444444" ;
                break ;
               case '0' :
                fgcolor = "#222200" ;
                break ;
               case '1' :
                fgcolor = "#333300" ;
                break ;
               case '2' :
                fgcolor = "#666600" ;
                break ;
               case '3' :
                fgcolor = "#999900" ;
                break ;
               case '4' :
                fgcolor = "#CCCC00" ;
                break ;
               case '5' :
                fgcolor = "yellow" ;
                break ;
               default :
                fgcolor = 'black' ;
                break ;
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
  },
   
  drawChoice: function(context,type,select,height) {
    var x=0;
    var y=0;
  	var c= type.length ;  
  	var xlen = 216/c ;
    var fgcolor ;
    var texte ;

    context.font = "8pt Arial";
    for(var i=0 ; i < c ; i++ )
    {
        switch( type[i] )
        {
           case 'A' :
           	texte = this.ThermLocalUI[0];
            fgcolor = "#bcbcbc" ;
            break ;
           case 'H' :
           	texte = this.ThermLocalUI[1];
            fgcolor = "#2387dc" ;
            break ;
           case 'E' :
           	texte = this.ThermLocalUI[2];
            fgcolor = "#53a840" ;
            break ;
           case 'C' :
//        	texte = widget.find('[data-ui-label=id_heat]').html();
           	texte = this.ThermLocalUI[3];
            fgcolor = "#da4336" ;
            break ;
           case 'O' :
           	texte = "On" ;
            fgcolor = "green" ;
           	break ;
           case 'S' :
           	texte = "SunSet" ;
            fgcolor = "#666600" ;
           	break ;
           case 'R' :
           	texte = "SunRise" ;
            fgcolor = "#999900" ;
           	break ;
           case 'F' :
           	texte = "Off" ;
            fgcolor = "#444444" ;
           	break ;
           case '0' :
           	texte = "0 %" ;
            fgcolor = "#222200" ;
           	break ;
           case '1' :
           	texte = "20 %" ;
            fgcolor = "#333300" ;
           	break ;
           case '2' :
           	texte = "40 %" ;
            fgcolor = "#666600" ;
           	break ;
           case '3' :
           	texte = "60 %" ;
            fgcolor = "#999900" ;
           	break ;
           case '4' :
           	texte = "80 %" ;
            fgcolor = "#CCCC00" ;
           	break ;
           case '5' :
           	texte = "100%" ;
            fgcolor = "yellow" ;
           	break ;
        }
        context.fillStyle=fgcolor;
        if( select == type[i] )
       	{
			context.beginPath();
			context.lineWidth="3";   
			context.strokeStyle="blue";   
			context.rect(x,0,xlen-1,height);
			context.fill();
			context.stroke();       		
       	}
       	else
	        context.fillRect(x,0,xlen,height);
        if( (fgcolor=="black") || (fgcolor=="#222200") || (fgcolor=="#444444") )
		    context.fillStyle = "white";
        else
    		context.fillStyle = "black";
    	context.fillText(texte,x+6,(height/2)+5);
    	x += xlen ;
    }
  }
}]
