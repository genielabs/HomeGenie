HG.WebApp.ProgramsList = HG.WebApp.ProgramsList || {};

HG.WebApp.ProgramsList.InitializePage = function () 
	{
		$('#page_automation_programs').on('pageinit', function (e) {
		    $('#automation_program_add').on('popupbeforeposition', function (event) {
				$('#program_new_name').val('');
			});
            //
            $('#automationprograms_program_options').on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramsList.RefreshProgramOptions();
                HG.WebApp.ProgramsList.RefreshProgramDetails();
            });
            $('#automationprograms_program_options').on('popupafteropen', function (event) {
                var hasdetails = true;
                if ($('#automationprograms_program_details').text().trim() == '')
                {
	                $('#automationprograms_program_options_tab1').css('visibility', 'hidden');
                	hasdetails = false;
                }
                else
                {
	                $('#automationprograms_program_options_tab1').css('visibility', '');
                }
                if ($('#automationprograms_program_parameters').text().trim() == '')
                {
	                $('#automationprograms_program_options_tab0').css('visibility', 'hidden');
					if (hasdetails) 
					{
						HG.WebApp.ProgramsList.SetOptionsTab(1);
					}
					else
					{
						$('#automationprograms_program_parameters').hide();
			            $('#automationprograms_program_details').hide();
					}
                }
                else
                {
	                $('#automationprograms_program_options_tab0').css('visibility', '');
					HG.WebApp.ProgramsList.SetOptionsTab(0);
                }
            });
            //
            $('#automationprograms_delete_button').bind('click', function (event) {
                HG.WebApp.ProgramsList.DeleteGroup(HG.WebApp.AutomationGroupsList._CurrentGroup);
            });
		    //	
			$('#program_new_button').bind('click', function (event) {
			    HG.WebApp.ProgramsList.AddProgram($('#program_new_name').val());
			});	
			//
			$('#program_switchtypecancel_button').bind('click', function (event) {
				$('#automation_programtype').val(HG.WebApp.ProgramEdit._CurrentProgram.Type);
				$('#automation_programtype').selectmenu('refresh');
			});
			//
			$('#program_switchtype_button').bind('click', function (event) {
			    HG.WebApp.ProgramEdit._CurrentProgram.Type = $('#automation_programtype').select().val();
			    if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() != 'wizard') {
			        $('#automation_conditiontype').val('OnTrue');
			        $('#automation_conditiontype').selectmenu().selectmenu('refresh');
                    // set a valid trigger code for the current programming language
			        if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'python') {
			            editor1.setValue('"""\nPut your trigger code logic here.\nCall \'hg.SetConditionTrue()\' when you want\nthe \'Code To Run\' to be executed.\n"""\nhg.SetConditionFalse()\n');
			            editor2.setValue('"""\nPython Automation Script\nExample for using Helper Classes:\nhg.Modules.WithName(\'Light 1\').On()\n"""\n')
                    }
			        else if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'ruby') {
			            editor1.setValue('# Put your trigger code logic here.\n# Call \'hg.SetConditionTrue()\' when you want\n# the \'Code To Run\' to be executed.\nhg.SetConditionFalse()\n');
			            editor2.setValue('# Ruby Automation Script\n# Example for using Helper Classes:\n# hg.Modules.WithName(\'Light 1\').On()\n');
                    }
			        else if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'javascript') {
			            editor1.setValue('// Put your trigger code logic here.\n// Call \'hg.SetConditionTrue()\' when you want\n// the \'Code To Run\' to be executed.\nhg.SetConditionFalse();\n');
			            editor2.setValue('// Javascript Automation Script\n// Example for using Helper Classes:\n// hg.Modules.WithName(\'Light 1\').On();\n');
                    }
			        else if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'csharp') {
			            editor1.setValue('// Put your trigger code logic here.\n// To execute the Code To Run,\n// use a \'return true\' or \'return false\' otherwise.\nreturn false;\n');
			            editor2.setValue('// CSharp Automation Program Plugin\n// Example for using Helper Classes:\n// Modules.WithName("Light 1").On();\n');
                    }
                } else {
			        $('#automation_conditiontype').val('OnSwitchTrue');
			        $('#automation_conditiontype').selectmenu().selectmenu('refresh');
			    }
			    HG.WebApp.ProgramsList.RefreshProgramType();
			});
            $('#automationprograms_program_edit').bind('click', function (event) {
                HG.WebApp.ProgramsList.EditProgram();
            });
            //
            $('#automationprograms_program_delete_button').bind('click', function (event) {
                HG.WebApp.ProgramsList.DeleteProgram(HG.WebApp.ProgramEdit._CurrentProgram.Address);
            });
            //
            $('#program_import_button').bind('click', function () {
                if ($('#program_import_uploadfile').val() == "") {
                    alert('Select a file to import first');
                    $('#program_import_uploadfile').parent().stop().animate({ borderColor: "#FF5050" }, 250)
                        .animate({ borderColor: "#FFFFFF" }, 250)
                        .animate({ borderColor: "#FF5050" }, 250)
                        .animate({ borderColor: "#FFFFFF" }, 250);
                }
                else {
                    $('#automation_program_import').popup('close');
                    $.mobile.loading('show', { text: 'Importing, please wait...', textVisible: true, html: '' });
                    $('#program_import_form').attr('action', '../HomeAutomation.HomeGenie/Automation/Programs.Import/' + HG.WebApp.AutomationGroupsList._CurrentGroup);
                    $('#program_import_form').submit();
                }
            });
            $('#program_import_uploadframe').bind('load', function () {
                $('#program_import_uploadfile').val('');
                $.mobile.loading('hide');
                HG.WebApp.ProgramsList.LoadPrograms(null);
            });

		});
	};


