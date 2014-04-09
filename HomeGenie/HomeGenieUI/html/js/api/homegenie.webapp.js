HG.WebApp = HG.WebApp || {};
//
HG.WebApp.Data = HG.WebApp.Data || {};
//
HG.WebApp.Data.Modules = Array(); 
HG.WebApp.Data.Groups = Array();  
HG.WebApp.Data.AutomationGroups = Array();
HG.WebApp.Data.Programs = Array();
HG.WebApp.Data.ServiceKey = 'api';
HG.WebApp.Data.ServiceDomain = "HomeAutomation.HomeGenie";
//
HG.WebApp.Data.ZWaveMainGroup = "1";
HG.WebApp.Data.ZWaveControllerId = "1";
HG.WebApp.Data.CurrentKw = -1;
//
HG.WebApp.Data._CurrentGroup = null;
HG.WebApp.Data._CurrentModule = null;
HG.WebApp.Data._IgnoreUIEvents = false;
//
HG.WebApp.Data._CurrentLocale = {};
//
var now = new Date();
HG.WebApp.Data.LoggingFrom = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(),  now.getUTCHours(), now.getUTCMinutes(), now.getUTCSeconds(), now.getUTCMilliseconds()));
//
//
//
HG.WebApp.InitializePage = function ()
{
    //
    // application start - init stuff
    //
	$.ajaxSetup({ cache: false });
	$.mobile.ajaxFormsEnabled = false;
    //
	HG.Configure.LoadData();
    //
    window.setInterval('HG.WebApp.Home.UpdateHeaderStatus();', 10000);
    //window.setInterval('HG.WebApp.Home.LoggingPoll();', 5000);
    //
    //HG.WebApp.SystemSettings.CheckConfigureStatus();
	//
	// page open - init stuff
	//
	//$(document).delegate('[data-role="page"]', 'pagecreate', function (e) {
	//});
	//
    //$('[data-role=page]').on('pageshow', function (event) 
    //{
    //});
	//
    $('[data-role=page]').on('pagebeforeshow', function (event) 
    {
		setTheme(uitheme);
        //
        if (this.id == "page_control") // && HG.WebApp.Control._RefreshIntervalObject == null) 
        {
            // init "Control" page
	        HG.Automation.Programs.List(function () {
	            //if ($('#control_groupslist').children().length == 0) 
	            {
				    $.mobile.showPageLoadingMsg();
				    HG.Configure.Groups.List('Control', function () 
				    {
				        if ($('#control_groupslist').children().length == 0) 
				        {
				            HG.WebApp.Control.RenderGroupsCollapsibleItems();
				        }
				        HG.WebApp.Control._RefreshFn();
			            //HG.WebApp.Control.Refresh();
			            // HG.WebApp.Control.SetAutoRefresh( true );
				    });    
	            }
	        });
            //
            HG.Automation.Macro.GetDelay(function(data){
                $('#macrorecord_delay_none').prop('checked', false).checkboxradio( 'refresh' );
                $('#macrorecord_delay_mimic').prop('checked', false).checkboxradio( 'refresh' );
                $('#macrorecord_delay_fixed').prop('checked', false).checkboxradio( 'refresh' );
                $('#macrorecord_delay_' + data.DelayType.toLowerCase()).prop('checked', true).checkboxradio( 'refresh' );
                $('#macrorecord_delay_seconds').val(data.DelayOptions);
            });
        }
        else if (this.id == "page_home")
        {
            //HG.WebApp.SystemSettings.CheckConfigureStatus();
        }
        else if (this.id == "page_analyze") 
        {
            HG.WebApp.Statistics.SetAutoRefresh(true);
            window.setTimeout(function(){
                HG.WebApp.Statistics.Refresh();
            }, 500);
        }
        else if (this.id == 'page_configure_interfaces') 
        {
        	HG.WebApp.SystemSettings.LoadSettings();
        }
        else if (this.id == 'page_configure_maintenance')
        {
            HG.WebApp.Maintenance.LoadSettings();
        }
        else if (this.id == 'page_configure_groups') 
        {
    	    HG.Configure.Modules.List(function (data) {
	            try
	            {
	        	    HG.WebApp.Data.Modules = eval(data);                   
	            } catch (e) { }
    	        HG.Automation.Programs.List(function () {
    	            HG.WebApp.GroupsList.LoadGroups();
                });
            });
        }
        else if (this.id == 'page_configure_groupmodules') 
        {
            HG.WebApp.GroupModules.LoadGroupModules();
            $.mobile.showPageLoadingMsg();
//            setTimeout('HG.WebApp.GroupModules.LoadGroupModules()', 1000);
        }
        else if (this.id == 'page_configure_automationgroups') 
        {
    	    HG.Automation.Programs.List(function () {
    	        HG.WebApp.AutomationGroupsList.LoadGroups();
            });
        }
        else if (this.id == 'page_configure_schedulerservice')
        {
            HG.WebApp.Scheduler.LoadScheduling();
        }
        else if (this.id == 'page_automation_programs') 
        {
	        HG.WebApp.ProgramsList.LoadPrograms();
        }
        else if (this.id == 'page_automation_editprogram_code') 
        {
            $('#automation_program_scriptcondition').next().css('display', 'none');
            $('#automation_program_scriptsource').next().css('display', '');
            HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
            if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors.trim() != '' && HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors.trim() != '[]')
            {
                HG.WebApp.ProgramEdit.ShowProgramErrors(HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors);
            }
        }
        else if (this.id == 'page_automation_editprogram') 
        {	            
			automationpage_ConditionsRefresh();                                                    
			automationpage_CommandsRefresh();                                                   
        }
        //
		$(window).bind('scroll resize', function() {
		    $('#statuspopup').css('top', $(this).scrollTop() + 40);
		});
    });
	//
	// page close - cleanup stuff
	//
    $('[data-role=page]').on('pagehide', function (event) {
        if ((this.id == 'page_control' || this.id == 'page_configure_groups')) 
        {
            HG.WebApp.Control.SetAutoRefresh( false );
        }
        else if (this.id == "page_analyze") 
        {
            HG.WebApp.Statistics.SetAutoRefresh( false );
        }
    });
	//
    setTimeout(function(){
	    var userLang = (navigator.language) ? navigator.language : navigator.userLanguage;
	    HG.WebApp.Locales.Localize('./locales/' + userLang.toLowerCase().substring(0, 2) + '.json');
	    // enable/disable speech input
	    if (typeof document.createElement('input').webkitSpeech == 'undefined') {
		    //no speech support
		    $('#speechinput').hide();
	    }
	    else {
		    // lookup for a localized version, if any
		    HG.VoiceControl.Localize('./locales/' + userLang.toLowerCase().substring(0, 2) + '.lingo.json', function(success) {
			    if (!success)
			    {
				    // fallback to english lingo file
				    HG.VoiceControl.Localize('./locales/en.lingo.json', function(res){ });
			    }
		    });
	    }
        setTheme(sessvars.UserSettings.UiTheme);
    }, 1000);
};


