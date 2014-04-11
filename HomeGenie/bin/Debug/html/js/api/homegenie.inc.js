<script type="text/javascript">

/*
// HomeGenie AJAX/WebService API
// Copyright: (c) 2010-2013 GenieLabs
// Author   : Generoso Martello
// E-Mail   : generoso@martello.com
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

    var uitheme = 'a'; // TODO: move this code as part of HG js api

    sessvars.UserSettings = sessvars.UserSettings || { UiTheme: uitheme };

    function setTheme(theme) {
        sessvars.UserSettings.UiTheme = uitheme = theme;
        //
        $.mobile.activePage.find('.ui-li')
                    .removeClass('ui-btn-up-a ui-btn-up-b ui-btn-up-c ui-btn-up-d ui-btn-up-e ui-btn-up-f ui-btn-up-g ui-btn-hover-a ui-btn-hover-b ui-btn-hover-c ui-btn-hover-d ui-btn-hover-e ui-btn-hover-f ui-btn-hover-g')
                    .addClass('ui-btn-up-' + theme).attr('data-theme', theme);

        $.mobile.activePage.find('.ui-btn').not('.ui-li-divider')
                    .removeClass('ui-btn-up-a ui-btn-up-b ui-btn-up-c ui-btn-up-d ui-btn-up-e ui-btn-up-f ui-btn-up-g ui-btn-hover-a ui-btn-hover-b ui-btn-hover-c ui-btn-hover-d ui-btn-hover-e ui-btn-hover-f ui-btn-hover-g')
                    .addClass('ui-btn-up-' + theme)
                    .attr('data-theme', theme);

        $.mobile.activePage.find('.ui-header, .ui-footer, .ui-listview-filter')
                    .removeClass('ui-bar-a ui-bar-b ui-bar-c ui-bar-d ui-bar-e ui-bar-f ui-bar-g')
                    .addClass('ui-bar-' + theme)
                    .attr('data-theme', theme);

        $(document).find('.ui-input-text, .ui-input-search, .ui-popup, .ui-dialog, .ui-page').removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g')
                    .addClass('ui-body-' + theme)
                    .attr('data-theme', theme);

        $.mobile.activePage.find('.ui-popup').removeClass('ui-body-a ui-body-b ui-body-c ui-body-d ui-body-e ui-body-f ui-body-g')
                    .addClass('ui-body-' + theme)
                    .attr('data-theme', theme);

        $.mobile.activePage.find('.ui-li-divider').each(function (index, obj) {
            //if ($(this).parent().attr('data-divider-theme') == 'undefined') {
            $(this).removeClass('ui-bar-a ui-bar-b ui-bar-c ui-bar-d ui-bar-e ui-bar-f ui-bar-g')
                .removeClass('ui-btn-up-a ui-btn-up-b ui-btn-up-c ui-btn-up-d ui-btn-up-e ui-btn-up-f ui-btn-up-g ui-btn-hover-a ui-btn-hover-b ui-btn-hover-c ui-btn-hover-d ui-btn-hover-e ui-btn-hover-f ui-btn-hover-g')
                .addClass('ui-bar-a')
                .attr('data-theme', 'a');
            //}
        })

        if ($.mobile.activePage.attr('id') == 'page_control') {
            $.mobile.activePage.find('a[data-ui-field="widget"]').each(function (index, obj) {
                $(this).parent().parent().removeClass('ui-btn-up-' + theme).addClass('ui-btn-up-z').attr('data-theme', 'z');;
                $(this).parent().parent().next().removeClass('ui-btn-up-' + theme).addClass('ui-btn-up-z').attr('data-theme', 'z');;
            });
        }

    }


//////////////////////////////////////////////////////

    
	function configurepage_GetModuleIcon(module, callback, elid) {
        var icon = 'pages/control/widgets/homegenie/generic/images/unknown.png';
        if (module != null && module.DeviceType && module.DeviceType != '' && module.DeviceType != 'undefined')
        {
            var widget = 'homegenie/generic/' + module.DeviceType.toLowerCase();
            if (module.WidgetInstance && module.WidgetInstance != null && module.WidgetInstance != 'undefined')
            {
                icon = module.WidgetInstance.IconImage;
            }
            else
            {
                // get reference to generic type widget 
	    	    HG.WebApp.Control.GetWidget(widget, function (widgetobject) {
                    if (widgetobject != null)
                    {
                        //widgetobject.Instance.RenderView( null, module );
	                    icon = widgetobject.Instance.IconImage;
                        if (callback != null) callback(icon, elid);
                    }
                });
                return icon;
            }
        }
        if (callback != null) callback(icon, elid);
        return icon;
	}


	function configurepage_GetModuleStatus(module) {
		var type = (module.Type + '').toLowerCase();
		var text = '';
		//
        var watts = null;
        var level = null;
        var temp = null;
        var customdata = '';
        //
        var type = module.DeviceType + '';
        type = type.toLowerCase();
        //
        if (module.Properties != null) {

            for (p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name == "Meter.Watts") {
                    watts = Math.round(module.Properties[p].Value.replace(',', '.')) / 1000;
                }
                else if (module.Properties[p].Name == "Status.Level") {
                    level = (module.Properties[p].Value.replace(',', '.')) * 100;
                }
                else if (module.Properties[p].Name == 'Status.Battery')
                {
                    var blevel = 0;
                    blevel = parseFloat(module.Properties[p].Value);
                    if (blevel == 255) blevel = 0;
                    else if (blevel > 80 && blevel <= 100) blevel = 100;
                    else if (blevel > 60) blevel = 80; 
                    else if (blevel > 40) blevel = 60; 
                    else if (blevel > 20) blevel = 40; 
                    else if (blevel > 10) blevel = 20; 
                    else if (blevel > 0) blevel = 10; 
                    customdata += '<img alt="Battery Level" src="pages/control/widgets/homegenie/generic/images/battery_level_' + blevel + '.png" width="33" height="18" style="vertical-align: middle;" /> &nbsp; <font style="color:gray;">' + module.Properties[p].Value + '%</font> &nbsp;&nbsp;&nbsp;&nbsp; ';
                }
                else if (module.Properties[p].Name == 'Sensor.DoorWindow')
                {
                    var doorw = (module.Properties[p].Value.replace(',', '.')) * 1;
        			text += (doorw > 0 ? '<span style="color:green;font-weight:bold">OPENED</span>' : '<span style="font-weight:bold">CLOSED</span>');
                }
                else if (module.Properties[p].Name == 'Sensor.Tamper')
                {
                    var tamper = (module.Properties[p].Value.replace(',', '.')) * 1;
                    if (tamper > 0)
                    {
                        customdata += '<span style="color:red">TAMPERED</span>';
                    }
                }
                else if (module.Properties[p].Name == "Sensor.Temperature") {
                    temp = Math.round(module.Properties[p].Value.replace(',', '.') * 100) / 100;
                    customdata += '<img alt="Luminance Level" src="pages/control/widgets/homegenie/generic/images/temperature.png" width="18" height="18" style="vertical-align: middle;" /> &nbsp; <font style="color:gray;">' + temp + '</font> &nbsp;&nbsp;&nbsp;&nbsp; ';
                }
                else if (module.Properties[p].Name == 'Sensor.Luminance')
                {
                    customdata += '<img alt="Luminance Level" src="pages/control/widgets/homegenie/generic/images/luminance.png" width="18" height="18" style="vertical-align: middle;" /> &nbsp; <font style="color:gray;">' + module.Properties[p].Value + '%</font> &nbsp;&nbsp;&nbsp;&nbsp; ';
                }
                else if (module.Properties[p].Name.indexOf('Sensor.') == 0 && module.Properties[p].Name != 'Sensor.MotionDetect') {
                    customdata += '<b>' + module.Properties[p].Name.replace('Sensor.', '') + '</b> &nbsp; <font style="color:gray;">' + Math.round(module.Properties[p].Value.replace(',', '.') * 100) / 100 + '</font> &nbsp;&nbsp;&nbsp;&nbsp; ';
                }
            }

        }
        //
        if (watts != null && (type == 'light' || type == 'dimmer' || type == 'switch'))
        {
			text += '<span class="ui-li-count"><strong>kW</strong> ' + (watts.toFixed(3)) + '</span>';
        }
        //
        if (level != null && (type == 'light' || type == 'dimmer' || type == 'switch'))
        {
        	if (level >= 99 || level == 0)
        	{
				text += '<span><strong>' + (level >= 99 ? 'ON' : 'OFF') + '</strong></span>';
        	}
        	else
        	{
				text += '<span><strong>' + level.toFixed(0) + '%</strong></span>';
        	}
        }
        else if (level != null && type == 'shutter')
        {
			text += (level > 0 ? '<span style="color:green;font-weight:bold">OPENED</span>' : '<span style="font-weight:bold">CLOSED</span>');
        }
        //
//        if (temp != null)
//        {
//			text += '<span class="ui-li-count"><strong>&#8451;</strong> ' + temp.toFixed(1) + '</span>';
//        }
        //
        if (customdata != '')
        {
			text += '<span> ' + customdata + '</span>';
        }
        //
        if (text != '')
        {
        	text = '<div style="margin-top:3px;font-size:9pt;color:darkblue;">' + text + '</div>';
        }
        //
		return text;
	}


</script>	