HG.WebApp.ProgramsList._CurrentOptionsTab = 0;

HG.WebApp.ProgramsList.SetOptionsTab = function (tabid)
	{
		HG.WebApp.ProgramsList._CurrentOptionsTab = tabid;
        //
		$('#automationprograms_program_options_tab0 a').removeClass('ui-btn-active');
		$('#automationprograms_program_options_tab0 a').trigger('create');
		$('#automationprograms_program_options_tab1 a').removeClass('ui-btn-active');
		$('#automationprograms_program_options_tab1 a').trigger('create');
		//
		if (tabid == 0)
		{
            HG.WebApp.ProgramsList.RefreshProgramOptions();
            $('#automationprograms_program_details').hide();
			$('#automationprograms_program_parameters').show();
		}
		else
		{
            HG.WebApp.ProgramsList.RefreshProgramDetails();
			$('#automationprograms_program_parameters').hide();
			$('#automationprograms_program_details').show();
		}
        //
		setTimeout(function(){
			$('#automationprograms_program_options_tab' + tabid + ' a').addClass('ui-btn-active');
			$('#automationprograms_program_options_tab' + tabid + ' a').trigger('create');
			//
			setTimeout(function(){
				$("#automationprograms_program_options").popup("reposition", {positionTo: 'window'});
			}, 50);
		}, 200);
	};