//
// namespace : HG.WebApp.Control 
// info      : -
//
{include pages/control/_control.js}		
//
// namespace : HG.WebApp.Statistics 
// info      : -
//
{include pages/analyze/_statistics.js}		
//
// namespace : HG.WebApp.GroupsList 
// info      : Configure/Groups ui logic (Groups List)
//
{include pages/configure/groups/_groupslist.js}		
//
// namespace : HG.WebApp.GroupModules 
// info      : Configure/Groups ui logic (Group Modules)
//
{include pages/configure/groups/_groupmodules.js}	
//
// namespace : HG.WebApp.SystemSettings 
// info      : -
//
{include pages/configure/interfaces/_systemsettings.js}	
//
// namespace : HG.WebApp.Maintenance 
// info      : -
//
{include pages/configure/maintenance/_maintenance.js}	
//
// namespace : HG.WebApp.AutomationGroups 
// info      : -
//
{include pages/configure/programengine/_groupslist.js}	
//
// namespace : HG.WebApp.ProgramsList 
// info      : -
//
{include pages/configure/programengine/_programslist.js}	
//
// namespace : HG.WebApp.ProgramEdit 
// info      : -
//
{include pages/configure/programengine/_programedit.js}	
//
// namespace : HG.WebApp.Scheduler 
// info      : -
//
{include pages/configure/scheduler/_scheduler.js}	
//
// namespace : HG.WebApp.Apps.NetPlay.SlideShow 
// info      : -
//
{include pages/apps/netplay/_slideshow.js}	
//
// namespace : HG.WebApp.Home namespace
// info      : -
//
HG.WebApp.Home = HG.WebApp.Home || {};
HG.WebApp.Home.UpdateHeaderStatus = function()
{
	HG.WebApp.Home.UpdateInterfacesStatus();
};
//
HG.WebApp.Home.PopupEvent = function(eventLog)
{
    var module = HG.WebApp.Utility.GetModuleByDomainAddress(eventLog.Domain, eventLog.Source);
    if (module != null)
    {
        var curprop = HG.WebApp.Utility.GetModulePropertyByName(module, eventLog.Property);
        // discard dupes if event is not a automation program event
        if (module.Domain != 'HomeAutomation.HomeGenie.Automation' && curprop !== null && curprop.Value == eventLog.Value) return; 
        // update current event property 
        HG.WebApp.Utility.SetModulePropertyByName(module, eventLog.Property, eventLog.Value, eventLog.Timestamp);
        HG.WebApp.Control.UpdateModuleWidget(eventLog.Domain, eventLog.Source);
        // when event is an automation program event, we update the whole module
        if (module.Domain == 'HomeAutomation.HomeGenie.Automation')
        {
            HG.Configure.Modules.Get(module.Domain, module.Address, function (data) {
                try
                {
                    var mod = eval('[' + data + ']')[0];
                    var idx = HG.WebApp.Utility.GetModuleIndexByDomainAddress(mod.Domain, mod.Address);
                    HG.WebApp.Data.Modules[idx] = mod;
                    HG.WebApp.Control.UpdateModuleWidget(mod.Domain, mod.Address);
                } catch (e) { }
            });
        }
    }
    //
	var ts = -1;
	var now = new Date();
    var s = '';
    //
    if (eventLog.UnixTimestamp >= HG.WebApp.Data.LoggingFrom)
    {
        //
        if (ts == -1) ts = eventLog.UnixTimestamp;
        switch (eventLog.Domain)
    	{
            case 'HomeGenie.System':
                if (eventLog.Value == 'STARTED')
                {
                    $('#configure_system_updateinstall_status').html('Update install complete. HomeGenie started.');
                    setTimeout(function(){
                        document.location.href = '/';
                    }, 3000);
                }
            case 'HomeGenie.UpdateChecker':
                $('#configure_system_updateinstall_log').prepend('*&nbsp;<strong>' + eventLog.Property + '</strong><br/>&nbsp;&nbsp;' + eventLog.Value + '<br/>');
            case 'HomeGenie.Automation':
        	case 'HomeAutomation.HomeGenie.Automation':
        		if (eventLog.Property != 'Meter.Watts')
        		{
	            	//
        			//var icon = '<img src="' + configurepage_GetModuleIcon(module, null) + '" width="48" height="48">';
	            	//s += '<table><tr><td>';
	            	//s += icon;
	            	//s += '</td><td valign="top">';
	            	//s += '' + eventLog.Property + '<br>' + eventLog.Value + '<br>';
        		    //s += '</tr></table>';
                    var iconImage = configurepage_GetModuleIcon(module, null);
                    $('#content').notify("create", "notifypopup", {
                        icon: iconImage,
                        title: eventLog.Property,
                        text: eventLog.Value,
                    });
        		}
        	break;
        	default:
        		//var module = HG.WebApp.Utility.GetModuleByDomainAddress(eventLog.Domain, eventLog.Source);
        		if (module != null && eventLog.Property != 'Meter.Watts')
        		{
                    //var curprop = HG.WebApp.Utility.GetModulePropertyByName(module, eventLog.Property);
                    //if (curprop !== null && curprop.Value == eventLog.Value) break; // discard dupes
                    //
        			// update current module prop
	            	//HG.WebApp.Utility.SetModulePropertyByName(module, eventLog.Property, eventLog.Value);
	            	//
        			var iconImage = configurepage_GetModuleIcon(module, null);
            		if ((module.Address == 'RF' || module.Address == 'IR') && eventLog.Value != '')
            		{
            			iconImage = 'images/remote.png';
            			$('#content').notify("create", "notifypopup", {
            			    icon: iconImage,
            			    title: module.Address + ' ' + eventLog.Property,
            			    text: eventLog.Value,
            			});
                        //
                        //$('#comparison_value_input').val(eventLog.Value);
            		}
            		else if (eventLog.Property.substring(0, 7) == 'Sensor.')
            		{
		            	var group = HG.WebApp.GroupsList.GetModuleGroup(module);
		            	if (group != null) group = group.Name;
		            	var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
		            	var logdate = new Date(eventLog.UnixTimestamp);
		            	var date = HG.WebApp.Utility.FormatDateTime(logdate);
		            	var propname = eventLog.Property.substring(eventLog.Property.indexOf('.') + 1);
		            	//
		            	switch (propname)
		            	{
		            	case 'Temperature':
	            			iconImage = 'pages/control/widgets/homegenie/generic/images/temperature.png';
	            			break;
		            	case 'Luminance':
	            			iconImage = 'pages/control/widgets/homegenie/generic/images/luminance.png';
	            			break;
		            	default:
	            			iconImage = 'pages/control/widgets/homegenie/generic/images/sensor.png';
	            			break;
		            	}
		            	//
		            	if (module.Name != '') name = module.Name;
		            	if (group == null) group = '';
		            	//
		            	$('#content').notify("create", "notifypopup", {
		            	    icon: iconImage,
		            	    title: '<span style="color:gray;font-size:8pt;">' + group + '</span><br><b>' + name + '</b><br>' + propname,
		            	    text: parseFloat(eventLog.Value.replace(',', '.')).toFixed(2)
		            	});
		            	//s += '<table width="100%"><tr><td width="48" rowspan="2">';
		            	//s += icon;
		            	//s += '</td><td valign="top" align="left">';
		            	//s += '<span style="color:gray;font-size:8pt;">' + group + '</span><br><b>' + name + '</b><br>' + propname;
		            	//s += '</td><td align="right" style="color:lime;font-size:12pt">' + parseFloat(eventLog.Value.replace(',', '.')).toFixed(2) + '</td></tr>';
            			//s += '<tr><td colspan="2" align="right"><span style="color:gray;font-size:8pt;">' + date + '</span>';
            			//s += '</td></tr></table>';
            			
            		}
		            else if (eventLog.Property.substring(0, 7) == 'Status.')
		            {
		            	var group = HG.WebApp.GroupsList.GetModuleGroup(module);
		            	if (group != null) group = group.Name;
		            	var name = module.Domain.substring(module.Domain.indexOf('.') + 1) + ' ' + module.Address;
		            	var propname = eventLog.Property.substring(eventLog.Property.indexOf('.') + 1);
		            	var value = (parseFloat(eventLog.Value.replace(',', '.')));
                        if (propname == 'Level')
                        {
                            value = value * 100;
                            if (value > 98) value = 100;
                        }
                        value += '%'; 
		            	var logdate = new Date(eventLog.UnixTimestamp);
		            	var date = HG.WebApp.Utility.FormatDateTime(logdate);
		            	//
		            	if (module.Name != '') name = module.Name;
		            	if (group == null) group = '';
		            	//
		            	$('#content').notify("create", "notifypopup", {
		            	    icon: iconImage,
		            	    title: '<span style="color:gray;font-size:8pt;">' + group + '</span><br><b>' + name + '</b>',
		            	    text: propname + ' ' + value
		            	});
                        //
		            	//s += '<table width="100%"><tr><td width="48" rowspan="2">';
		            	//s += icon;
		            	//s += '</td><td valign="top" align="left">';
		            	//s += '<span style="color:gray;font-size:8pt;">' + group + '</span><br><b>' + name + '</b>';
		            	//s += '</td><td align="right" style="color:lime;font-size:12pt">' + propname + ' ' + value + '</td></tr>';
            			//s += '<tr><td colspan="2" align="right"><span style="color:gray;font-size:8pt;">' + date + '</span>';
            			//s += '</td></tr></table>';
		        	}
                    //
                    if (HG.WebApp.ProgramEdit._IsCapturingConditions && eventLog.Value != '')
                    {
                        var conditionobj = { 
                            'Domain' : module.Domain, 
                            'Target' : module.Address,
                            'Property' : eventLog.Property, 
                            'ComparisonOperator' : 'Equals', 
                            'ComparisonValue' : eventLog.Value
                        };
                        HG.WebApp.ProgramEdit._CurrentProgram.Conditions.push( conditionobj );
                        automationpage_ConditionsRefresh();                                        
                    }
                    else if (HG.WebApp.ProgramEdit._IsCapturingCommands && eventLog.Value != '')
                    {
                        var command = HG.WebApp.Utility.GetCommandFromEvent(module, eventLog);
                        if (command != null)
                        {
                            HG.WebApp.ProgramEdit._CurrentProgram.Commands.push( command );
                            automationpage_CommandsRefresh();
                        }
                    }
	        	}
	        	else
	        	{
	        		if (eventLog.Domain == 'Protocols.AirPlay' && eventLog.Property == 'PlayControl.DisplayImage')
	        		{
		            	var logdate = new Date(eventLog.UnixTimestamp);
		            	var date = HG.WebApp.Utility.FormatDateTime(logdate);
		            	
		            	s += '<table width="100%"><tr><td width="48" rowspan="2">';
		            	s += '<a _href="#dialog_netplay_show_popup" -data-rel="popup"><img src="images/playcontrol.png" width="48" height="48"></a>';
		            	s += '</td><td valign="top" align="left">';
		            	s += '<span style="color:gray;font-size:8pt;">AirPlay Service</span><br><b>Remote image display reuqest</b>';
		            	s += '</td><td align="right" style="color:lime;font-size:12pt">    </td></tr>';
            			s += '<tr><td colspan="2" align="right"><span style="color:gray;font-size:8pt;">' + date + '</span>';
            			s += '</td></tr></table>';

						var displayid = eventLog.Value;
						var cts = eventLog.UnixTimestamp;
						
						HG.WebApp.Apps.NetPlay.SlideShow.DisplayImage(displayid, cts);

	        		}
	        	}
        	break;
        }
    }
    //
    if (s != '')
    {
        var delay = 1000;
        try { delay = eventLog.UnixTimestamp - ts; } catch (e) { }
        HG.WebApp.Home.ShowEventPopup( s, delay + 500 );
    	popupopen = true;
    }
    //
    try { HG.WebApp.Data.LoggingFrom = eventLog.UnixTimestamp; } catch (e) { }
};
//
HG.WebApp.Home.LoggingPoll = function()
{
//    HG.WebApp.Data.LoggingFrom = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(),  now.getUTCHours(), now.getUTCMinutes(), now.getUTCSeconds(), now.getUTCMilliseconds()));
//    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Logging/Recent.From/' + ts + '/' + (new Date().getTime()), function (data) {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Logging/Recent.Last/5000/' + (new Date().getTime()), function (data) {
        var logs = eval(arguments[2].responseText);
        if ( logs && typeof logs != 'undefined')
        {
        	var popupopen = false;
	        for (var i = 0; i < logs.length; i++) {
				HG.WebApp.Home.PopupEvent(logs[i]);
	        }
	        if (!popupopen) HG.WebApp.Home.ShowEventPopup( '' );
        }
        else
        {
	        HG.WebApp.Home.ShowEventPopup( '' );
            alert(arguments[2].responseText);
        }
    });	
};
//
HG.WebApp.Home.ShowEventPopup = function(html, delay)
{
	var s = html;
	setTimeout(function(){
	    if ( s != '' )
	    {
	        $('#statuspopup').html( s );
		    $('#statuspopup').css('display', '');
		    //$('#statuspopup').animate({ opacity: '0.30'}, 250, function(){
	    	    $('#statuspopup').animate({right:'5px', opacity: '0.70'}, 500);
		    //});
	    }
	    else
	    {
	    	if ($('#statuspopup').css('display') != 'none')
	    	{
	    	    $('#statuspopup').animate({right:'-400px', opacity: '0.0'}, 500, function(){
		    	    $('#statuspopup').css('display', 'none');
	    	    });
	    	}
	    }
    }, delay);
}
//
HG.WebApp.Home.FxAnimateNumber = function(element, targetvalue) 
{
    if (HG.WebApp.Data.CurrentKw == -1)
    {
        HG.WebApp.Data.CurrentKw = targetvalue - 0.020;
    }
    if (HG.WebApp.Data.CurrentKw < targetvalue) {
        HG.WebApp.Data.CurrentKw += 0.001;
        var wh = parseFloat(HG.WebApp.Data.CurrentKw).toFixed(6);
        $('body').find(element).each(function(){ $(this).html('kW ' + (wh.substr(0, wh.length - 3))); }); /* + ' ' + wh.substr(wh.length - 3) */ 
        setTimeout('HG.WebApp.Home.FxAnimateNumber("' + element + '", ' + targetvalue + ')', 100);
    }
};
//
HG.WebApp.Home.UpdateInterfacesStatus = function() 
{
    var ifaceurl = '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.List/' + (new Date().getTime());
	$.ajax({
		url: ifaceurl,
		dataType: 'json',
		success: function (data) {
            var interfaces = eval(data);
            var status = '';
            var isupdateavailable = false;
            //
            if (interfaces && interfaces != 'undefined')
            {
	            for (i = 0; i < interfaces.length; i++) {
	                var domain = interfaces[i].Domain.split('.');
	                var name = domain[1].toUpperCase();
	                var connected = interfaces[i].IsConnected;
	                //status += '<span style="color:' + (connected == 'True' ? 'lime' : 'gray') + ';margin-right:20px">' + name + '</span>';
	                if (interfaces[i].Domain != "HomeGenie.UpdateChecker")
	                {
	                    status += '<img src="images/interfaces/' + name.toLowerCase() + '.png" height="28" width="30" style="' + (connected == 'True' ? 'opacity:1.0' : 'opacity:0.4') + '" vspace="2" hspace="0" />';
	                }
	                else
	                {
	                    isupdateavailable = true;
	                }
	            }
            }
		    //
            if (isupdateavailable)
            {
                status += '<a href="#page_configure_maintenance" data-transition="slide" alt="Update available."><img title="Update available." src="images/update.png" height="28" width="28" style="margin-left:6px" vspace="2" hspace="0" /></a>';
            }
		    //
            $('#interfaces_status').html(status);
		}
	});		
};
//
// namespace : HG.WebApp.Utility namespace
// info      : global utility functions
//
HG.WebApp.Utility = HG.WebApp.Utility || {};
HG.WebApp.Utility.GetElapsedTimeText = function (timestamp)
{
    var ret = "";
    timestamp = (new Date() - timestamp) / 1000;
    if (timestamp > 0)
    {
        var days = Math.floor(timestamp / 86400);
        var hours = Math.floor((timestamp - (days * 86400 )) / 3600);
        var minutes = Math.floor((timestamp - (days * 86400 ) - (hours * 3600 )) / 60);
        var secs = Math.floor((timestamp - (days * 86400 ) - (hours * 3600 ) - (minutes * 60)));
        //
        if (days > 0) ret += days + "d ";
        if (hours > 0) ret += hours + "h ";
        if (minutes > 0) ret += minutes + "m ";
        else if (secs > 0) ret += secs + "s";
    }
    return ret;
};
HG.WebApp.Utility.GetModuleByDomainAddress = function (domain, address) 
{
    var module = null;
    for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
        if (HG.WebApp.Data.Modules[m].Domain == domain && HG.WebApp.Data.Modules[m].Address == address) {
            module = HG.WebApp.Data.Modules[m];
            break;
        }
    }
    return module;
};
HG.WebApp.Utility.GetModuleIndexByDomainAddress = function (domain, address) 
{
    var moduleidx = -1;
    for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
        if (HG.WebApp.Data.Modules[m].Domain == domain && HG.WebApp.Data.Modules[m].Address == address) {
            moduleidx = m;
            break;
        }
    }
    return moduleidx;
};
HG.WebApp.Utility.GetModulePropertyByName = function (module, prop) {
    var value = null;
    if (module.Properties != null) {
        for (var p = 0; p < module.Properties.length; p++) {
            if (module.Properties[p].Name == prop) {
                value = module.Properties[p];
                break;
            }
        }
    }
    return value;
};
HG.WebApp.Utility.SetModulePropertyByName = function (module, prop, value, timestamp) {
    var found = false;
    if (module.Properties != null) {
        for (var p = 0; p < module.Properties.length; p++) {
            if (module.Properties[p].Name == prop) {
                module.Properties[p].LastValue = module.Properties[p].Value;
                module.Properties[p].Value = value;
                if (typeof timestamp != 'undefined')
                {
                    module.Properties[p].UpdateTime = timestamp;
                }
                found = true;
                break;
            }
        }
        if (!found)
        {
            module.Properties.push({ 'Name' : prop, 'Value' : value });
        }
    }
};
	
