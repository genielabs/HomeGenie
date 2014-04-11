HG.WebApp.GroupModules = HG.WebApp.GroupModules || {};
HG.WebApp.GroupModules.CurrentGroup = '(none)';
HG.WebApp.GroupModules.CurrentModule = null;
HG.WebApp.GroupModules.CurrentModuleProperty = null;
HG.WebApp.GroupModules.EditModule = Array();

HG.WebApp.GroupModules.InitializePage = function () {
    $('#page_configure_groupmodules').on('pageinit', function (e) {

        $('#module_add_button').bind('click', function (event) {
            var selectedopt = $('#automation_group_moduleadd').find(":selected");
            var domain = selectedopt.attr('data-context-domain');
            var address = selectedopt.attr('data-context-value');
            HG.WebApp.GroupModules.AddGroupModule(HG.WebApp.GroupModules.CurrentGroup, domain, address);
        });
        //
        $('#group_delete_button').bind('click', function (event) {
            HG.WebApp.GroupModules.DeleteGroup(HG.WebApp.GroupModules.CurrentGroup);
        });
        //	
        $('#module_options_button').bind('click', function (event) {
            HG.WebApp.GroupModules.ShowModuleOptions(HG.WebApp.GroupModules.CurrentModule.Domain, HG.WebApp.GroupModules.CurrentModule.Address);
        });
        //
        $('#automation_group_modulechoose').on('popupbeforeposition', function (event) {
            $('#automation_group_moduleadd').empty();
            $('#automation_group_moduleadd').append(HG.WebApp.GroupModules.GetModulesListViewItems(HG.WebApp.GroupModules.CurrentGroup));
            $('#automation_group_moduleadd').selectmenu('refresh');
        });
        //
        $('#page_configure_groupmodules_propspopup').on('popupbeforeposition', function (event) {
            HG.WebApp.GroupModules.LoadModuleParameters();
        });
        //
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
            HG.WebApp.GroupsList.SaveModules();
        });
        //
        $('#module_remove_button').bind('click', function (event) {
            HG.WebApp.GroupModules.DeleteGroupModule(HG.WebApp.GroupModules.CurrentGroup, HG.WebApp.GroupModules.CurrentModule);
            HG.WebApp.GroupsList.SaveGroups(null);
        });
        //
        $('#automation_group_module_propdelete').bind('click', function () {
            if (HG.WebApp.GroupModules.CurrentModuleProperty != null) {
                HG.WebApp.GroupModules.ModulePropertyDelete(HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').first().val());
                HG.WebApp.GroupModules.CurrentModuleProperty.remove();
                HG.WebApp.GroupModules.CurrentModuleProperty = null;
            }
        });
        //
        $('#groupmodules_groupname').change(function () {
            HG.Configure.Groups.RenameGroup('Control', HG.WebApp.GroupModules.CurrentGroup, $('#groupmodules_groupname').val(), function (res) {
                HG.WebApp.GroupModules.CurrentGroup = $('#groupmodules_groupname').val();
                $("#configure_groupslist").attr('selected-group-name', $('#groupmodules_groupname').val());
                $.mobile.showPageLoadingMsg();
                HG.Configure.Modules.List(function (data) {
                    try {
                        HG.WebApp.Data.Modules = eval(data);
                    } catch (e) { }
                    HG.Automation.Programs.List(function () {
                        HG.WebApp.GroupsList.LoadGroups();
                        $.mobile.hidePageLoadingMsg();
                    });
                });
            });
        });
        //
        $("#page_configure_groupmodules_list").sortable();
        $("#page_configure_groupmodules_list").disableSelection();
        //<!-- Refresh list to the end of sort for having a correct display -->
        $("#page_configure_groupmodules_list").bind("sortstop", function (event, ui) {
            HG.WebApp.GroupModules.SortModules();
        });
    });
};

HG.WebApp.GroupModules.SortModules = function () {

    var neworder = '';
    $('#page_configure_groupmodules_list').children('li').each(function () {
        var midx = $(this).attr('data-module-index');
        if (midx >= 0) {
            neworder += (midx + ';')
        }
    });
    $.mobile.showPageLoadingMsg();
    HG.Configure.Groups.SortModules('Control', HG.WebApp.GroupModules.CurrentGroup, neworder, function (res) {
        HG.Configure.Groups.List('Control', function () {
            HG.WebApp.GroupModules.LoadGroupModules();
            $.mobile.hidePageLoadingMsg();
        });
    });

};