HG.WebApp.ProgramsList.RefreshProgramDetails = function () 
	{
        var fieldparams = $('#automationprograms_program_details');
        fieldparams.empty();
        fieldparams.trigger('create');
		//
        var cp = HG.WebApp.Utility.GetProgramByAddress(HG.WebApp.ProgramEdit._CurrentProgram.Address);
        if (cp != null)
        {
	        var params = '';
	        //
	        // Program Features
	        //
	        var features = cp.Features;
            for (var f = 0; f < features.length; f++) {
	            params += '<li data-theme="' + uitheme + '"><p style="font-size:12pt;margin:0;padding:0"><strong>' + features[f].Property + '</strong> : ';
	            params += '' + features[f].Description + '</br>'
	            var fordomains = features[f].ForDomains; if (fordomains == '') fordomains = 'Any';
	            var fortypes = features[f].ForTypes; if (fortypes == '') fortypes = 'Any';
	            params += '<span style="font-size:8pt;font;">Applies to: &nbsp;&nbsp;&nbsp; <strong>Domain</strong> &#9658; <em>' + fordomains + '</em> &nbsp;&nbsp;&nbsp; <strong>Type</strong> &#9658; <em>' + fortypes + '</em></span></p></li>';	            
            }
	        if (params != '')
	        {
	        	params = '</br><ul data-role="listview"><li data-theme="' + uitheme + '" data-role="list-divider">Implemented Features</li>' + params + '</ul></br>';
	        	fieldparams.append(params);
        	}
	 	} 
	 	// 		
	    var module = HG.WebApp.Utility.GetModuleByDomainAddress(HG.WebApp.ProgramEdit._CurrentProgram.Domain, HG.WebApp.ProgramEdit._CurrentProgram.Address);
	    if (module != null)
        {
	        //
	        // Program Config Input Fields
	        //
			params = '';
	        for (var p = 0; p < module.Properties.length; p++) {
	        	if (module.Properties[p].Name.substring(0, 17) == 'ConfigureOptions.')
	        	{
		        	var updatetime = module.Properties[p].UpdateTime; updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
	            	var d = new Date(updatetime);	        	
		            params += '<li data-theme="' + uitheme + '"><p style="font-size:12pt;margin:0;padding:0"><strong>' + module.Properties[p].Name.substring(17) + '</strong> = ';
		            params += '"' + module.Properties[p].Value + '"</br>'
		            params += '<span style="font-size:8pt;"><em>' + d + '</em></span></p></li>';
	            }
	        }
	        if (params != '')
	        {
	        	params = '</br><ul data-role="listview"><li data-theme="' + uitheme + '" data-role="list-divider">Configuration Options</li>' + params + '</ul></br>';
	        	fieldparams.append(params);
        	}
    	}
    	//
	    if (module != null)
	    {
	        //
	        // Program Parameter Fields
	        //
            var params = '';
	        for (var p = 0; p < module.Properties.length; p++) {
	        	if (module.Properties[p].Name.substring(0, 17) == 'ConfigureOptions.' || module.Properties[p].Name == 'Widget.DisplayModule' || module.Properties[p].Name == 'VirtualModule.ParentId') continue;
	        	var updatetime = module.Properties[p].UpdateTime; updatetime = updatetime.replace(' ', 'T'); // fix for IE and FF
            	var d = new Date(updatetime);	        	
	            params += '<li data-theme="' + uitheme + '"><p style="font-size:12pt;margin:0;padding:0"><strong>' + module.Properties[p].Name + '</strong> = ';
	            params += '"' + module.Properties[p].Value + '"</br>'
	            params += '<span style="font-size:8pt;font;"><em>' + d + '</em></span></p></li>';
	        }
	        if (params != '')
	        {
	        	params = '</br><ul data-role="listview"><li data-theme="' + uitheme + '" data-role="list-divider">Module Parameters</li>' + params + '</ul></br>';
	        	fieldparams.append(params);
        	}
	        //
	        // Program Widget Display module
	        //
			var widget = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
	        //
	        // Program Handling Module Events (yes/no)
	        //
	        //
	        // Program Adding WebService (base url/no)
	        //
		}
        //
        // Program Virtual Modules
        //
        var params = '';
		if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length)
		{
			for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
				var cmod = HG.WebApp.Data.Modules[m];
				var vparentid = HG.WebApp.Utility.GetModulePropertyByName(cmod, "VirtualModule.ParentId");
				if (vparentid != null && vparentid.Value == HG.WebApp.ProgramEdit._CurrentProgram.Address && cmod.Domain != 'HomeAutomation.HomeGenie.Automation')
				{
		            params += '<li data-theme="' + uitheme + '"><p style="font-size:12pt;margin:0;padding:0"><strong>' + cmod.Domain + ' ' + cmod.Address + '</strong>';
		            params += '</br>'
		            params += '<span style="font-size:8pt;font;"> ';
		            params += '<strong>Type</strong> &#9658; <em>' + cmod.DeviceType + '</em> &nbsp;&nbsp;&nbsp;';
		            if (cmod.Name != '') params += '<strong>Name</strong> &#9658; <em>' + cmod.Name + '</em> &nbsp;&nbsp;&nbsp;';
		            params += '</span></p></li>';
				}
			}
        }
        if (params != '')
        {
        	params = '</br><ul data-role="listview"><li data-theme="' + uitheme + '" data-role="list-divider">Virtual Modules</li>' + params + '</ul></br>';
        	fieldparams.append(params);
    	}
        //
	    fieldparams.trigger('create');
	};
		
