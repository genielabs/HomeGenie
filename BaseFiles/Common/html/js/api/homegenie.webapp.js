HG.WebApp = HG.WebApp || {};
//
HG.WebApp.Data = HG.WebApp.Data || {};
//
HG.WebApp.Data.Interfaces = Array();
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
HG.WebApp.Data._CurrentGroupIndex = 0;
//
HG.WebApp.Data._DefaultLocale = {};
HG.WebApp.Data._CurrentLocale = {};
//
// Code Mirror editor instances (TODO: refactor these global vars to a better name)
var editor1 = null;
var editor2 = null;
var editor3 = null;
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
    dataStore = $.jStorage;
    //
    var theme = dataStore.get('UI.Theme');
    if (theme == null || (theme < 'a' || theme > 'g')) {
        dataStore.set('UI.Theme', 'a');
    }
    //
    $.mobile.ajaxFormsEnabled = false;
    $.ajaxSetup({
        cache: false
    });
    //
    HG.Configure.LoadData();
    //
    HG.WebApp.Home.UpdateHeaderStatus();
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
        setTheme(dataStore.get('UI.Theme'));
        //
        if (this.id == "page_control") // && HG.WebApp.Control._RefreshIntervalObject == null) 
        {
            // init "Control" page
            $.mobile.loading('show');
            HG.Configure.Groups.List('Control', function () 
            {
                if ($('#control_groupslist').children().length == 0) 
                {
                    HG.WebApp.Control.RenderGroups();
                }
                HG.WebApp.Control.UpdateModules();
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
            HG.WebApp.Statistics.InitConfiguration();
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
            $.mobile.loading('show');
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
        else if (this.id == 'page_automation_editprogram') 
        {	            

            $('#automation_program_scriptcondition').next().css('display', '');
            $('#automation_program_scriptsource').next().css('display', '');
            
            HG.WebApp.ProgramEdit.SetTab(1);
            HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
            //if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors.trim() != '' && HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors.trim() != '[]')
            //{
            //    HG.WebApp.ProgramEdit.ShowProgramErrors(HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors);
            //}

            automationpage_ConditionsRefresh();                                                    
            automationpage_CommandsRefresh();                                                   
        }
    });
    //
    // Page events - Close - Cleanup stuff
    //
    $('[data-role=page]').on('pagehide', function (event) {
        if (this.id == "page_analyze") 
        {
            HG.WebApp.Statistics.SetAutoRefresh( false );
        }
    });
    //
    // Prevent body scrolling when a popup is open
    //
    //$(document).on('popupafteropen', '[data-role="popup"]' ,function( event, ui ) {
    //    $('body').css('overflow-y','hidden');
    //}).on('popupafterclose', '[data-role="popup"]' ,function( event, ui ) {
    //    $('body').css('overflow-y','auto');
    //});
    //$(document).on('popupafteropen', '[data-ui-field="controlpopup"]', function (event, ui) {
    //    $('body').css('overflow-y', 'hidden');
    //}).on('popupafterclose', '[data-ui-field="controlpopup"]', function (event, ui) {
    //    $('body').css('overflow-y', 'auto');
    //});
    //
    // UI Localization
    //
    setTimeout(function() {

        var userLang = HG.WebApp.Locales.GetUserLanguage();
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
        //
        // Apply UI settings
        setTheme(dataStore.get('UI.Theme'));
        if (dataStore.get('UI.EventsHistory'))
        {
            $('#btn_eventshistory_led').show();
        }
        //
        // add css google web fonts
        $('head').append('<link href="http://fonts.googleapis.com/css?family=Oxygen:400,700&subset=latin,latin-ext" rel="stylesheet" type="text/css">');

    }, 100);
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
    editor3 = CodeMirror.fromTextArea(document.getElementById('automation_program_sketchfile'), {
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        extraKeys: {
            "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
            "Ctrl-Space": "autocomplete"
        },
        foldGutter: true,
        gutters: ["CodeMirror-lint-markers-3", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        highlightSelectionMatches: { showToken: /\w/ },
        mode: { globalVars: true },
        theme: 'ambiance'
    });
    $(editor3.getWrapperElement()).hide();
    //
    editor4 = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_html'), {
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        extraKeys: {
            "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
            "Ctrl-Space": "autocomplete"
        },
        foldGutter: true,
        gutters: ["CodeMirror-lint-markers-4", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        highlightSelectionMatches: { showToken: /\w/ },
        mode: "text/html",
        theme: 'ambiance'
    });
    editor5 = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_javascript'), {
        lineNumbers: true,
        matchBrackets: true,
        autoCloseBrackets: true,
        extraKeys: {
            "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
            "Ctrl-Space": "autocomplete"
        },
        foldGutter: true,
        gutters: ["CodeMirror-lint-markers-5", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        highlightSelectionMatches: { showToken: /\w/ },
        mode: "text/javascript",
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
    // Global Popups
    $( "#automation_group_module_edit" ).enhanceWithin().popup();
    $('#module_update_button').bind('click', function (event) {
        HG.WebApp.GroupModules.CurrentModule.Name = HG.WebApp.GroupModules.EditModule.Name;
        HG.WebApp.GroupModules.CurrentModule.DeviceType = HG.WebApp.GroupModules.EditModule.DeviceType;
        HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, 'VirtualMeter.Watts', HG.WebApp.GroupModules.EditModule.WMWatts);
        //TODO: find out why it's not setting NeedsUpdate flag to true for VirtualMeter.Watts
        HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.EditModule, 'VirtualMeter.Watts', HG.WebApp.GroupModules.EditModule.WMWatts);
        //
        for (var p = 0; p < HG.WebApp.GroupModules.EditModule.Properties.length; p++) {
            var prop = HG.WebApp.GroupModules.EditModule.Properties[p];
            HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, prop.Name, prop.Value);
            prop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, prop.Name);
            prop.NeedsUpdate = 'true';
        }
        //
        HG.WebApp.GroupModules.UpdateModule(HG.WebApp.GroupModules.CurrentModule, function () {
            HG.WebApp.GroupModules.ModuleUpdatedCallback();
        });
    });
    $('#module_remove_button').bind('click', function (event) {
        HG.WebApp.GroupModules.DeleteGroupModule(HG.WebApp.GroupModules.CurrentGroup, HG.WebApp.GroupModules.CurrentModule);
        HG.WebApp.GroupsList.SaveGroups(null);
    });
    //
    $('#automationprograms_program_options').enhanceWithin().popup();
    $('#configure_popupsettings_edit').enhanceWithin().popup();
    $('#configure_popupsettings_edit').on('popupbeforeposition', function(){
        HG.WebApp.Events.PopupRefreshIgnore();
    });

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
// namespace : HG.WebApp.WidgetEditor 
// info      : -
//
{include pages/configure/widgeteditor/_widgetslist.js} 
{include pages/configure/widgeteditor/_widgetedit.js} 
//
// namespace : HG.WebApp.Scheduler 
// info      : -
//
{include pages/configure/scheduler/_scheduler.js}	
//
// namespace : HG.WebApp.Apps.NetPlEditCurrentModuleay.SlideShow 
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
        type: 'GET',
        success: function (data) {
            var interfaces = HG.WebApp.Data.Interfaces = eval(data);
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
    var endianType = HG.WebApp.Locales.GetDateEndianType();
    var dt = null;
    if (endianType == 'M')
        dt = $.datepicker.formatDate('D, mm/dd/yy', date);
    else
        dt = $.datepicker.formatDate('D, dd/mm/yy', date);
    return dt; 
};