HG.WebApp.GroupModules.ModulePropertyDelete = function (name) {
    var module = HG.WebApp.GroupModules.CurrentModule;
    for (var p = 0; p < module.Properties.length; p++) {

        if (module.Properties[p].Name == name) {
            delete module.Properties[p];
            module.Properties.splice(p, 1);
        }

    }
}

HG.WebApp.GroupModules.ModulePropertyAdd = function (module, name, value) {
    var doesexists = false;
    for (var p = 0; p < module.Properties.length; p++) {

        if (module.Properties[p].Name == name) {
            module.Properties[p].Value = value;
            doesexists = true;
            break;
        }

    }
    if (!doesexists) {
        module.Properties.push({ Name: name, Value: value });
    }
}




HG.WebApp.GroupModules.UpdateModule = function (module) {
    $.mobile.showPageLoadingMsg();
    $.ajax({
        type: 'POST',
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Modules.Update/',
        //                        dataType: 'json',
        data: JSON.stringify(module, function (key, value) { if (key == "WidgetInstance") return undefined; else return value; }),
        success: function (response) {
            $.mobile.hidePageLoadingMsg();
        },
        error: function (a, b, c) {
            $.mobile.hidePageLoadingMsg();
        }
    });
};


HG.WebApp.GroupModules.UpdateCurrentModuleParameter = function () {
    HG.WebApp.GroupModules.ModulePropertyAdd(HG.WebApp.GroupModules.CurrentModule, HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').first().val(), HG.WebApp.GroupModules.CurrentModuleProperty.find('input[type=text]').last().val());
    var stop = $('#automation_group_module_params').children().last().offset().top - 314;
    $('#automation_group_module_params').animate({ scrollTop: '+=' + stop }, 1000);
    $('#automation_group_module_params').children().last().find('input[type=text]').first().focus();
    HG.WebApp.GroupModules.LoadModuleParameters();
}

HG.WebApp.GroupModules.LoadModuleParameters = function () {
    $('#automation_group_module_params').empty();
    //
    var module = HG.WebApp.GroupModules.CurrentModule;
    if (module.Properties != null) {
        for (var p = 0; p < module.Properties.length; p++) {
            var item = '<li data-theme="' + uitheme + '">';
            item += '        <div class="ui-grid-a">';
            item += '            <div class="ui-block-a" style="padding-right:7.5px;width:70%;"><input type="text" value="' + module.Properties[p].Name + '" onchange="HG.WebApp.GroupModules.UpdateCurrentModuleParameter()" style="font-size:11pt" -class="ui-disabled" /></div>';
            item += '            <div class="ui-block-b" style="padding-left:7.5px;width:30%;"><input type="text" value="' + module.Properties[p].Value + '" onchange="HG.WebApp.GroupModules.UpdateCurrentModuleParameter()" style="font-size:11pt" -class="ui-disabled" /></div>';
            item += '        </div>';
            item += '   </li>';
            $('#automation_group_module_params').append(item);
        }
    }
    //
    $('#automation_group_module_params').trigger('create');
    $('#automation_group_module_params').listview().listview('refresh');
    $('#automation_group_module_params input').focus(function () {
        var back = $(this).closest('div').parent().parent().parent();
        if (back.css('background') != '#E6E6FA') {
            $(this).attr('originalbackground', back.css('background'));
            back.css('background', '#E6E6FA');
            setTimeout("$('#automation_group_module_propdelete').removeClass('ui-disabled')", 500);
//            setTimeout("$('#automation_group_module_propsave').removeClass('ui-disabled')", 500);
            HG.WebApp.GroupModules.CurrentModuleProperty = back;
        }
    });
    $('#automation_group_module_params input').blur(function () {
        $(this).closest('div').parent().parent().parent().css('background', $(this).attr('originalbackground'));
        setTimeout("$('#automation_group_module_propdelete').addClass('ui-disabled')", 250);
//        setTimeout("$('#automation_group_module_propsave').addClass('ui-disabled')", 250);
    });
}

HG.WebApp.GroupModules.GetModulesListViewItems = function (groupname)
	{
	    var groupmodules = HG.Configure.Groups.GetGroupModules(groupname);
		var htmlopt = '';
		var cursect = '';
		if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length)
		{
			for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
				var module = HG.WebApp.Data.Modules[m];
				var haselement = $.grep(groupmodules.Modules, function(value){
					return (value.Domain == module.Domain && value.Address == module.Address);
				});
				if (haselement.length == 0) {
				    var propwidget = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
				    var vmparentid = HG.WebApp.Utility.GetModulePropertyByName(module, "VirtualModule.ParentId");
                    // check if no explicit witdget is specified and it's not a virtual module
				    if ((propwidget != null && propwidget.Value != null && propwidget.Value == "") && (vmparentid != null && vmparentid.Value == module.Address)) continue;
                    //
					if (cursect != module.Domain)
					{
						cursect = module.Domain;
						htmlopt += '<optgroup label="' + cursect + '"></optgroup>';
					}
					var displayname = (module.Name != '' ? module.Name : (module.Description != '' ? module.Description : module.DeviceType));
                    displayname += ' (' + module.Address + ')';
					htmlopt += '<option data-context-domain="' + module.Domain + '" data-context-value="' + module.Address + '">' + displayname + '</option>';
				}
			}
		}
		return htmlopt;
	};
				