HG.WebApp.ProgramsList.RefreshProgramOptions = function () 
    {
        var fieldparams = $('#automationprograms_program_parameters');
        fieldparams.empty();
        fieldparams.trigger('create');
        //
        var cp = HG.WebApp.Utility.GetProgramByAddress(HG.WebApp.ProgramEdit._CurrentProgram.Address);
        if (cp != null)
        {
            $('#automationprograms_program_title').html(cp.Name);
            if (cp.Description != 'undefined' && cp.Description != null) {
                $('#automationprograms_program_description').html(cp.Description.replace(/\n/g, '<br />'));
            } else {
                $('#automationprograms_program_description').html('');
            }
        }
        //
	    var module = HG.WebApp.Utility.GetModuleByDomainAddress(HG.WebApp.ProgramEdit._CurrentProgram.Domain, HG.WebApp.ProgramEdit._CurrentProgram.Address);
	    if (module != null)
        {
            var arr = Array();
	        for (var p = 0; p < module.Properties.length; p++) {
	            if (module.Properties[p].Name.substring(0, 17) == 'ConfigureOptions.') {
	                // eg. ConfigureOptions.WeatherLocation and ConfigureOptions.BridgeAddress
                    arr.push(module.Properties[p]);
	            }
	        }

	        arr.sort(function (a, b) {
	            var i = 0;
	            a = a.Description;
                b = b.Description;
	            if (a < b) {
	                return -1;
	            } else if (a > b) {
	                return 1;
	            } else {
	                return 0;
	            }
	        });

            for (var p = 0; p < arr.length; p++) {

                var desc = (arr[p].Description && arr[p].Description != 'undefined' && arr[p].Description != '' ? arr[p].Description : arr[p].Name);
                var opt = '<div><p style="font-weight:bold">' + desc + '</p><input type="text" value="' + arr[p].Value + '" data-parameter-name="' + arr[p].Name + '" onchange="HG.WebApp.ProgramsList.UpdateProgramParameter($(this))" /></div>';
                fieldparams.append(opt);            
            
            }

	    }
        //
	    fieldparams.trigger('create');
	};


HG.WebApp.ProgramsList.UpdateProgramParameter = function (el) 
    {
        var parameter = el.attr('data-parameter-name');
        var module = HG.WebApp.Utility.GetModuleByDomainAddress(HG.WebApp.ProgramEdit._CurrentProgram.Domain, HG.WebApp.ProgramEdit._CurrentProgram.Address);
        for (var p = 0; p < module.Properties.length; p++) {
            if (module.Properties[p].Name == parameter) {
                module.Properties[p].Value = el.val();
                module.Properties[p].NeedsUpdate = 'true';
            }
        }
        HG.WebApp.GroupModules.UpdateModule(module);
    };


HG.WebApp.ProgramsList.RefreshProgramType = function ()
	{
		HG.WebApp.ProgramEdit._CurrentProgram.Type = $('#automation_programtype').select().val();
        //
		if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() != 'wizard')
		{
			$('#configure_program_editfortypewizard').css('display', 'none');
			$('#configure_program_editfortypecsharp').css('display', '');
        }
		else
		{
			$('#configure_program_editfortypewizard').css('display', '');
			$('#configure_program_editfortypecsharp').css('display', 'none');
        }
	};

HG.WebApp.ProgramsList.ChangeProgramType = function (type) 
	{
		$("#automation_program_switchtype").popup().popup('open');
	};

