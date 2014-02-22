HG.WebApp.ProgramEdit = HG.WebApp.ProgramEdit || {};
HG.WebApp.ProgramEdit._CurrentProgram = Array();
HG.WebApp.ProgramEdit._CurrentProgram.Name = 'New Program';
HG.WebApp.ProgramEdit._CurrentProgram.Conditions = Array(); 
HG.WebApp.ProgramEdit._CurrentProgram.Commands = Array(); 
HG.WebApp.ProgramEdit._IsCapturingConditions = false;
HG.WebApp.ProgramEdit._IsCapturingCommands = false;

HG.WebApp.ProgramEdit.InitializePage = function () 
	{
		$('#page_automation_editprogram').on('pageinit', function (e) {
			$('#program_delete_button').bind('click', function (event) {
				HG.WebApp.ProgramEdit.DeleteProgram( HG.WebApp.ProgramEdit._CurrentProgram.Address );
                return true;
            });
//            $('#editprograms_backbutton').on('click', function() {
//                history.back();
//                return false;
//            });
            //
            $('#editprograms_actionmenu').on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramEdit.RefreshProgramOptions();
            });
            $('#editprograms_code_actionmenu').on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramEdit.RefreshProgramOptions();
            });
            //
            $('#automation_capture_condition_popup').popup().on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramEdit._IsCapturingConditions = true;
            });
            //
            $('#automation_capture_condition_popup').popup().on('popupafterclose', function (event) {
                HG.WebApp.ProgramEdit._IsCapturingConditions = false;
            });
            //
            $('#automation_capture_command_popup').popup().on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramEdit._IsCapturingCommands = true;
            });
            //
            $('#automation_capture_command_popup').popup().on('popupafterclose', function (event) {
                HG.WebApp.ProgramEdit._IsCapturingCommands = false;
            });
            //
            $('#automation_domain').on('popupbeforeposition', function (event) {
	            $('#automation_conditiontarget li:gt(0)').remove();
                var domains = Array();
	            for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) 
                {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++)
                    {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain)
                        {
                            exists = true; 
                            break;
                        }
                    }
                    if (!exists)
                    {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if (HG.WebApp.ProgramEdit.GetDomainComparableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
                        //
                        var displayname = HG.WebApp.Data.Modules[m].Domain.substring(HG.WebApp.Data.Modules[m].Domain.lastIndexOf('.') + 1);
                        if (displayname == 'Automation') displayname = 'Programs';
                        $('#automation_conditiontarget').append('<li data-theme="' + uitheme + '" data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_target_popup">' + displayname + '</a></li>');
                    }
                }
                $('#automation_conditiontarget').listview('refresh');//trigger('create');
            });
            //
            $('#automation_condition_value_domain').on('popupbeforeposition', function (event) {
	            $('#automation_conditionvalue_domain').empty();
                var domains = Array();
	            for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) 
                {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++)
                    {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain)
                        {
                            exists = true; 
                            break;
                        }
                    }
                    if (!exists)
                    {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if (HG.WebApp.ProgramEdit.GetDomainComparableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
                        //
                        var displayname = HG.WebApp.Data.Modules[m].Domain.substring(HG.WebApp.Data.Modules[m].Domain.lastIndexOf('.') + 1);
                        if (displayname == 'Automation') displayname = 'Programs';
                        $('#automation_conditionvalue_domain').append('<li data-theme="' + uitheme + '" data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_condition_value_address">' + displayname + '</a></li>');
                    }
                }
                $('#automation_conditionvalue_domain').listview('refresh');//trigger('create');
            });
            //
            $('#automation_command_domain_popup').on('popupbeforeposition', function (event) {
	            $('#automation_commandtarget li:gt(1)').remove();
                var domains = Array();
	            for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) 
                {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++)
                    {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain)
                        {
                            exists = true; 
                            break;
                        }
                    }
                    if (!exists)
                    {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if (HG.WebApp.ProgramEdit.GetDomainControllableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
                        //
                        var displayname = HG.WebApp.Data.Modules[m].Domain.substring(HG.WebApp.Data.Modules[m].Domain.lastIndexOf('.') + 1);
                        $('#automation_commandtarget').append('<li data-theme="' + uitheme + '" data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_command_target_popup">' + displayname + '</a></li>');
                    }
                }
                $('#automation_commandtarget').listview('refresh');//trigger('create');
            });
            //
            $('#automation_target_popup').on('popupbeforeposition', function (event) {
            });
            //
            $('#editprograms_code_codeblockstoggle').click(function(e) {
                e.stopImmediatePropagation();
                e.preventDefault();
                if ($('#automation_program_scriptcondition').next().css('display') == 'none')
                {
                    $('#automation_program_scriptcondition').next().css('display', '');
                    $('#automation_program_scriptsource').next().css('display', 'none');
                }
                else
                {
                    $('#automation_program_scriptcondition').next().css('display', 'none');
                    $('#automation_program_scriptsource').next().css('display', '');
                }
                HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
            });
        });
	};