HG.WebApp.GroupModules.AddGroupModule = function(group, domain, address)
	{
		//var module = HG.WebApp.Utility.GetModuleByDomainAddress(domain, address);
		//
	    var alreadyexists = false;
        var moduleindex = -1;
		for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
			if (HG.WebApp.Data.Groups[i].Name == group) {
				for (c = 0; c < HG.WebApp.Data.Groups[i].Modules.length; c++) {
					if (domain == HG.WebApp.Data.Groups[i].Modules[c].Domain && address == HG.WebApp.Data.Groups[i].Modules[c].Address) {
						alreadyexists = true;
						break;
					}
				}
				if (!alreadyexists) {
				    HG.WebApp.Data.Groups[i].Modules.push({ 'Address': address, 'Domain': domain });
				    moduleindex = HG.WebApp.Data.Groups[i].length - 1;
				}
				//
				break;
			}
		}
		//
        HG.WebApp.GroupsList.SaveGroups(function(){
            HG.WebApp.GroupModules.ModuleEdit($("#page_configure_groupmodules_list li").last());
            $.mobile.showPageLoadingMsg();
            setTimeout("$('#automation_group_module_edit').popup('open');$.mobile.hidePageLoadingMsg();", 1000);
        });
	};
	
HG.WebApp.GroupModules.UpdateWMWatts = function ( module, wmwatts )
    {
	    HG.WebApp.GroupModules.EditModule.WMWatts = wmwatts;
    }				
										
HG.WebApp.GroupModules.UpdateModuleType = function (type)
	{
		HG.WebApp.GroupModules.EditModule.DeviceType = type;
		type = type.toLowerCase();
		//
		$('#module_options_1').css('display', '');
		$('#module_options_2').css('display', '');
		$('#module_options_3').css('display', '');
		//
		if (type == 'light' || type == 'dimmer' || type == 'switch')
		{
			$('#module_vmwatts').val('0');
			$('#module_vmwatts_label').removeClass('ui-disabled');
			$('#module_vmwatts').removeClass('ui-disabled');
		}
        else if (type == 'program') {
            $('#module_options_1').css('display', 'none');
            $('#module_options_2').css('display', 'none');
            $('#module_options_3').css('display', 'none');
        }
        else if (type && type != undefined && type != 'generic' && type != '')
		{
			$('#module_vmwatts').val('');
			$('#module_vmwatts_label').addClass('ui-disabled');
			$('#module_vmwatts').addClass('ui-disabled');
		}
        configurepage_GetModuleIcon(HG.WebApp.GroupModules.EditModule, function(icon) {
            $('#module_icon').attr('src', icon);
        });
        //
        HG.WebApp.GroupModules.UpdateFeatures();
	};	
				
HG.WebApp.GroupModules.DeleteGroupModule = function (groupname, module) 
	{
		for (i = 0; i < HG.WebApp.Data.Groups.length; i++) 
		{
			if (HG.WebApp.Data.Groups[i].Name == groupname) 
			{
				HG.WebApp.Data.Groups[i].Modules = $.grep(HG.WebApp.Data.Groups[i].Modules, function (value) {
					return value.Domain != module.Domain || value.Address != module.Address;
				});
				break;
			}
		}
	};
				
