HG.WebApp = HG.WebApp || {};
//
HG.WebApp.Data = HG.WebApp.Data || {};
//
HG.WebApp.Data.Events = Array();
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
// Code Mirror editor instances (TODO: refactor these global vars to a better name)
var editor1 = null;
var editor2 = null;
//
// Speech Recognition objects
var recognition = null;
var final_transcript = '';
//
//
HG.WebApp.InitializePage = function ()
{
    //
    // Application start - Init stuff
    //
    var t = sessvars.UserSettings.UiTheme;
    if (t < 'a' || t > 'g') {

        sessvars.UserSettings.UiTheme = 'a';
    }
    //
    $.mobile.ajaxFormsEnabled = false;
    $.ajaxSetup({
        cache: false //,
        //contentType: 'application/x-www-form-urlencoded; charset=ISO-8859-1',
        //beforeSend: function(jqXHR) {
        //    jqXHR.overrideMimeType('application/x-www-form-urlencoded; charset=ISO-8859-1');
        //}
    });
    //
    HG.Configure.LoadData();
    //
    window.setInterval('HG.WebApp.Home.UpdateHeaderStatus();', 10000);
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
    // Page Events: Open - Initialize stuff
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
        else if (this.id == 'page_events')
        {
            HG.WebApp.Events.Refresh();
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
    });
    //
    // Page events - Close - Cleanup stuff
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
    // Prevent body scrolling when a popup is open
    //
    $(document).on('popupafteropen', '[data-role="popup"]' ,function( event, ui ) {
        $('body').css('overflow-y','hidden');
    }).on('popupafterclose', '[data-role="popup"]' ,function( event, ui ) {
        $('body').css('overflow-y','auto');
    });
    $(document).on('popupafteropen', '[data-ui-field="controlpopup"]', function (event, ui) {
        $('body').css('overflow-y', 'hidden');
    }).on('popupafterclose', '[data-ui-field="controlpopup"]', function (event, ui) {
        $('body').css('overflow-y', 'auto');
    });
    //
    // UI Localization
    //
    setTimeout(function(){
        var userLang = (navigator.language) ? navigator.language : navigator.userLanguage;
        HG.WebApp.Locales.Localize(document, './locales/' + userLang.toLowerCase().substring(0, 2) + '.json');
        // enable/disable speech input
        if (!('webkitSpeechRecognition' in window)) {
            //no speech support
            $('#speechinput').hide();
        } else {
            // lookup for a localized version, if any
            HG.VoiceControl.Localize('./locales/' + userLang.toLowerCase().substring(0, 2) + '.lingo.json', function(success) {
                if (!success)
                {
                    // fallback to english lingo file
                    HG.VoiceControl.Localize('./locales/en.lingo.json', function(res){ });
                }
            });
            recognition = new webkitSpeechRecognition();
            recognition.continuous = false;
            recognition.interimResults = false;
            recognition.onstart = function() { 
                $('#voicerecognition_button').addClass('ui-disabled');
            }
            recognition.onresult = function(event) { 
                var interim_transcript = '';
                if (typeof(event.results) == 'undefined') {
                    $('#speechinput').hide();
                    recognition.onend = null;
                    recognition.stop();
                    return;
                }
                for (var i = event.resultIndex; i < event.results.length; ++i) {
                    if (event.results[i].isFinal) {
                        final_transcript += event.results[i][0].transcript;
                    } else {
                        interim_transcript += event.results[i][0].transcript;
                    }
                }
            }
            recognition.onerror = function(event) { 
                $('#voicerecognition_button').removeClass('ui-disabled');
                alert('Voice Recognition Error: ' + event); 
            }
            recognition.onend = function() { 
                $('#voicerecognition_text').val(final_transcript);
                $('#voicerecognition_button').removeClass('ui-disabled');
                HG.VoiceControl.InterpretInput(final_transcript);
                final_transcript = '';
            }
        }
        setTheme(sessvars.UserSettings.UiTheme);
    }, 1000);
    //
    // Code Mirror and other UI widgets
    //
    editor1 = CodeMirror.fromTextArea(document.getElementById('automation_program_scriptcondition'), {
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        extraKeys: {
            "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
            "Ctrl-Space": "autocomplete"
        },
        foldGutter: true,
        gutters: ["CodeMirror-lint-markers-1", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        highlightSelectionMatches: { showToken: /\w/ },
        mode: { globalVars: true },
        theme: 'ambiance'
    });
    editor2 = CodeMirror.fromTextArea(document.getElementById('automation_program_scriptsource'), {
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        extraKeys: {
            "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
            "Ctrl-Space": "autocomplete"
        },
        foldGutter: true,
        gutters: ["CodeMirror-lint-markers-2", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        highlightSelectionMatches: { showToken: /\w/ },
        mode: { globalVars: true },
        theme: 'ambiance'
    });
    //
    // stacked message popups
    //
    $('#content').notify({
        speed: 500,
        expires: 5000,
        stack: 'below' // 'above'
    });
    //
    // Raphael graphics - this is used by Color Light widget (this shouldn't be here though...)
    //
    Raphael.fn.ball = function (x, y, r, color) {
        return this.set(
            this.ellipse(x, y + r - r / 5, r, r / 2).attr({ fill: "rhsb(" + color.h + ", 1, .25)-hsb(" + color.h + ", 1, .25)", stroke: "none", opacity: 0 }),
            this.ellipse(x, y, r, r).attr({ fill: "r(.5,.9)hsb(" + color.h + ", " + color.s + ", .75)-hsb(" + color.h + ", " + color.s + ", " + color.v + ")", stroke: "none", opacity: 0.8 }),
            this.ellipse(x, y, r - r / 5, r - r / 20).attr({ stroke: "none", fill: "r(.5,.1)#ccc-#ccc", opacity: 0 })
        );
    };

};


//
// namespace : HG.WebApp.Events 
// info      : -
//
{include pages/events/_events.js}	
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

HG.WebApp.Utility.FormatDateTime = function (date, showms)
{
    var hh = date.getHours().toString(); if (hh.length == 1) hh = '0' + hh;
    var mm = date.getMinutes().toString(); if (mm.length == 1) mm = '0' + mm;
    var ss = date.getSeconds().toString(); if (ss.length == 1) ss = '0' + ss;
    var dt = hh + ':' + mm + ':' + ss;
    if (showms) dt += '.' + date.getMilliseconds();
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
HG.WebApp.Locales.Localize = function(container, langurl)
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
            $(container).find('[data-locale-id]').each(function(index){
                var stringid = $(this).attr('data-locale-id');
                var text = HG.WebApp.Locales.GetLocaleString(stringid);
                if (text != null) {
                    $this = $(this);
                    if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                        $('span.ui-btn-text', $this).text(text);
                    }
                    else {
                        $(this).html(text);
                    }
                }
            });
        }
    });		
};
HG.WebApp.Locales.LocalizeWidget = function(widgetpath, elementid) {
    var userLang = (navigator.language) ? navigator.language : navigator.userLanguage;
    widgetpath = widgetpath.substring(0, widgetpath.lastIndexOf('/'));
    HG.WebApp.Locales.Localize('#' + elementid, 'pages/control/widgets/' + widgetpath + '/locales/' + userLang.toLowerCase().substring(0, 2) + '.json');
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
	