HG.WebApp.Utility.FormatDateTime = function (date, showms)
{
    var endianType = HG.WebApp.Locales.GetDateEndianType();
    var dt = null;
    var h = date.getHours();
    var mm = date.getMinutes().toString(); if (mm.length == 1) mm = '0' + mm;
    var ss = date.getSeconds().toString(); if (ss.length == 1) ss = '0' + ss;
    if (endianType == 'M')
    {
        var ampm = (h >= 12 ? 'PM' : 'AM');
        h = h % 12; h = (h ? h : 12);
        dt = h + ':' + mm + ':' + ss + (showms ? '.' + date.getMilliseconds() : '') + ' ' + ampm;
    }
    else
    {
        dt = h + ':' + mm + ':' + ss + (showms ? '.' + date.getMilliseconds() : '');
    }
    return dt; 
};
	
HG.WebApp.Utility.JScrollToElement = function (element, delay) {
    $('html, body').animate({
        scrollTop: $(element).offset().top
    }, delay);
};

HG.WebApp.Utility.SwitchPopup = function(popup_id1, popup_id2, notransition) {
    var switchfn = function( event, ui ) {
        if (notransition == true)
        {
            setTimeout(function () { $(popup_id2).popup('open'); }, 10);
        }
        else	
        {
            setTimeout(function () { $(popup_id2).popup('open', { transition: 'pop' }); }, 100);
        }
        $(popup_id1).off('popupafterclose', switchfn);
    };
    $(popup_id1).on('popupafterclose', switchfn);
    $(popup_id1).popup('close');
};
//
// namespace : HG.WebApp.Utility namespace
// info      : global utility functions
//
HG.WebApp.Locales = HG.WebApp.Locales || {};
HG.WebApp.Locales.GetUserLanguage = function()
{
    var userLang = (navigator.languages ? navigator.languages[0] : (navigator.language || navigator.userLanguage));
    if (userLang.length > 2) userLang = userLang.substring(0, 2);
    return userLang;
};
HG.WebApp.Locales.GetDateEndianType = function()
{
    // L = Little Endian -> DMY
    // M = Middle Endian -> MDY
    var endianType = 'L';
    var testDate = new Date(98326800000);
    var localeDateParts = testDate.toLocaleDateString().split('/');
    if (localeDateParts[0] == '2') endianType = 'M';
    return endianType;
};
HG.WebApp.Locales.GetDefault = function(callback) {
    $.ajax({
        url: './locales/en.json',
        type: 'GET',
        success: function (data) {
            HG.WebApp.Data._DefaultLocale = $.parseJSON( data );
            callback();
        }
    });     
};
HG.WebApp.Locales.Localize = function(container, langurl)
{
    // get data via ajax 
    // store it in 		HG.WebApp.Data._CurrentLocale
    // and replace locales strings in the current page
    HG.WebApp.Locales.GetDefault(function(){
        $.ajax({
            url: langurl,
            type: 'GET',
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
                        else if( $this.is('input') ) {
                            $this.attr("placeholder", text);
                        }
                        else {
                            $(this).html(text);
                        }
                    }
                });
            }
        });
    });		
};
HG.WebApp.Locales.LocalizeWidget = function(widgetpath, elementid) {
    var userLang = HG.WebApp.Locales.GetUserLanguage();
    widgetpath = widgetpath.substring(0, widgetpath.lastIndexOf('/'));
    var container = '#' + elementid;
    var langurl = 'pages/control/widgets/' + widgetpath + '/locales/' + userLang.toLowerCase().substring(0, 2) + '.json';
    $.ajax({
        url: langurl,
        type: 'GET',
        success: function (data) {
            var locale = $.parseJSON( data );
            $(container).find('[data-ui-field=widget]').data('Locale', locale);

            $(container).find('[data-locale-id]').each(function(index){
                var stringid = $(this).attr('data-locale-id');
                var text = HG.WebApp.Locales.FindLocaleString(locale, stringid);
                if (text != null) {
                    $this = $(this);
                    if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                        $('span.ui-btn-text', $this).text(text);
                    }
                    else if( $this.is('input') ) {
                        $this.attr("placeholder", text);
                    }
                    else {
                        $(this).html(text);
                    }
                }
            });
            // localizable strings
            $(container).find('[data-localizable]').each(function(index){
                var stringid = $(this).text();
                var text = HG.WebApp.Locales.FindLocaleString(locale, stringid);
                if (text != null) {                    
                    $(this).text(text);                    
                }
            });
            // try to localize widget's popups if they were already processed by jQuery popup() function
            var popups = $(container).find('[data-ui-field=widget]').data('ControlPopUp');
            if (popups)
            popups.each(function (index) {
                var popup = $(this);
                $(popup).find('[data-locale-id]').each(function(index){
                    var stringid = $(this).attr('data-locale-id');
                    var text = HG.WebApp.Locales.FindLocaleString(locale, stringid);
                    if (text != null) {
                        $this = $(this);
                        if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                            $('span.ui-btn-text', $this).text(text);
                        }
                        else if( $this.is('input') ) {
                            $this.attr("placeholder", text);
                        }
                        else {
                            $(this).html(text);
                        }
                    }
                });
            });
        }
    });
};
HG.WebApp.Locales.FindLocaleString = function(locale, stringid) {
    var text = null;
    $.each(locale, function(key, value) {
        if (key == stringid)
        {
            text = value;
            return false; // break each
        }
    });
    if (text == null)
    {
        console.log("WIDGET LOCALIZATION ERROR " + stringid + ' == ' + text + '!!!');
    }
    return text;
};
HG.WebApp.Locales.GetWidgetLocaleString = function(widget, stringId, defaultValue) {
    var retval = null;
    if(typeof(widget.data("Locale")) == "undefined")
        return (defaultValue ? defaultValue : null);
    retval = HG.WebApp.Locales.FindLocaleString(widget.data("Locale"), stringId);
    return (retval == null && defaultValue ? defaultValue : retval);
};
HG.WebApp.Locales.GetLocaleString = function(stringid, defaultValue)
{
    var retval = null;
    $.each(HG.WebApp.Data._CurrentLocale, function(key, value) {
        if (key == stringid)
        {
            retval = value;
            return false; // break each
        }
    });
    if (retval == null)
    {
        $.each(HG.WebApp.Data._DefaultLocale, function(key, value) {
            if (key == stringid)
            {
                retval = value;
                return false; // break each
            }
        });
        if (retval == null)
        {
            console.log("LOCALIZATION ERROR " + stringid + ' == ' + retval + '!!!'); 
        }
    }
    return (retval == null && defaultValue ? defaultValue : retval);
};
HG.WebApp.Locales.GenerateTemplate = function()
{
    var localestring = '';
    $(document).find('[data-locale-id]').each(function(index){
        var stringid = $(this).attr('data-locale-id');
        var value = $(this).html().trim();
        if (localestring.indexOf('"' + stringid + '\"') < 0)
        {
            localestring += '\t\"' + stringid + '\": \n\t\t \"' + value + '\",\n';
        }
    });
    console.log( localestring );
};
	