HG.WebApp.ProgramsList.LoadPrograms = function (callback) 
    {
	    $.mobile.showPageLoadingMsg();
	    HG.Automation.Programs.List(function () {
	        HG.WebApp.ProgramsList.RefreshPrograms();
	        $.mobile.hidePageLoadingMsg();
	        if (callback) callback();
	    });
	};

	HG.WebApp.ProgramsList.RefreshPrograms = function () {
	    var automationtitle = HG.WebApp.Locales.GetLocaleString('configure_program_automationtitle');
	    $('#configure_automation_group_title').html('<font style="color:gray">' + (automationtitle != null ? automationtitle : 'Automation')+ '</font><br />' + HG.WebApp.AutomationGroupsList._CurrentGroup);
	    $('#configure_programslist').empty();
	    $('#configure_programslist').append('<li data-theme="a" data-icon="false" data-role="list-divider">Programs List</li>');
	    //
	    for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
	    	var progrm = HG.WebApp.Data.Programs[i];
	        var pgroup = progrm.Group;
	        if (pgroup == null || pgroup == 'undefined') pgroup = '';
	        if (pgroup != HG.WebApp.AutomationGroupsList._CurrentGroup) continue;
            //
	        var pname = progrm.Name;
	        var item = '<li data-theme="' + uitheme + '" data-icon="' + (progrm.IsEnabled ? 'check' : 'alert') + '" data-program-domain="' + progrm.Domain + '"  data-program-address="' + progrm.Address + '" data-program-index="' + i + '">';
	        item += '<a href="#automationprograms_program_options" data-rel="popup" data-position-to="window" data-transition="slidedown">';
	        //
	        var status = HG.WebApp.ProgramsList.GetProgramStatusColor(progrm);
	        var triggertime = '';
	        if (progrm.TriggerTime != null)
	        {
	            var triggerts = moment(progrm.TriggerTime);
	            triggertime = triggerts.format('L LT');
        	}
	        //
	        item += '	<p class="ui-li-aside ui-li-desc">' + progrm.Type + ' &nbsp;&nbsp;&nbsp;<strong>PID:</strong> ' + progrm.Address + '<br><font style="opacity:0.5">' + triggertime + '</font></p>';
	        item += '	<h3 class="ui-li-heading"><img src="images/common/led_' + status + '.png" style="width:24px;height:24px;vertical-align:middle;margin-bottom:5px;margin-right:5px;" /> ' + pname + '</h3>';
	        item += '	<p class="ui-li-desc">' + (progrm.Description != null ? progrm.Description : '') + ' &nbsp;</p>';
	        item += '</a>';
	        item += '<a data-theme="' + uitheme + '" style="border:0;-moz-border-radius: 0px;-webkit-border-radius: 0px;border-radius: 0px" href="javascript:HG.WebApp.ProgramsList.ToggleProgramIsEnabled(\'' + progrm.Address + '\')">' + (progrm.IsEnabled ? 'Tap to DISABLE program' : 'Tap to ENABLE program') + '</a>';
	        //
	        item += '</li>';
	        $('#configure_programslist').append(item);
	    }
	    $('#configure_programslist').listview();
	    $('#configure_programslist').listview('refresh');
	    //
	    $("#configure_programslist li").bind("click", function () {
	        HG.WebApp.ProgramEdit._CurrentProgram.Domain = $(this).attr('data-program-domain');
	        HG.WebApp.ProgramEdit._CurrentProgram.Address = $(this).attr('data-program-address');
	    });
	};

HG.WebApp.ProgramsList.GetProgramByAddress = function(pid)
    {
        var program = null;
        for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
            if (HG.WebApp.Data.Programs[i].Address == pid) 
            {
                program = HG.WebApp.Data.Programs[i];
                break;
            }
        }
        return program;
    };

HG.WebApp.ProgramsList.GetProgramStatusColor = function (prog)
    {
        var status = 'black';
        if (prog.IsRunning) {
            status = 'green';
        }
        else if (prog.IsEnabled) {
            status = 'yellow';
        }
        else {
            status = 'black';
        }
        if (prog.Type.toLowerCase() != 'wizard' && prog.ScriptErrors != '') {
            if (prog.IsEnabled) {
                status = 'red';
            }
            else {
                status = 'brown';
            }
        }
        return status; 
    };

