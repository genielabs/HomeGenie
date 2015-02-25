<script type="text/javascript">

/*
// HomeGenie AJAX/WebService API
// Copyright: (c) 2010-2014 GenieLabs
// Author   : Generoso Martello
// E-Mail   : gene@homegenie.it
*/ 


var HG = HG || {};
//
// namespace : HG.Automation
// info      : -
//
{include js/api/homegenie.automation.js}
//
// namespace : HG.Configure
// info      : -
//
{include js/api/homegenie.configure.js}
//
// namespace : HG.Control
// info      : -
//
{include js/api/homegenie.control.js}
//
// namespace : HG.System
// info      : -
//
{include js/api/homegenie.system.js}
//
// namespace : HG.Statistics
// info      : -
//
{include js/api/homegenie.statistics.js}
//
// namespace : HG.WebApp
// info      : -
//
{include js/api/homegenie.webapp.js}		
//
// namespace : HG.VoiceControl
// info      : -
//
{include js/api/homegenie.voicecontrol.js}		
//
//
// namespace : HG.Ext.ZWave
{include ext/zwave/_nodesetup.js}
</script>







<script type="text/javascript">
// TODO: deprecate all js code below, or move it to appropriate place in hg api

// Persist-Js data store 
var dataStore;

function setTheme(theme) {
    dataStore.set('UI.Theme', theme);
    $(document).find('.ui-page')
			.removeClass('ui-page-theme-a ui-page-theme-b ui-page-theme-c ui-page-theme-d ui-page-theme-e ui-page-theme-f ui-page-theme-g ui-page-theme-h')
            .addClass('ui-page-theme-' + theme);
    $(document).find('.ui-mobile-viewport')
            .removeClass('ui-overlay-a ui-overlay-b ui-overlay-c ui-overlay-d ui-overlay-e ui-overlay-f ui-overlay-g ui-overlay-h')
            .addClass('ui-overlay-' + theme);
    $(document).find('.ui-popup')
            .removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g ui-body-h')
            .addClass('ui-body-' + theme);
    $(document).find('.ui-loader')
            .removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g ui-body-h')
            .addClass('ui-body-' + theme);
    return;
}


//////////////////////////////////////////////////////

    
function configurepage_GetModuleIcon(module, callback, elid) {
    var icon = 'pages/control/widgets/homegenie/generic/images/unknown.png';
    if (module != null && module.DeviceType && module.DeviceType != '' && module.DeviceType != 'undefined')
    {
        var widgeticon = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayIcon');
        var widget = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayModule');
        if (widget != null && widget.Value != '')
        {
            widget = widget.Value;
        }
        else
        {
            widget = 'homegenie/generic/' + module.DeviceType.toLowerCase();
        }
        //
        if (widgeticon != null && widgeticon.Value != '')
        {
            icon = widgeticon.Value;
        }
        else if (module.WidgetInstance && module.WidgetInstance != null && module.WidgetInstance != 'undefined')
        {
            icon = module.WidgetInstance.IconImage;
        }
        else
        {
            // get reference to generic type widget 
            HG.WebApp.WidgetsList.GetWidgetIcon(widget, elid, callback);
            return icon;
        }
    }
    if (callback != null) callback(icon, elid);
    return icon;
}

</script>	