HG.WebApp.ProgramEdit.GetDomainControllableModules = function (domain, showall)
    {
        var mods = Array();
	    if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length)
	    {
		    for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
			    if (HG.WebApp.Data.Modules[m].Domain == domain) {
				    if (HG.WebApp.ProgramEdit.IsModuleControllable(HG.WebApp.Data.Modules[m]) || showall)
				    {
                        mods.push(HG.WebApp.Data.Modules[m]);
                    }
                }
            }
        }
        return mods;
    };
    
HG.WebApp.ProgramEdit.IsModuleControllable = function (module)
    {
        return (module.DeviceType != 'Generic' && module.DeviceType != 'Sensor' && module.DeviceType != 'DoorWindow' && module.DeviceType != 'Temperature');
    };

HG.WebApp.ProgramEdit.GetDomainComparableModules = function (domain, showall)
    {
        var mods = Array();
	    if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length)
	    {
		    for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
			    if (HG.WebApp.Data.Modules[m].Domain == domain) {
				    if (HG.WebApp.ProgramEdit.GetModuleComparableProperties(HG.WebApp.Data.Modules[m]).length > 0 || showall)
				    {
                        mods.push(HG.WebApp.Data.Modules[m]);
                    }
                }
            }
        }
        return mods;
    };

HG.WebApp.ProgramEdit.GetModuleComparableProperties = function (module)
    {
        var props = Array();
		for (var p = 0; p < module.Properties.length; p++)
        {
            var prop = module.Properties[p];
            if (prop.Name.substring(0, 17) == 'ConfigureOptions.' || prop.Name.substring(0, 14) == 'VirtualModule.' || prop.Name.substring(0, 7) == 'Widget.') continue;
            props.push(prop);
        }
        return props;
    };

HG.WebApp.ProgramEdit.RefreshProgramEditorTitle = function () 
    {
        if ($('#automation_program_scriptcondition').next().css('display') == 'none')
        {
            $('#page_automation_programcode_title').html('<span style="font-size:7pt;font-weight:bold">EDIT PROGRAM <b>CODE TO RUN</b></span><br />' + HG.WebApp.ProgramEdit._CurrentProgram.Name);
            $('#editprograms_code_codeblockstoggle .ui-btn-text').text('Edit Trigger Code');
            setTimeout(function(){
			    editor2.refresh();
            }, 500);
        }
        else
        {
            $('#page_automation_programcode_title').html('<span style="font-size:7pt;font-weight:bold">EDIT PROGRAM <b>TRIGGER CODE</b></span><br />' + HG.WebApp.ProgramEdit._CurrentProgram.Name);
            $('#editprograms_code_codeblockstoggle .ui-btn-text').text('Edit Code to Run');
            setTimeout(function(){
			    editor1.refresh();
            }, 500);
        }
		//
		if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors != '')
		{
            $('#program_error_button').show(100);
		}
        else
        {
            $('#program_error_button').hide(100);
        }

    };

HG.WebApp.ProgramEdit.RefreshProgramOptions = function () 
    {
	    $('[id=editprograms_actionmenu_run]').each( function() { $(this).show(); } );
	    $('[id=editprograms_actionmenu_break]').each( function() { $(this).hide(); } );
	    $('[id=editprograms_actionmenu_run]').each( function() { $(this).addClass('ui-disabled'); } );
	    $('[id=editprograms_actionmenu_compile]').each( function() { $(this).addClass('ui-disabled'); } );
	    //
	    setTimeout(function () {
	        HG.Automation.Programs.List(function () {
	            $('[id=editprograms_actionmenu_compile]').each( function() { $(this).removeClass('ui-disabled'); } );
	            $('[id=editprograms_actionmenu_run]').each( function() { $(this).removeClass('ui-disabled'); } );
	            $('[id=editprograms_actionmenu_run]').each( function() { $(this).hide(); } );
	            //
	            var cp = HG.WebApp.Utility.GetProgramByAddress(HG.WebApp.ProgramEdit._CurrentProgram.Address);
	            if (cp != null) {
	                if (cp.IsRunning) {
	                    $('[id=editprograms_actionmenu_break]').each( function() { $(this).show(); } );
	                }
	                else {
	                    $('[id=editprograms_actionmenu_run]').each( function() { $(this).show(); } );
	                }
	            }
	        });
	    }, 1500);
	};

