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
    //$.ajaxSetup({
    //    cache: false
    //});
    //
    HG.Configure.LoadData(function(){
        HG.WebApp.Control.RenderMenu();
    });
    //
    HG.WebApp.Home.UpdateHeaderStatus();
    window.setInterval('HG.WebApp.Home.UpdateHeaderStatus();', 10000);
    //
    // Page Before Show: common initialization stuff
    //
    $('[data-role=page]').on('pagebeforeshow', function (event) 
    {
        setTheme(dataStore.get('UI.Theme'));
        //
        if (this.id == "page_analyze") 
        {
            HG.WebApp.Statistics.InitConfiguration();
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
        else if (this.id == 'page_configure_schedulerservice')
        {
            HG.WebApp.Scheduler.LoadScheduling();
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

        // localize UI
        var userLang = HG.WebApp.Locales.GetUserLanguage();
        HG.WebApp.Locales.Localize(document, './locales/' + userLang.toLowerCase().substring(0, 2) + '.json', function(success){
            HG.WebApp.Locales.Localize(document, './locales/' + userLang.toLowerCase().substring(0, 2) + '.programs.json', function(success){
                $('#homegenie_overlay').fadeOut(200);
            });
        });
        HG.VoiceControl.Initialize();

        // apply UI settings
        setTheme(dataStore.get('UI.Theme'));
        if (dataStore.get('UI.EventsHistory'))
        {
            $('#btn_eventshistory_led').show();
        }

        // get HG release info
        HG.System.GetVersion(function(res){
            $('#systemversion').html(res.Version);
        });

        // add css google web fonts
        setTimeout(function(){
            $('head').append('<link href="https://fonts.googleapis.com/css?family=Oxygen:400,700&subset=latin,latin-ext" rel="stylesheet" type="text/css">');
        }, 5000);

    }, 100);
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
            this.ellipse(x, y, r, r).attr({ fill: "r(.5,.9)hsb(" + color.h + ", " + color.s + ", .75)-hsb(" + color.h + ", " + color.s + ", " + color.b + ")", stroke: "none", opacity: 0.8 }),
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
    $('#actionconfirm_popup').enhanceWithin().popup();
    //
    // Side Panel
    var menuPanel = $('body>[data-role="panel"]');
    menuPanel.panel()
        .on('panelopen', function() {
            $('#homegenie_overlay').fadeIn(150);
        })
        .on('panelbeforeclose', function() {
            $('#homegenie_overlay').fadeOut(150);
        })
        .children().first().trigger('create');
    menuPanel.on('click', function() {
        $(this).panel('close');
    });
    // A swipe gesture open up the Side Panel
    $( document ).on('swipeleft swiperight', 'div[data-role=page]', function( e ) {
        if ($('.ui-page-active .ui-popup-active').length == 0)
        if (!$(e.target).is('span'))
        if (!$(e.target).is('pre'))
        if (!$(e.target).is('p'))
        if (!$(e.target).is(':input'))
        if ($(e.target).attr('id') != 'page_automation_editprogram')
        if ($('#page_automation_editprogram').children().find($(e.target)).length == 0)
        if ($(e.target).attr('id') != 'page_widgeteditor_editwidget')
        if ($('#page_widgeteditor_editwidget').children().find($(e.target)).length == 0)
        if ($.mobile.activePage.jqmData('panel') !== 'open') {
            if (e.type === 'swipeleft') {
                $(menuPanel.get(0)).panel('open');
            } else if (e.type === 'swiperight') {
                $(menuPanel.get(1)).panel('open');
            }
        }
    });

    // Cron Event Wizard popup
    HG.Ui.GenerateWidget('core/popup.cronwizard', { parent: $(document).find('body') }, function(handler){
        var element = handler.element;
        element.enhanceWithin().popup();
        HG.Ui.Popup.CronWizard = handler;
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
    var ifaceurl = '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Interfaces.List/';
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
                status += '<a href="#page_configure_maintenance" alt="Update available."><img title="Update available." src="images/update.png" height="28" width="28" style="margin-left:6px" vspace="2" hspace="0" /></a>';
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
// code mirror full screen editor popup
HG.WebApp.Utility._cmFsEditor = null;
HG.WebApp.Utility.EditorPopup = function(name, title, subtitle, content, callback) {
    if (HG.WebApp.Utility._cmFsEditor == null) {
        HG.WebApp.Utility._cmFsEditor = CodeMirror.fromTextArea(document.getElementById('fullscreen_edit_text'), {
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
            mode: { name: "javascript", globalVars: true },
            theme: 'ambiance'
        });    
    }
    var editor = $('#fullscreen_edit_box');
    editor.find('[data-ui-field=title]').html(title);
    editor.find('[data-ui-field=subtitle]').html(subtitle);
    var nameLabel = editor.find('[data-ui-field=namelabel]').html(name);
    var nameInputDiv = editor.find('[data-ui-field=nameinput]');
    var nameInputText = nameInputDiv.find('input').val(name);
    var confirmButton = editor.find('[data-ui-field=confirm]');
    var cancelButton = editor.find('[data-ui-field=cancel]');
    if (name == null || name == '') {
        nameLabel.hide();
        nameInputDiv.show();
    } else {
        nameLabel.show();
        nameInputDiv.hide();
    }
    cancelButton.on('click', function() {
        var response = { 'name': nameInputText.val(), 'text': HG.WebApp.Utility._cmFsEditor.getValue(), 'isCanceled': true };
        cancelButton.off('click');
        confirmButton.off('click');
        $('#fullscreen_edit_box').hide(150);
        callback(response);
    });
    confirmButton.on('click', function() {
        var response = { 'name': nameInputText.val(), 'text': HG.WebApp.Utility._cmFsEditor.getValue(), 'isCanceled': false };
        if (nameInputText.val() == '') {
            nameInputText.qtip({
                content: {
                    text: HG.WebApp.Locales.GetLocaleString('fullscreen_editor_entervalidname', 'Enter a valid name.')
                },
                show: { event: false, ready: true, delay: 200 },
                hide: { event: false, inactive: 2000 },
                style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                position: { my: 'bottom center', at: 'top center' }
            })
            .parent().stop().animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250)
                .animate({ borderColor: "#FF5050" }, 250)
                .animate({ borderColor: "#FFFFFF" }, 250);
        } else {
            cancelButton.off('click');
            confirmButton.off('click');
            $('#fullscreen_edit_box').hide(150);
            callback(response);
        }
    });
    HG.WebApp.Utility._cmFsEditor.setValue(content);
    setTimeout(function(){
        HG.WebApp.Utility._cmFsEditor.refresh();
        HG.WebApp.Utility._cmFsEditor.focus();
        HG.WebApp.Utility._cmFsEditor.setCursor({ line: 0, ch: 0 });
    }, 500);
    $('#fullscreen_edit_box').show(150);
};
HG.WebApp.Utility.ConfirmPopup = function(title, description, callback) {
    var confirmPopup = $('#actionconfirm_popup'); 
    confirmPopup.buttonProceed = $('#actionconfirm_confirm_button');
    confirmPopup.buttonCancel = $('#actionconfirm_cancel_button');
    confirmPopup.find('h3').html(title);
    confirmPopup.find('p').html(description);
    confirmPopup.buttonCancel.focus();
    var canceled = function( event, ui ) {
        callback(false);
    };
    var confirmed = function( event, ui ) {
        callback(true);
    };
    confirmPopup.buttonCancel.on('click', canceled);
    confirmPopup.buttonProceed.on('click', confirmed);
    confirmPopup.on( "popupafterclose", function(){
        confirmPopup.buttonCancel.off('click', canceled);
        confirmPopup.buttonProceed.off('click', confirmed);
    });
    setTimeout(function(){ confirmPopup.popup('open', { transition: 'pop' }); }, 250);
};
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
HG.WebApp.Utility.ParseModuleDomainAddress = function (domainAddress) 
{
    var result = null;
    if (domainAddress.indexOf(':') > 0) {
        result = { 
            Domain: domainAddress.substring(0, domainAddress.lastIndexOf(':')),
            Address: domainAddress.substring(domainAddress.lastIndexOf(':') + 1)
        };
    }
    return result;
},
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
    };
    $(popup_id1).one('popupafterclose', switchfn);
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
    var localeDateParts = testDate.toLocaleDateString().replace(/[\u200E]/g, "").split('/');
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
            $.ajax({
                url: './locales/en.programs.json',
                type: 'GET',
                success: function (pdata) {
                    HG.WebApp.Data._DefaultLocale = $.extend(HG.WebApp.Data._DefaultLocale, $.parseJSON( pdata ));
                }
            });     
        }
    });  
};
HG.WebApp.Locales.Localize = function(container, langurl, callback)
{
    // get data via ajax 
    // store it in 		HG.WebApp.Data._CurrentLocale
    // and replace locales strings in the current page
    HG.WebApp.Locales.GetDefault(function(){
        $.ajax({
            url: langurl,
            type: 'GET',
            success: function (data) {
                HG.WebApp.Data._CurrentLocale = $.extend(HG.WebApp.Data._CurrentLocale, $.parseJSON( data ));
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
                if (typeof callback == 'function') callback(true);
            },
            error: function(xhr, status, error) {
                console.log('LOCALIZATION ERROR: '+xhr.status+':'+xhr.statusText);
                if (typeof callback == 'function') callback(false);
            }
        });
    });		
};
HG.WebApp.Locales.LocalizeElement = function(elementId, locale) {
    $(elementId).find('[data-locale-id]').each(function(index){
        var stringid = $(this).attr('data-locale-id');
        var text = HG.WebApp.Locales.GetLocaleString(stringid, false, locale);
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
    
};
HG.WebApp.Locales.GetLocaleString = function(stringid, defaultValue, locale)
{
    var retval = null;
    // try user provided locale
    if (locale)
    $.each(locale, function(key, value) {
        if (key == stringid)
        {
            retval = value;
            return false; // break each
        }
    });
    // try current locale
    if (retval == null)
    $.each(HG.WebApp.Data._CurrentLocale, function(key, value) {
        if (key == stringid)
        {
            retval = value;
            return false; // break each
        }
    });
    // fallback to default locale
    if (retval == null)
    $.each(HG.WebApp.Data._DefaultLocale, function(key, value) {
        if (key == stringid)
        {
            retval = value;
            return false; // break each
        }
    });
    if (retval == null)
        console.log('LOCALIZATION ERROR "' + stringid + '" NOT FOUND!!!'); 
    return (retval == null && defaultValue ? defaultValue : retval);
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
                var text = HG.WebApp.Locales.GetLocaleString(stringid, false, locale);
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
                var text = HG.WebApp.Locales.GetLocaleString(stringid, false, locale);
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
                    var text = HG.WebApp.Locales.GetLocaleString(stringid, false, locale);
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
HG.WebApp.Locales.GetWidgetLocaleString = function(widget, stringid, defaultValue) {
    var retval = null;
    if(typeof(widget.data("Locale")) == "undefined")
        return (defaultValue ? defaultValue : null);
    retval = HG.WebApp.Locales.GetLocaleString(stringid, false, widget.data("Locale"));
    return (retval == null && defaultValue ? defaultValue : retval);
};
HG.WebApp.Locales.GetProgramLocaleString = function(programAddress, stringId, defaultValue) {
    var response = defaultValue;
    var plocale;
    var hasLocale = eval('(HG.WebApp.Data._CurrentLocale.Programs && HG.WebApp.Data._CurrentLocale.Programs['+programAddress+'])');
    if (hasLocale)
        plocale = eval('HG.WebApp.Data._CurrentLocale.Programs['+programAddress+']');
    else {
        hasLocale = eval('(HG.WebApp.Data._DefaultLocale.Programs && HG.WebApp.Data._DefaultLocale.Programs['+programAddress+'])');
        if (hasLocale)
            plocale = eval('HG.WebApp.Data._DefaultLocale.Programs['+programAddress+']');
    }
    if (typeof plocale != 'undefined') {
        response = HG.WebApp.Locales.GetLocaleString(stringId, defaultValue, plocale);
    }
    return response;
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
	