HG.WebApp.GroupModules.DeleteGroup = function (group) 
	{
		$.mobile.showPageLoadingMsg();
		HG.Configure.Groups.DeleteGroup('Control', group, function () { 
			$.mobile.changePage($('#page_configure_groups'), { transition: "slide" });						
			$.mobile.hidePageLoadingMsg();
		});
	};

HG.WebApp.GroupModules.LoadGroupModules = function () 
    {
	    HG.WebApp.GroupModules.CurrentGroup = $("#configure_groupslist").attr('selected-group-name');
	    //
	    var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.GroupModules.CurrentGroup);
	    //
	    //$('#page_configure_groupmodules_title').html('<span style="font-size:7pt;font-weight:bold">EDIT GROUP</span>' + '<br />' + groupmodules.Name);
	    $('#groupmodules_groupname').val(groupmodules.Name);
	    //
	    $('#page_configure_groupmodules_list').empty();
	    $('#page_configure_groupmodules_list').listview().listview('refresh');
	    $('#page_configure_groupmodules_list').append('<li data-theme="a" data-icon="false" data-role="list-divider">Group Modules</li>');
	    //
	    var html = '';
	    for (var m = 0; m < groupmodules.Modules.length; m++) {
	        var domain_label = groupmodules.Modules[m].Domain.substring(groupmodules.Modules[m].Domain.lastIndexOf('.') + 1);
	        var address_label = "Module";
	        if (groupmodules.Modules[m].Domain == 'HomeAutomation.ZWave') {
	            address_label = "Node";
	        }
	        else if (groupmodules.Modules[m].Domain == 'EmbeddedSystems.RaspiGPIO') {
	            address_label = "GPIO";
	        }
	        else if (groupmodules.Modules[m].Domain == 'EmbeddedSystems.Weeco4mGPIO') {
	            address_label = "GPIO";
	        }
	        else if (groupmodules.Modules[m].Domain == 'HomeAutomation.HomeGenie.Automation') {
	            address_label = "Program";
	        }
	        var iconid = 'module_icon_image_' + m;
	        var icon = configurepage_GetModuleIcon(groupmodules.Modules[m], function(iconimage, elid){
	        	$('#' + elid).attr('src', iconimage);
	        }, iconid);
	        html += '<li data-theme="' + uitheme + '" data-icon="bars" data-module-index="' + m + '">';
	        html += '<a href="#automation_group_module_edit" data-rel="popup" data-transition="pop">';
	        html += '<table><tr><td rowspan="2" align="left"><img id="' + iconid + '" height="54" src="' + icon + '"></td>';
	        html += '<td style="padding-left:10px"><span>' + groupmodules.Modules[m].Name + '</span></td></tr>';
	        html += '<tr><td style="padding-left:10px"><span style="color:gray">' + domain_label + '</span> ' + address_label + ' ' + groupmodules.Modules[m].Address + '</td>';
	        html += '</tr></table></a>';
	        html += '<a href="#page_configure_groupmodules_propspopup" data-rel="popup" style="border:0;-moz-border-radius: 0px;-webkit-border-radius: 0px;border-radius: 0px">Module Parameters</a>';
	        html += '</li>';
	    }
	    $('#page_configure_groupmodules_list').append(html);
	    //
	    $('#page_configure_groupmodules_list').listview().listview('refresh');
	    //
	    $("#page_configure_groupmodules_list li").on("click", function () {
	        HG.WebApp.GroupModules.ModuleEdit($(this));
	    });
	    $("#configure_groupslist").listview("refresh");
	};


