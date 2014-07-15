HG.WebApp.Apps = Array();
HG.WebApp.Apps.NetPlay = Array();
HG.WebApp.Apps.NetPlay.SlideShow = Array();
HG.WebApp.Apps.NetPlay.SlideShow.DisplayImage = function(displayid, cts)
{		
		$.mobile.changePage('#page_apps_netplay', { transition: "flow"});

		var slidediv = '#bgapp_netplay_image1';
		if ($('#bgapp_netplay_image1').is(':visible'))
		{	
			$('#bgapp_netplay_image1').animate({ top: 0, left: -4000 }, 2000, function() { $('#bgapp_netplay_image1').hide(); } );
			slidediv = '#bgapp_netplay_image2';
		}
		else
		{
			$('#bgapp_netplay_image2').animate({ top: 0, left: -4000 }, 2000, function() { $('#bgapp_netplay_image2').hide(); }  );
			slidediv = '#bgapp_netplay_image1';
		}
		setTimeout(function(){ 
			if (displayid == "") displayid = "0"; // 0 = broadcast to all displays (only supproted method currently)
			$(slidediv).html('<img src="/' + HG.WebApp.Data.ServiceKey + '/Protocols.AirPlay/' + displayid + '/Control.GetImage/' + (new Date().getTime()) + '" height="100%">');
			$(slidediv).show();
			$(slidediv).animate({ top: 0, left: 0 }, 2000 );
		});   
};


