//
// namespace : HG.WebApp namespace
// info      : -
//
HG.WebApp = HG.WebApp || new function(){ var $$ = this;

    $$.Data = {

        ServiceKey: 'api',
        ServiceDomain: 'HomeAutomation.HomeGenie',
        Modules: [],
        Groups: [],
        AutomationGroups: [],
        Programs: [],
        Interfaces: [],
        Events: []

    };

    $$.Initialize = function() {
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
            $$.Control.RenderMenu();
        });
        //
        $$.Home.UpdateHeaderStatus();
        window.setInterval('HG.WebApp.Home.UpdateHeaderStatus();', 10000);
        //
        // Page Before Show: common initialization stuff
        //
        $('[data-role=page]').on('pagebeforeshow', function (event) {
            HG.Ui.SetTheme(dataStore.get('UI.Theme'));
            if (this.id == "page_analyze") {
                $$.Statistics.InitConfiguration();
            } else if (this.id == 'page_configure_maintenance') {
                $$.Maintenance.LoadSettings();
            } else if (this.id == 'page_configure_groups') {
                HG.Configure.Modules.List(function (data) {
                    try
                    {
                        $$.Data.Modules = eval(data);
                    } catch (e) { }
                    HG.Automation.Programs.List(function () {
                        $$.GroupsList.LoadGroups();
                    });
                });
            } else if (this.id == 'page_configure_schedulerservice') {
                $$.Scheduler.LoadScheduling();
            }
        });
        //
        // Page events - Close - Cleanup stuff
        //
        $('[data-role=page]').on('pagehide', function (event) {
            if (this.id == "page_analyze") {
                $$.Statistics.SetAutoRefresh( false );
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
            var userLang = $$.Locales.GetUserLanguage();
            $$.Locales.Load('./locales/' + userLang.toLowerCase().substring(0, 2) + '.json', function(success){
                $$.Locales.Load('./locales/' + userLang.toLowerCase().substring(0, 2) + '.programs.json', function(success){
                    $$.Locales.Localize(document);
                    $('#homegenie_overlay').fadeOut(200);
                    // Show about popup
                    if (!dataStore.get('UI.AboutPopupShown')) {
                        dataStore.set('UI.AboutPopupShown', true);
                        setTimeout($$.Home.About, 3000);
                    }
                });
            });
            HG.VoiceControl.Initialize();

            // apply UI settings
            HG.Ui.SetTheme(dataStore.get('UI.Theme'));
            if (dataStore.get('UI.EventsHistory')) {
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
        //
        // Global Popups
        //
        $('#automation_group_module_edit').enhanceWithin().popup();
        $('#page_configure_groupmodules_propspopup').enhanceWithin().popup();
        $('#page_configure_groupmodules_propspopup').on('popupbeforeposition', function (event) {
            $('#automation_group_module_params').scrollTop(0);
            $$.GroupModules.LoadModuleParameters();
        });
        $('#module_options_button').on('click', function (event) {
            $$.GroupModules.ShowModuleOptions($$.GroupModules.CurrentModule.Domain, $$.GroupModules.CurrentModule.Address);
        });
        $('#module_update_button').bind('click', function (event) {
            $$.GroupModules.CurrentModule.Name = $$.GroupModules.EditModule.Name;
            $$.GroupModules.CurrentModule.DeviceType = $$.GroupModules.EditModule.DeviceType;
            $$.Utility.SetModulePropertyByName($$.GroupModules.CurrentModule, 'VirtualMeter.Watts', $$.GroupModules.EditModule.WMWatts);
            //TODO: find out why it's not setting NeedsUpdate flag to true for VirtualMeter.Watts
            $$.Utility.SetModulePropertyByName($$.GroupModules.EditModule, 'VirtualMeter.Watts', $$.GroupModules.EditModule.WMWatts);
            //
            for (var p = 0; p < $$.GroupModules.EditModule.Properties.length; p++) {
                var prop = $$.GroupModules.EditModule.Properties[p];
                $$.Utility.SetModulePropertyByName($$.GroupModules.CurrentModule, prop.Name, prop.Value);
                prop = $$.Utility.GetModulePropertyByName($$.GroupModules.CurrentModule, prop.Name);
                prop.NeedsUpdate = 'true';
            }
            //
            $$.GroupModules.UpdateModule($$.GroupModules.CurrentModule, function () {
                $$.GroupModules.ModuleUpdatedCallback();
            });
        });
        //
        $('#automationprograms_program_options').enhanceWithin().popup();
        $('#configure_popupsettings_edit').enhanceWithin().popup();
        $('#configure_popupsettings_edit').on('popupbeforeposition', function(){
            $$.Events.PopupRefreshIgnore();
        });
        $('#actionconfirm_popup').enhanceWithin().popup();
        //
        // Side Panel
        //
        var menuPanel = $('body>[data-role="panel"]');
        menuPanel.panel()
            .on('panelopen', function() {
                $('#homegenie_overlay').fadeIn(150);
            })
            .on('panelbeforeopen', function() {
                $('#homegenie_overlay').fadeIn(250);
            })
            .on('panelbeforeclose', function() {
                $('#homegenie_overlay').fadeOut(250);
            })
            .children().first().trigger('create');
        menuPanel.on('click', function() {
            $(this).panel('close');
        });
        //
        // A swipe gesture open up the Side Panel
        //
        $( document ).on('swipeleft swiperight', 'div[data-role=page]', function( e ) {
            if (!$('[data-ui-field=homegenie_panel_button]').hasClass('ui-disabled'))
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
        //
        // Cron Event Wizard popup
        //
        HG.Ui.GenerateWidget('core/popup.cronwizard', { parent: $(document).find('body') }, function(handler){
            var element = handler.element;
            element.enhanceWithin().popup();
            HG.Ui.Popup.CronWizard = handler;
        });

        $('#homegenie_about').enhanceWithin().popup();
        $('#about_popup_updatebutton').on('click', function(){
            HG.System.UpdateManager.UpdateCheck();
            $('#homegenie_about').popup('close');
        });
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