HG.WebApp.Utility.GetProgramByAddress = function (paddr)
{
	var cp = null;
	for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
		if (HG.WebApp.Data.Programs[i].Address == paddr)
		{
			cp = HG.WebApp.Data.Programs[i];
			break;
		}
	}
	return cp;
};	

HG.WebApp.Utility.GetCommandFromEvent = function (module, event)
{
    var commandobj = null;
    if ((module.DeviceType == 'Switch' || module.DeviceType == 'Light' || module.DeviceType == 'Dimmer' || module.DeviceType == 'Siren' || module.DeviceType == 'Fan' || module.DeviceType == 'Shutter') && event.Property == 'Status.Level')
    {
        var command = 'Control.Level';
        var arg = event.Value;
        if (parseFloat(arg.replace(',', '.')) == 0)
        {
            command = 'Control.Off';
            arg = '';
        }
        else if (parseFloat(arg.replace(',', '.')) == 1)
        {
            command = 'Control.On';
            arg = '';
        }
        commandobj = { 
            'Domain' : module.Domain, 
            'Target' : module.Address, 
            'CommandString' : command, 
            'CommandArguments' : arg 
        };
    }
    else if (module.Domain == 'Controllers.LircRemote' && module.Address == 'IR')
    {
        commandobj = { 
            'Domain' : module.Domain, 
            'Target' : module.Address, 
            'CommandString' : 'Control.IrSend', 
            'CommandArguments' : event.Value 
        };
    }
    return commandobj;
};