HG.WebApp.GroupModules.ModuleEdit = function (item) {

    var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.GroupModules.CurrentGroup);
    //
    $("#configure_groupslist").attr('selected-module-index', item.attr('data-module-index'));
    //
    var m = item.attr('data-module-index');
    //
    if (m) {
        HG.WebApp.GroupModules.CurrentModule = HG.WebApp.Utility.GetModuleByDomainAddress(groupmodules.Modules[m].Domain, groupmodules.Modules[m].Address);
        if (HG.WebApp.GroupModules.CurrentModule == null) {
            // module not found, pheraps it was removed
            // so we return the data in the group module reference (address and domain only)
            HG.WebApp.GroupModules.CurrentModule = groupmodules.Modules[m];
            HG.WebApp.GroupModules.CurrentModule.DeviceType = '';
        }
        //
        HG.WebApp.GroupModules.EditModule.Domain = HG.WebApp.GroupModules.CurrentModule.Domain;
        HG.WebApp.GroupModules.EditModule.Address = HG.WebApp.GroupModules.CurrentModule.Address;
        HG.WebApp.GroupModules.EditModule.Name = HG.WebApp.GroupModules.CurrentModule.Name;
        HG.WebApp.GroupModules.EditModule.Type = HG.WebApp.GroupModules.CurrentModule.Type;
        HG.WebApp.GroupModules.EditModule.DeviceType = HG.WebApp.GroupModules.CurrentModule.DeviceType;
        //
        HG.WebApp.GroupModules.EditModule.WMWatts = 0;
        if (HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualMeter.Watts") != null) {
            HG.WebApp.GroupModules.EditModule.WMWatts = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualMeter.Watts").Value;
        }
        //
        // disable option button if it's a virtual module
        $('#module_options_button').removeClass('ui-disabled');
        if (HG.WebApp.GroupModules.CurrentModule.Domain != 'HomeAutomation.ZWave')
        {
            $('#module_options_button').addClass('ui-disabled');
        }
        else if (HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualModule.ParentId") != null) {
            var parentid = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, "VirtualModule.ParentId").Value;
            if (parentid != HG.WebApp.GroupModules.CurrentModule.Address)
            {
                $('#module_options_button').addClass('ui-disabled');
            }
        }
        //
        HG.WebApp.GroupModules.UpdateModuleType(HG.WebApp.GroupModules.CurrentModule.DeviceType);
        //
        $('#module_title').html(HG.WebApp.GroupModules.EditModule.Domain.split('.')[1] + ' ' + HG.WebApp.GroupModules.EditModule.Address + ' - Settings');
        $('#module_name').val(HG.WebApp.GroupModules.EditModule.Name);
        $('#module_type').val(HG.WebApp.GroupModules.EditModule.DeviceType).attr('selected', true).siblings('option').removeAttr('selected');
        $('#module_type').selectmenu('refresh', true);
        configurepage_GetModuleIcon(HG.WebApp.GroupModules.EditModule, function (icon) {
            $('#module_icon').attr('src', icon);
        });
        $('#module_vmwatts').val(HG.WebApp.GroupModules.EditModule.WMWatts > 0 ? HG.WebApp.GroupModules.EditModule.WMWatts : '0');
        //
        HG.WebApp.GroupModules.UpdateFeatures();
    }

}

HG.WebApp.GroupModules.ShowFeatures = function (programid)
{
    $('#module_programs_features').empty();
    $('#module_programs_featuredesc').html(HG.WebApp.Data.Programs[programid].Description);
    for (var p = 0; p < HG.WebApp.GroupModules.EditModule.Properties.length; p++) {
        var mp = HG.WebApp.GroupModules.EditModule.Properties[p];
        if (mp.ProgramIndex == programid)
        {
            var featurecb = '';
            if (mp.FieldType == "text") {
                featurecb = '<label for="feature-' + p + '">' + mp.Description + '</label><input onchange="HG.WebApp.GroupModules.FeatureUpdate(HG.WebApp.GroupModules.EditModule, \'' + mp.Name + '\', this.value)" type="text" value="' + mp.Value + '" name="option-' + p + '" id="feature-' + p + '" data-mini="true" />';
                $('#module_programs_features').append(featurecb);
            }
        }
    }
    for (var p = 0; p < HG.WebApp.GroupModules.EditModule.Properties.length; p++) {
        var mp = HG.WebApp.GroupModules.EditModule.Properties[p];
        if (mp.ProgramIndex == programid) {
            if (mp.FieldType == "checkbox") {
                var checked = '';
                var featurecb = '';
                featurecb = '<label for="feature-' + p + '">' + mp.Description + '</label><input onchange="HG.WebApp.GroupModules.FeatureUpdate(HG.WebApp.GroupModules.EditModule, \'' + mp.Name + '\')" type="text" name="option-' + p + '" id="feature-' + p + '" data-mini="true" />';
                if (mp != null && mp.Value == 'On') {
                    checked = ' checked';
                }
                featurecb = '<input' + checked + ' onclick="HG.WebApp.GroupModules.FeatureToggle(HG.WebApp.GroupModules.EditModule, \'' + mp.Name + '\')" type="checkbox" name="option-' + p + '" id="checkbox-' + p + '" data-iconpos="right" data-mini="true" /><label for="checkbox-' + p + '">' + mp.Description + '</label>';
                $('#module_programs_features').append(featurecb);
            }
        }
    }
    $('#module_programs_features').trigger('create');
    $('#automation_group_module_edit').popup("reposition", { positionTo: 'window' });
}