HG.WebApp.ProgramEdit.ProgramEnable = function (pid, isenabled)
	{
	    var fn = (isenabled ? 'Enable' : 'Disable');
		//
		$.mobile.showPageLoadingMsg();
		// 
		$('#control_groupslist').empty();
		//
		$('#automation_program_saving').popup().popup('open');
		$.ajax({
			type: 'POST',
			url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + fn + '/' + pid + '/',
			success: function (response) {
				$('#automation_program_saving').popup('close'); 
				$.mobile.hidePageLoadingMsg();
			},
			error: function (a, b, c) {
				$('#automation_program_saving').popup('close'); 
				$.mobile.hidePageLoadingMsg();
			}
		});	        	
	};


HG.WebApp.ProgramEdit.UpdateProgram = function (programblock, compile)
	{
		$.mobile.showPageLoadingMsg();
		// 
		$('#control_groupslist').empty();
		//
		$('#automation_program_saving').popup().popup('open');
		$.ajax({
			type: 'POST',
			url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + (compile ? 'Compile' : 'Update')  + '/',
			//                        dataType: 'json',
			data: JSON.stringify(programblock),
			success: function (response) {
				$('#automation_program_saving').popup('close'); 
				$.mobile.hidePageLoadingMsg();
				if (response.trim() != '')
				{
					//$('#program_barbutton_run').attr('disabled', 'disabled');
					//$('#program_barbutton_run').button().button('refresh');
					$.mobile.showPageLoadingMsg( $.mobile.pageLoadErrorMessageTheme, 'Error compiling program!', true );
					setTimeout( $.mobile.hidePageLoadingMsg, 2000 );
					HG.WebApp.ProgramEdit.ShowProgramErrors( response );
				}
				else
				{
					//$('#program_barbutton_run').removeAttr('disabled');
					//$('#program_barbutton_run').button().button('refresh');
					$.mobile.showPageLoadingMsg( $.mobile.loadingMessageTheme, 'Saving succeed.', true );
					setTimeout( $.mobile.hidePageLoadingMsg, 2000 );
					HG.WebApp.ProgramEdit.HideProgramErrors( );
				}
                //
	            // update modules
	            //TODO: make this better...
	            setTimeout(function () {
	                HG.Configure.Modules.List(function (data) {
	                    //
	                    try {
	                        HG.WebApp.Data.Modules = eval(data);
	                    } catch (e) { }
	                });
	            }, 3000);

			},
			error: function (a, b, c) {
				$('#automation_program_saving').popup('close'); 
				$.mobile.hidePageLoadingMsg();
				//
				$.mobile.showPageLoadingMsg( $.mobile.pageLoadErrorMessageTheme, $.mobile.pageLoadErrorMessage, true );
				setTimeout( $.mobile.hidePageLoadingMsg, 5000 );
			}
		});	        	
	};

HG.WebApp.ProgramEdit.ShowProgramErrors = function (message)
    {
        if (typeof (message) == 'undefined') message == '';
        HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = message;
        if (message == '') return;
        //
		for (i = 0; i < HG.WebApp.Data.Programs.length; i++) 
        {
			if (HG.WebApp.Data.Programs[i].Address == HG.WebApp.ProgramEdit._CurrentProgram.Address)
			{
                HG.WebApp.Data.Programs[i].ScriptErrors = message;
                break;
            }
        }
        //
		var errs = message.split('\n');
		var msgs = '';
		for (x = 0; x < errs.length; x++)
		{
			if (errs[x].trim() != '')
			{
				msgs += '<b>' + (x + 1) + '.</b> ' + errs[x] + '<br/>';
			}
		}
		$('#program_error_message_text').html('<h3>Errors:</h3><i>' + msgs + '</i><h3 style="color:red;font-weight:bold">Program disabled, fix errors first.</h3>');
		$('#program_error_message').popup().popup('open');
        $('#program_error_button').show(100);
	};

HG.WebApp.ProgramEdit.HideProgramErrors = function ()
	{
		$('#program_error_message_text').html('');
        $('#program_error_button').hide();
	};

