[{
  Name: 'Calendrier Widget',
  Author: 'DaniMail',
  Version: '2014-12-20',

  GroupName : '',
  IconImage : 'images/scheduler.png',
  StatusText : '',
  Description : '',
  
  RenderView: function (cuid, module) {

	var dateTime=new Date();
	var numMonth=dateTime.getMonth();
	var fullYear=dateTime.getFullYear();
	var debTime=new Date( fullYear, numMonth, 1 );
	var offsetDay=debTime.getDay();
	var newYear=fullYear;
	var needsUpdate='false';
	var offYear;
	
	if( fullYear%2 == 0 )
		offYear=0 ;
	else
		offYear=1;
    
    var container=$(cuid);
    var widget=container.find('[data-ui-field=widget]');
    var controlpopup = widget.data('ControlPopUp');
    this.Description=(module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);

    var tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
    var context=$("#canvas_mois")[0].getContext('2d');
    drawMois(context,offsetDay,tabMois.Value,numMonth,newYear);
    drawLegend(context);

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

      // popup values on open
      controlpopup.on('popupbeforeposition', function(evt, ui){
      });
	  $( "#canvas_mois" ).on( "click", function( event ) {
			var rect=$(this).offset();
	        var Posx,Posy;
			Posx=event.pageX-rect.left;
			Posy=event.pageY-rect.top;
		
	       	if((Posy > 2) && (Posy < 28))
	       	{
	       		var nIndex=Math.floor((Posx-6)/36);
	       		
				if(needsUpdate == 'true')
				{
					needsUpdate='false' ;
		        	HG.WebApp.GroupModules.UpdateModule( module );
					HG.Control.Modules.ServiceCall('Calendrier.SetMonth',module.Domain,module.Address,newYear+'/'+numMonth, function (data) { });
		        }
	       		if(nIndex == 0)
	       		{
	       			if(numMonth > 0)
	       				numMonth -- ;
	       			else
	       			{
	       				if(newYear != fullYear)
	       				{
	       					numMonth=11 ;
	       					newYear=fullYear ; 
	       				}
	       			}
					if(newYear%2 == 0)
						offYear=0 ;
					else
						offYear=1 ;
					debTime=new Date( newYear, numMonth, 1 );
					offsetDay=debTime.getDay();
	    			tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
		        	drawMois( context, offsetDay, tabMois.Value, numMonth, newYear );
	       		}
	       		if(nIndex == 6)
	       		{
	       			if(numMonth < 11)
	       				numMonth++;
	       			else
	       			{
	       				if(newYear == fullYear)
	       				{
	       					numMonth=0 ;
	       					newYear=fullYear+1; 
	       				}
	       			}
					if(newYear%2 == 0)
						offYear=0;
					else
						offYear=1;
					debTime=new Date( newYear, numMonth, 1 );
					offsetDay=debTime.getDay();
	    			tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
		        	drawMois( context, offsetDay, tabMois.Value, numMonth, newYear );
	       		}
	        }
	        if((Posy > 62) && (Posy < 212))
	        {
	       		var xIndex=Math.floor((Posx-6)/36); 
	            var yIndex=Math.floor((Posy-62)/30); 
	            var index=(yIndex*7)+xIndex ;
	            if(index >= offsetDay)
	            {
	            	index -= offsetDay ;
		        	var subStr=tabMois.Value.charAt(index);
		        	
		        	if( subStr == 'W' )
		        		subStr='O';
		        	else
		        	if( subStr == 'O' )
		        		subStr='F';
		        	else
		        	if( subStr == 'F' )
		        		subStr='S';
		        	else
		        	if( subStr == 'S' )
		        		subStr='W';
		        	var newStr=tabMois.Value.substr(0,index) + subStr + tabMois.Value.substr(index+1);
		        	tabMois.NeedsUpdate='true';
		        	HG.WebApp.Utility.SetModulePropertyByName(module, tabMois.Name, newStr );
		        	drawMois( context, offsetDay, tabMois.Value, numMonth, newYear );
					needsUpdate='true';
	        	}
	        }
	    });
    }
    else
      _this=this;
  },
}]