HG.WebApp.GroupModules.UpdateFeatures = function () {
    HG.WebApp.GroupModules.EditModule.Properties = Array(); // used to store "features" values
    //
    $('#module_options_features').hide();
    $('#module_programs_featureset').empty();
    $('#module_programs_features').empty();
    //
    var featureset = '';
    var selected = -1;
    for (var p = 0; p < HG.WebApp.Data.Programs.length; p++) {
        var cprogram = -1;
        var features = HG.WebApp.Data.Programs[p].Features;
        if (features.length > 0) {
            for (var f = 0; f < features.length; f++) {
                var fd = ',' + features[f].ForDomains.toLowerCase() + ',|' + features[f].ForDomains.toLowerCase() + '|';
                var fs = ',' + features[f].ForTypes.toLowerCase() + ',|' + features[f].ForTypes.toLowerCase() + '|';
                var featurematch = (features[f].ForDomains == '' || fd.indexOf(HG.WebApp.GroupModules.EditModule.Domain.toLowerCase(), 0) >= 0) && (features[f].ForTypes == '' || fs.indexOf(HG.WebApp.GroupModules.EditModule.DeviceType.toLowerCase(), 0) >= 0);
                if (featurematch) {
                    var property = features[f].Property;
                    var prop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.CurrentModule, property);
                    HG.WebApp.Utility.SetModulePropertyByName(HG.WebApp.GroupModules.EditModule, property, (prop != null ? prop.Value : ""));
                    prop = HG.WebApp.Utility.GetModulePropertyByName(HG.WebApp.GroupModules.EditModule, property);
                    prop.ProgramIndex = p;
                    prop.FieldType = features[f].FieldType;
                    prop.Description = features[f].Description;
                    //
                    if (cprogram < 0)
                    {
                        var pname = HG.WebApp.Data.Programs[p].Name;
                        if (pname == '') pname = HG.WebApp.Data.Programs[p].Address;
                        featureset += '<option value="' + p + '">' + pname + '</option>';
                        cprogram = p;
                        if (selected < 0) selected = p;
                    }
                }
            }
        }
    }
    //
    if (featureset != '')
    {
        $('#module_programs_featureset').append(featureset);
        $('#module_programs_featureset').selectmenu('refresh', true);
        $('#module_options_features').show();
        //
        if (selected != -1) {
            HG.WebApp.GroupModules.ShowFeatures(selected);
        }
    }
    $('#automation_group_module_edit').popup("reposition", { positionTo: 'window' });
};

HG.WebApp.GroupModules.FeatureUpdate = function (module, property, value)
    {
        var mp = HG.WebApp.Utility.GetModulePropertyByName(module, property);
        HG.WebApp.Utility.SetModulePropertyByName(module, property, value);
        property.Changed = true;
    }

HG.WebApp.GroupModules.FeatureToggle = function (module, property)
    {
        var mp = HG.WebApp.Utility.GetModulePropertyByName(module, property);
        if (mp != null && mp.Value != "")
        {
            HG.WebApp.Utility.SetModulePropertyByName(module, property, "");
        }
        else
        {
            HG.WebApp.Utility.SetModulePropertyByName(module, property, "On");
        }
        property.Changed = true;
    };

HG.WebApp.GroupModules.ZWave_AssociationGet = function () 
    {
        $('#opt-zwave-association-label').html('Nodes Id in this group = ? (querying node...)');
        zwave_AssociationGet($('#configurepage_OptionZWave_id').val(), $('#configassoc-gid').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-association-label').html('Nodes Id in this group = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-association-label').html('Nodes Id in this group = ' + res);
            }
        });
    };

HG.WebApp.GroupModules.ZWave_ConfigVariableGet = function () 
    {
        $('#opt-zwave-configvar-label').html('Variable Value = ? (querying node...)');
        zwave_ConfigurationParameterGet($('#configurepage_OptionZWave_id').val(), $('#configvar-id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-configvar-label').html('Variable Value = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-configvar-label').html('Variable Value = ' + res);
            }
        });
    };

HG.WebApp.GroupModules.ZWave_BasicGet = function () 
    {
        $('#opt-zwave-basic-label').html('Basic Value = ? (querying node...)');
        zwave_BasicGet($('#configurepage_OptionZWave_id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-basic-label').html('Basic Value = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-basic-label').html('Basic Value = ' + res);
            }
        });
    };