HG.WebApp.ProgramEdit.CompileProgram = function () {
        HG.WebApp.ProgramEdit.HideProgramErrors();
        var programblock = HG.WebApp.ProgramEdit.SetProgramData();
	    HG.WebApp.ProgramEdit.UpdateProgram(programblock, true);
    };
HG.WebApp.ProgramEdit.SaveProgram = function () {
        $('#program_error_button').hide(500);
        var programblock = HG.WebApp.ProgramEdit.SetProgramData();
	    HG.WebApp.ProgramEdit.UpdateProgram(programblock, false);
	};
HG.WebApp.ProgramEdit.SetProgramData = function () {
        HG.WebApp.AutomationGroupsList._CurrentGroup = $('#automation_programgroup').val();
        HG.WebApp.ProgramEdit._CurrentProgram.Group = $('#automation_programgroup').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.Name = $('#automation_programname').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.Description = $('#automation_programdescription').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = editor1.getValue(); //$('#automation_program_scriptcondition').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
	    var programblock = {
	        'Address': HG.WebApp.ProgramEdit._CurrentProgram.Address,
	        'Type': HG.WebApp.ProgramEdit._CurrentProgram.Type,
	        'Group': HG.WebApp.ProgramEdit._CurrentProgram.Group,
	        'Name': HG.WebApp.ProgramEdit._CurrentProgram.Name,
	        'Description': HG.WebApp.ProgramEdit._CurrentProgram.Description,
	        'IsEnabled': HG.WebApp.ProgramEdit._CurrentProgram.IsEnabled,
	        'ScriptCondition': HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition,
	        'ScriptSource': HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource,
	        'ConditionType': HG.WebApp.ProgramEdit._CurrentProgram.ConditionType,
	        'Conditions': HG.WebApp.ProgramEdit._CurrentProgram.Conditions,
	        'Commands': HG.WebApp.ProgramEdit._CurrentProgram.Commands
	    }
        return programblock;
	};
	
HG.WebApp.ProgramEdit.CheckAndRunProgram = function (program)
	{
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = editor1.getValue(); //$('#automation_program_scriptcondition').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
		//
		// check if program is using the special var PROGRAM_OPTIONS_STRING
		// this var is used to pass a string argument to the program
		if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource.indexOf('PROGRAM_OPTIONS_STRING') > 0)
		{
			var prompt = 'Enter program options:';
			if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource.substring(0, 24) == '//OPTIONS_STRING_PROMPT=')
			{
				prompt = HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource.substring(24);
				prompt = prompt.substring(0, prompt.indexOf('\n'));			
			}
		    $('#simplestringdialog').simpledialog({
		        'mode': 'string',
		        'prompt': prompt,
		        'cleanOnClose': false,
		        'buttons': {
		            'OK': {
		                click: function () {
							HG.WebApp.ProgramEdit.RunProgram( program.Address, $('#simplestringdialog').attr('data-string') );
		                }
		            },
		            'Cancel': {
		                click: function () {
		                    //console.log(this);
		                },
		                icon: "delete",
		            }
		        }			
			});
  		}
  		else
  		{
			HG.WebApp.ProgramEdit.RunProgram( program.Address, null );
  		}
	};

HG.WebApp.ProgramEdit.BreakProgram = function (pid)
    {
        $.mobile.showPageLoadingMsg();
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Break/' + pid + '/',
            success: function (response) {
                HG.WebApp.ProgramEdit.RefreshProgramOptions();
                $.mobile.hidePageLoadingMsg();
            },
            error: function (a, b, c) {
                $.mobile.hidePageLoadingMsg();
            }
        });	        	

    }	
	
HG.WebApp.ProgramEdit.RunProgram = function (pid, options)
	{
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = editor1.getValue(); //$('#automation_program_scriptcondition').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
		//		
		$.mobile.showPageLoadingMsg();
        HG.Automation.Programs.Run(pid, options, function(res){
            if (res != null) HG.WebApp.ProgramEdit.RefreshProgramOptions();
			$.mobile.hidePageLoadingMsg();
        });
	};

HG.WebApp.ProgramEdit.DeleteProgram = function (program) {
	    $.mobile.showPageLoadingMsg();
	    HG.Automation.Programs.DeleteProgram(program, function () {
	        $.mobile.hidePageLoadingMsg();
	        setTimeout(function () {
	            $.mobile.changePage($('#page_automation_programs'), { transition: 'fade' });
	        }, 200);
	    });
	};