HG.WebApp.Utility.FormatDate = function (date)
{
    var dt = $.datepicker.formatDate('D, dd/mm/yy', date);
    return dt; 
};

HG.WebApp.Utility.FormatDateTime = function (date)
{
	var hh = date.getHours().toString(); if (hh.length == 1) hh = '0' + hh;
	var mm = date.getMinutes().toString(); if (mm.length == 1) mm = '0' + mm;
	var ss = date.getSeconds().toString(); if (ss.length == 1) ss = '0' + ss;
	var dt = hh + ':' + mm + ':' + ss; // + '.' + date.getMilliseconds();
	return dt; 
};
	
HG.WebApp.Utility.JScrollToElement = function (element, delay) {
	    $('html, body').animate({
	        scrollTop: $(element).offset().top
	    }, delay);
	};
//
// namespace : HG.WebApp.Utility namespace
// info      : global utility functions
//
HG.WebApp.Locales = HG.WebApp.Locales || {};
HG.WebApp.Locales.Localize = function(langurl)
	{
		// get data via ajax 
		// store it in 		HG.WebApp.Data._CurrentLocale
		// and replace locales strings in the current page
		$.ajax({
		        url: langurl,
		        //dataType: 'json',
		        success: function (data) {
		            HG.WebApp.Data._CurrentLocale = $.parseJSON( data );
					//
					$(document).find('[data-locale-id]').each(function(index){
			    		var stringid = $(this).attr('data-locale-id');
			    		var text = HG.WebApp.Locales.GetLocaleString(stringid);
			    		if (text != null /* && text.trim() != ''*/ ) 
                        {
                            $this = $(this);
                            if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                                $('span.ui-btn-text', $this).text(text);
                            }
                            /*else if( $this.is('input') ) {
                                $this.val(text);
                                // go up the tree
                                var ctx = $this.closest('.ui-btn');
                                $('span.ui-btn-text', ctx).text(text);
                            }*/
                            else
                            {
                                $(this).html(text);
                            }
                        }
					});
		        }
		    });		
	};
HG.WebApp.Locales.GetLocaleString = function(stringid)
	{
		var retval = null;
    	$.each(HG.WebApp.Data._CurrentLocale, function(key, value) {
    		if (key == stringid)
    		{
    			retval = value;
    			return false; // break each
    		}
		});
		return retval;
	}
HG.WebApp.Locales.GenerateTemplate = function()
	{
		//
		var localestring = '';
		$(document).find('[data-locale-id]').each(function(index){
    		var stringid = $(this).attr('data-locale-id');
    		var value = $(this).html().trim();
    		if (localestring.indexOf('"' + stringid + '\"') < 0)
    		{
    			localestring += '\t\"' + stringid + '\": \n\t\t \"' + value + '\",\n';
			}
		});
		alert( localestring );
		//
	};
	