HG.WebApp.ProgramsList.EditProgram = function () 
	{
	    $('#automation_programgroup').empty();
	    $('#automation_programgroup').append('<option value="">(select program group)</option>');
	    for (var i = 0; i < HG.WebApp.Data.AutomationGroups.length; i++) {
	        $('#automation_programgroup').append('<option value="' + HG.WebApp.Data.AutomationGroups[i].Name + '">' + HG.WebApp.Data.AutomationGroups[i].Name + '</option>');
	    }
	    $('#automation_programgroup').trigger('create');
		//
		for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
			if (HG.WebApp.Data.Programs[i].Address == HG.WebApp.ProgramEdit._CurrentProgram.Address)
			{
			    HG.WebApp.ProgramEdit._CurrentProgram.Type = HG.WebApp.Data.Programs[i].Type;
			    HG.WebApp.ProgramEdit._CurrentProgram.Group = HG.WebApp.Data.Programs[i].Group;
			    HG.WebApp.ProgramEdit._CurrentProgram.Name = HG.WebApp.Data.Programs[i].Name;
			    HG.WebApp.ProgramEdit._CurrentProgram.Description = HG.WebApp.Data.Programs[i].Description;
			    HG.WebApp.ProgramEdit._CurrentProgram.Address = HG.WebApp.Data.Programs[i].Address;
				HG.WebApp.ProgramEdit._CurrentProgram.Domain = HG.WebApp.Data.Programs[i].Domain;
				HG.WebApp.ProgramEdit._CurrentProgram.Conditions = HG.WebApp.Data.Programs[i].Conditions;
				HG.WebApp.ProgramEdit._CurrentProgram.Commands = HG.WebApp.Data.Programs[i].Commands;
                //
				$('#automation_programname').val(HG.WebApp.ProgramEdit._CurrentProgram.Name);
				$('#automation_programdescription').val(HG.WebApp.ProgramEdit._CurrentProgram.Description);
				//
				$('#automation_programgroup').val(HG.WebApp.ProgramEdit._CurrentProgram.Group);
				$('#automation_programgroup').selectmenu().selectmenu('refresh');
				//
				$('#automation_programtype').val(HG.WebApp.ProgramEdit._CurrentProgram.Type);
				$('#automation_programtype').selectmenu().selectmenu('refresh');
				HG.WebApp.ProgramsList.RefreshProgramType();
				//
				HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = HG.WebApp.Data.Programs[i].ScriptCondition;
				HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = HG.WebApp.Data.Programs[i].ScriptSource; 
				//
				editor1.setValue(HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition);
				editor2.setValue(HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource);
				setTimeout(function(){
					editor1.refresh();
					editor2.refresh();
				}, 500);
				//
				HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = HG.WebApp.Data.Programs[i].ConditionType;
				//
				switch (HG.WebApp.ProgramEdit._CurrentProgram.ConditionType)
                {
                    case 2:
                        HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = "OnSwitchFalse";
                        break;
                    case 3:
                        HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = "Once";
                        break;
                    case 4:
                        HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = "OnTrue";
                        break;
                    case 5:
                        HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = "OnFalse";
                        break;
                    default:
                        HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = "OnSwitchTrue";
                        break;
                }
				//
				$('#automation_conditiontype').val(HG.WebApp.ProgramEdit._CurrentProgram.ConditionType);
				$('#automation_conditiontype').selectmenu();
				$('#automation_conditiontype').selectmenu('refresh');
				//
				HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = HG.WebApp.Data.Programs[i].ScriptErrors;
				break;
			}
		}
        //
        $('#page_automation_program_title').html('<span style="font-size:7pt;font-weight:bold">EDIT PROGRAM</span><br />' + HG.WebApp.ProgramEdit._CurrentProgram.Name);
	};


HG.WebApp.ProgramsList.ExportProgram = function (progaddr) {
	    $('#program_import_downloadframe').attr('src', location.protocol + '../HomeAutomation.HomeGenie/Automation/Programs.Export/' + progaddr + '/');
	};


HG.WebApp.ProgramsList.AddProgram = function (progname) {
	    HG.Automation.Programs.AddProgram(HG.WebApp.AutomationGroupsList._CurrentGroup, progname, function (data) {
	        HG.WebApp.ProgramsList.LoadPrograms(function () {
	            HG.WebApp.ProgramEdit._CurrentProgram.Address = data;
	            HG.WebApp.ProgramsList.EditProgram();
	            $.mobile.changePage($('#page_automation_editprogram'), { transition: "slide" });
	        });
	    });
	};

HG.WebApp.ProgramsList.ToggleProgramIsEnabled = function (paddr) {
	    var cp = HG.WebApp.Utility.GetProgramByAddress(paddr);
	    cp.IsEnabled = !cp.IsEnabled;
	    $.mobile.showPageLoadingMsg();
	    HG.WebApp.ProgramEdit.ProgramEnable(cp.Address, cp.IsEnabled);
        setTimeout(function () {
	        HG.WebApp.ProgramsList.LoadPrograms(function () {
	            HG.WebApp.ProgramsList.RefreshPrograms();
	            $.mobile.hidePageLoadingMsg();
	        });
	    }, 3000);
	};


HG.WebApp.ProgramsList.DeleteGroup = function (group) {
    $.mobile.showPageLoadingMsg();
    HG.Configure.Groups.DeleteGroup('Automation', group, function () {
        $.mobile.changePage($('#page_configure_automationgroups'), { transition: "slide" });
        $.mobile.hidePageLoadingMsg();
    });
};

HG.WebApp.ProgramsList.DeleteProgram = function (program) {
    $.mobile.showPageLoadingMsg();
    HG.Automation.Programs.DeleteProgram(program, function () {
        HG.WebApp.ProgramsList.LoadPrograms();
        $.mobile.hidePageLoadingMsg();
    });
};