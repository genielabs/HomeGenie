[{
  Name: 'Calendrier Widget',
  Author: 'DaniMail',
  Version: '2014-12-20',

  GroupName: '',
  IconImage: 'images/scheduler.png',
  StatusText: '',
  Description: '',
  Initialized: false,
  TabDayUI: ["sun","mon","tue","wed","thu","fri","sat"],
  TabMonthUI: ["jan","feb","mar","apr","mai","jun","jul","aug","sep","oct","nov","dec"],
  TabLegendUI: ["wek","work","free","spe"],
 
  InitView: function (container,offsetDay,tabMois,numMonth,newYear) {
    var _this = this;
   	var widget=container.find('[data-ui-field=widget]');
    setTimeout(function () {
        for(var i = 0; i < _this.TabDayUI.length; i++)
          _this.TabDayUI[i] = widget.find('[data-ui-label=id_'+_this.TabDayUI[i]+']').html();
        for(var i = 0; i < _this.TabMonthUI.length; i++)
          _this.TabMonthUI[i] = widget.find('[data-ui-label=id_'+_this.TabMonthUI[i]+']').html();
        for(var i = 0; i < _this.TabLegendUI.length; i++)
          _this.TabLegendUI[i] = widget.find('[data-ui-label=id_'+_this.TabLegendUI[i]+']').html();
        _this.drawLegend(widget.find('[id=canvas_mois]')[0].getContext('2d'));
        _this.drawMois(widget.find('[id=canvas_mois]')[0].getContext('2d'),offsetDay,tabMois,numMonth,newYear);
    },500);
   this.Initialized = true;
  },
  
  RenderView: function (cuid, module) {
	var dateTime=new Date();
	var fullYear=dateTime.getFullYear();
  	var numMonth=dateTime.getMonth();
	var debTime=new Date( fullYear, numMonth, 1 );
  	var offsetDay=debTime.getDay();
	var needsUpdate='false';
  	var newYear=fullYear;
	var offYear;
	
	if(fullYear%2 == 0) 
	  offYear=0; else
	  offYear=1;
    var container=$(cuid);
    var widget=container.find('[data-ui-field=widget]');
    this.Description=(module.Domain.substring(module.Domain.lastIndexOf('.') + 1)) + ' ' + module.Address;
    widget.find('[data-ui-field=name]').html(module.Name);
    widget.find('[data-ui-field=description]').html(this.Description);

    var tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
    
    if (!this.Initialized) this.InitView($(cuid),offsetDay,tabMois.Value,numMonth,newYear);
    
    var context=$("#canvas_mois")[0].getContext('2d');
   _this=this;

   $("#canvas_mois").on( "click", function( event ) {
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
				debTime=new Date(newYear,numMonth,1);
				offsetDay=debTime.getDay();
    			tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
	        	_this.drawMois(context,offsetDay,tabMois.Value,numMonth,newYear);
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
				debTime=new Date(newYear,numMonth,1);
				offsetDay=debTime.getDay();
    			tabMois=HG.WebApp.Utility.GetModulePropertyByName(module, 'ConfigureOptions.Calend.Year.'+offYear+'.'+numMonth );
	        	_this.drawMois(context,offsetDay,tabMois.Value,numMonth,newYear);
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
	        	var newStr=tabMois.Value.substr(0,index)+subStr+tabMois.Value.substr(index+1);
	        	tabMois.NeedsUpdate='true';
	        	HG.WebApp.Utility.SetModulePropertyByName(module,tabMois.Name,newStr);
	        	_this.drawMois(context,offsetDay,tabMois.Value,numMonth,newYear);
				needsUpdate='true';
        	}
        }
    });
  },

  drawLegend: function(context) {
    var x=6;
    var y=244;
  	var xlen=252/4;
  	var type="WOFS";
    var fgcolor;

    context.font = "8pt Arial";
    for(var i=0 ; i < 4 ; i++ )
    {
        switch( type[i] )
        {
           case 'W' :
            fgcolor = "#bcbcbc" ;
            break ;
           case 'O' :
            fgcolor = "#2387dc" ;
            break ;
           case 'F' :
            fgcolor = "#53a840" ;
            break ;
           case 'S' :
            fgcolor = "#da4336" ;
            break ;
        }
   		context.fillStyle=fgcolor;
        context.fillRect(x,y,xlen-2,30);
   		context.fillStyle="black";
    	context.fillText(this.TabLegendUI[i],x+6,y+20);
    	x+=xlen;
	}
  },
  
  drawMois: function(context,offsetDay,tabMois,numMonth,year)
  {
    var x=6;
    var y=28;
    var fgcolor;

    context.font = "14pt Arial";

    context.fillStyle = "grey";
    context.fillRect(6,2,34,24);
  	context.fillStyle = "black";
    context.fillText( '<', 14, 21 );
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="black";   
	context.rect(7,3,32,22);
	context.stroke();       		
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="white";   
	context.rect(8,4,30,20);
	context.stroke();       		

    context.fillStyle = "grey";
    context.fillRect(222,2,34,24);
  	context.fillStyle = "black";
    context.fillText( '>', 236, 21 );
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="black";   
	context.rect(223,3,32,22);
	context.stroke();       		
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="white";   
	context.rect(224,4,30,20);
	context.stroke();       		

    context.fillStyle = "grey";
    context.fillRect(42,2,178,24);
    context.fillStyle = "black";
    context.textAlign = "center";
    context.fillText(this.TabMonthUI[numMonth]+" "+year,130,21);
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="black";   
	context.rect(43,3,176,22);
	context.stroke();       		
 	context.beginPath();
	context.lineWidth="1";   
	context.strokeStyle="white";   
	context.rect(44,4,174,20);
	context.stroke();       		

    context.font = "12pt Arial";
    context.textAlign = "left";
    for( var i=0,x=6 ; i<7 ; i++ )
    {
	    context.fillStyle = "grey";
        context.fillRect(x,y,34,32);
    	context.fillStyle = "black";
        context.fillText(this.TabDayUI[i],x+9,y+21);
        x += 36 ;
    }
	x = 6 ;
	y += 34 ;
    context.font = "10pt Arial";
    for(var i=0, j=0, c = tabMois.length ; i < 42 ; i++ )
    {
    	if( i < offsetDay )
           	fgcolor = "grey";
        else
        {
        	if( j < c )
        	{
		        switch( tabMois[j] )
		        {
		           case 'W' :
		            fgcolor = "#bcbcbc" ;
		            break ;
		           case 'O' :
		            fgcolor = "#2387dc" ;
		            break ;
		           case 'F' :
		            fgcolor = "#53a840" ;
		            break ;
		           case 'S' :
		            fgcolor = "#da4336" ;
		            break ;
		           default :
		           	fgcolor = 'grey' ;
		           	break ;
		        }
		     }
		     else
	           	fgcolor = "grey";
	        j++ ;
        }
        if( (i != 0) && (i%7 === 0) )
        {
            x = 6 ;
            y += 30 ;
        }
        context.fillStyle=fgcolor;
        context.fillRect(x,y,34,28);
        if( (j != 0) && (j <= c ) )
        {
    		context.fillStyle = "black";
        	context.fillText(j,x+((j<10)?14:10),y+18);
       	}
        x += 36 ;
    }
  },    
}]