HG.WebApp.GroupModules.ZWave_BatteryGet = function () 
    {
        $('#opt-zwave-battery-label').html('Battery Level = ? (querying node...)');
        zwave_BatteryGet($('#configurepage_OptionZWave_id').val(), function (res) {
            if (res == '')
            {
                $('#opt-zwave-battery-label').html('Battery Level = ? (operation timeout!)');
            }
            else
            {
                $('#opt-zwave-battery-label').html('Battery Level = ' + res + '%');
            }
        });
    };

HG.WebApp.GroupModules.ZWave_WakeUpGet = function () 
    {
        $('#opt-zwave-wakeup-label').html('Wake Up Interval = ? (querying node...)');
        zwave_WakeUpGet($('#configurepage_OptionZWave_id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-wakeup-label').html('Wake Up Interval = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-wakeup-label').html('Wake Up Interval = ' + res + 's');
            }
        });
    };

HG.WebApp.GroupModules.ZWave_NodeInfoRequest = function (callback) 
    {
        $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ? (querying node...)');
        zwave_ManufacturerSpecificGet($('#configurepage_OptionZWave_id').val(), function (res) {
            if (res == '')
            {
                $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ? (operation timeout!)');
                if (callback != null) callback(false);
            }
            else
            {
                var mspecs = res;
                $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ' + mspecs + ' (querying nodeinfo)');
                zwave_NodeInformationGet($('#configurepage_OptionZWave_id').val(), function (res) {
                    if (res == '') 
                    {
                        $('#opt-zwave-manufacturerspecs-label').html('Manufacturer Specific = ' + mspecs + ' (operation timeout!)');
                        if (callback != null) callback(false);
                    }
                    else
                    {                    
                        var nodeid = $('#configurepage_OptionZWave_id').val();
                        //TODO: find a better way of refreshing options data
                        HG.Configure.Modules.List(function (data) {
                            HG.WebApp.Data.Modules = eval(data);
                            HG.WebApp.GroupModules.ShowModuleOptions("HomeAutomation.ZWave", nodeid);
                        });
                        //
                        if (callback != null) callback(true);
                    }
                });
            }
        });
    };

HG.WebApp.GroupModules.SwitchBinaryParameterGet = function () 
    {
        $('#opt-zwave-switchbinary-label').html('Switch Binary = ? (querying node...)');
        zwave_SwitchBinaryParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-switchbinary-label').html('Switch Binary = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-switchbinary-label').html('Switch Binary = ' + (res == '0' ? 'Off' : 'On') );
            }
        });
    }

HG.WebApp.GroupModules.SwitchMultiLevelParameterGet = function () 
    {
        $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ? (querying node...)');
        zwave_SwitchMultilevelParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-switchmulti-label').html('Switch MultiLevel = ' + Math.round(parseFloat(res.replace(',', '.') * 99)));
            }
        });
    };

HG.WebApp.GroupModules.SensorBinaryParameterGet = function () 
    {
        $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ? (querying node...)');
        zwave_SensorBinaryParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-sensorbinary-label').html('Sensor Binary = ' + (res == '0' ? 'Off' : 'On'));
            }
        });
    };

HG.WebApp.GroupModules.SensorMultiLevelParameterGet = function () 
    {
        $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ? (querying node...)');
        zwave_SensorMultilevelParameterGet($('#configurepage_OptionZWave_id').val(), $('#instancevar-id').val(), function (res) {
            if (res == '') {
                $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ? (operation timeout!)');
            }
            else {
                $('#opt-zwave-sensormulti-label').html('Sensor MultiLevel = ' + res);
            }
        });
    };





HG.WebApp.GroupModules.ShowModuleOptions = function (domain, address) 
    {
        var module = HG.WebApp.Utility.GetModuleByDomainAddress(domain, address);
        //
        if (module != null) {
            switch (module.Domain) {
                case 'HomeAutomation.ZWave':
                    HG.Ext.ZWave.NodeSetup.Show(module);
			        $.mobile.changePage($('#configurepage_OptionZWave'), { transition: "slide" });                    
                    break;
                case 'HomeAutomation.X10':
                    $.mobile.changePage($('#configurepage_OptionX10'), { transition: "slide" });
                    break;
                default:
                    alert('No options page available for this module.');
            }
        }
    };

