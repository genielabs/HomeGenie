HG.WebApp.ProgramEdit = HG.WebApp.ProgramEdit || {};
HG.WebApp.ProgramEdit._CurrentProgram = Array();
HG.WebApp.ProgramEdit._CurrentProgram.Name = 'New Program';
HG.WebApp.ProgramEdit._CurrentProgram.Conditions = Array(); 
HG.WebApp.ProgramEdit._CurrentProgram.Commands = Array(); 
HG.WebApp.ProgramEdit._CurrentSketchFile = '';
HG.WebApp.ProgramEdit._IsCapturingConditions = false;
HG.WebApp.ProgramEdit._IsCapturingCommands = false;
HG.WebApp.ProgramEdit._CurrentTab = 1;

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
            $('#automation_program_delete_button').bind('click', function (event) {
				HG.WebApp.Utility.SwitchPopup('#editprograms_actionmenu', '#automation_program_delete');
                return true;
            });editor1
			//
			$('#configure_program_editorcompilecode').bind('click', function (event) {
				HG.WebApp.ProgramEdit.CompileProgram();
                return true;
            });
			//
			$('#configure_program_editorcompilecode2').bind('click', function (event) {
				HG.WebApp.ProgramEdit.CompileProgram();
                return true;
            });
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
                        $('#automation_conditiontarget').append('<li data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_target_popup">' + displayname + '</a></li>');
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
                        $('#automation_conditionvalue_domain').append('<li data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_condition_value_address">' + displayname + '</a></li>');
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
                        $('#automation_commandtarget').append('<li data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_command_target_popup">' + displayname + '</a></li>');
                    }
                }
                $('#automation_commandtarget').listview('refresh');//trigger('create');
            });
            //HG.WebApp.ProgramEdit.CompileProgram
            //$('#automation_target_popup').on('popupbeforeposition', function (event) {
            //});
            //
            // Arduino Sketch files management
            $('#automation_program_listfiles').on('popupbeforeposition', function (event) {
                HG.WebApp.ProgramEdit.SketchFileList();
            });
            $('#automation_program_sketchfiles_add').bind('click', function(event) {
                $('#programfile_new_name').val('');
                HG.WebApp.Utility.SwitchPopup('#automation_program_listfiles', '#automation_program_fileadd', true);
            });
            $('#programfile_new_button').bind('click', function(event) {
                $('#automation_program_fileadd').popup('close');
                var filename = $('#programfile_new_name').val();
                $.mobile.loading('show', { text: 'Adding file ' + filename, textVisible: true, theme: 'a', html: '' });
                HG.Automation.Programs.ArduinoFileAdd(HG.WebApp.ProgramEdit._CurrentProgram.Address, filename, function(res){
                    if (res == 'EXISTS')
                    {
                        $.mobile.loading('show', { text: 'A file named ' + filename + ' already exists', textVisible: true, theme: 'a', html: '' });
                        setTimeout(function(){
                            $.mobile.loading('hide');
                        }, 3000);
                    }
                    else
                    {
                        $.mobile.loading('hide');
                        HG.WebApp.ProgramEdit.SketchFileOpen(filename);
                    }
                });
            });
            $('#automation_program_sketchfiles_edit').bind('click', function(event) {
                $('#automation_program_listfiles').popup('close');
                var filename = $('#automation_program_sketchfiles li a.ui-btn-active').attr('data-context-value');
                HG.WebApp.ProgramEdit.SketchFileOpen(filename);
            });
            $('#automation_program_sketchfiles_delete').bind('click', function(event) {
                var filename = $('#automation_program_sketchfiles li a.ui-btn-active').attr('data-context-value');
                $.mobile.loading('show', { text: 'Deleting file ' + filename, textVisible: true, theme: 'a', html: '' });
                HG.Automation.Programs.ArduinoFileDelete(HG.WebApp.ProgramEdit._CurrentProgram.Address, filename, function(res){
                    HG.WebApp.ProgramEdit.SketchFileList();
                    if (filename == HG.WebApp.ProgramEdit._CurrentSketchFile)
                    {
                        HG.WebApp.ProgramEdit._CurrentSketchFile = '';
                        HG.WebApp.ProgramEdit.SketchFileOpen('main');
                    }
                    $.mobile.loading('hide');
                });
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
        var editMode = 'wizard';
        if (typeof(HG.WebApp.ProgramEdit._CurrentProgram.Type) != 'undefined') editMode = HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase();
    	if (editMode != 'wizard')
    	{
	        var errors = HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors;
			if (typeof errors != 'undefined' && errors.trim() != '' && errors.trim() != '[]')
			{
				HG.WebApp.ProgramEdit.ShowProgramErrors(errors);
			}
			else
			{
				HG.WebApp.ProgramEdit.HideProgramErrors();
			}
		}
		else
		{
			HG.WebApp.ProgramEdit.HideProgramErrors();
		}
		// update title
        var status = HG.WebApp.ProgramsList.GetProgramStatusColor(HG.WebApp.ProgramEdit._CurrentProgram);
        var statusImage = '<img src="images/common/led_' + status + '.png" style="width:24px;height:24px;vertical-align:middle;margin-bottom:5px;margin-right:5px;" /> ';
        $('#page_automation_program_title').html('<span style="font-size:9pt;font-weight:bold">PROGRAM EDITOR (' + editMode + ')</span><br />' + statusImage + HG.WebApp.ProgramEdit._CurrentProgram.Address + ' ' + HG.WebApp.ProgramEdit._CurrentProgram.Name);
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
	                if (cp.ScriptErrors.trim() != '' && cp.ScriptErrors.trim() != '[]') {
                        HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = cp.ScriptErrors;
	                }
	                else {
	                    HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = '';
	                }
                    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
                }
	        });
	    }, 500);
	};

HG.WebApp.ProgramEdit.ProgramEnable = function (pid, isenabled)
	{
	    var fn = (isenabled ? 'Enable' : 'Disable');
	    var action = (isenabled ? 'Enabling' : 'Disabling');
	    $.mobile.loading('show', { text: action + ' program', textVisible: true, theme: 'a', html: '' });
		$('#control_groupslist').empty();
		$.ajax({
			type: 'POST',
			url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + fn + '/' + pid + '/',
			data: "{ dummy: 'dummy' }",
			success: function (response) {
				$.mobile.loading('hide');
			},
			error: function (a, b, c) {
				$.mobile.loading('hide');
			}
		});	        	
	};


HG.WebApp.ProgramEdit.UpdateProgram = function (programblock, compile)
	{
		//$('#configure_program_editorruncode').addClass('ui-disabled');
		$('#configure_program_editorcompilecode').addClass('ui-disabled');
		$('#configure_program_editorcompilecode2').addClass('ui-disabled');
	    $.mobile.loading('show', { text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_saving'), textVisible: true, theme: 'a', html: '' });
		$('#control_groupslist').empty();
		$.ajax({
			type: 'POST',
			url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + (compile ? 'Compile' : 'Update')  + '/',
			dataType: 'text',
			data: JSON.stringify(programblock),
			success: function (response) {
				$.mobile.loading('hide');
				//$('#configure_program_editorruncode').removeClass('ui-disabled');
				$('#configure_program_editorcompilecode').removeClass('ui-disabled');
				$('#configure_program_editorcompilecode2').removeClass('ui-disabled');
				if (response.trim() != '' && response.trim() != '[]')
				{
					//$('#proHG.WebApp.ProgramEdit.UpdateProgramgram_barbutton_run').attr('disabled', 'disabled');
				    //$('#program_barbutton_run').button().button('refresh');
				    $.mobile.loading('show', { text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_error_updating'), textVisible: true });
					HG.WebApp.ProgramEdit.ShowProgramErrors(response);
				}
				else
				{
					//$('#program_barbutton_run').removeAttr('disabled');
				    //$('#program_barbutton_run').button().button('refresh');
				    $.mobile.loading('show', { text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_saving_succeed'), textVisible: true });
				    HG.WebApp.ProgramEdit.RefreshProgramEditorTitle();
					//HG.WebApp.ProgramEdit.HideProgramErrors();
				}
				setTimeout(function () { $.mobile.loading('hide'); }, 2000);
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
				$.mobile.loading('hide');
				//$('#configure_program_editorruncode').removeClass('ui-disabled');
				$('#configure_program_editorcompilecode').removeClass('ui-disabled');
				$('#configure_program_editorcompilecode2').removeClass('ui-disabled');
			    //
				$.mobile.loading('show', { text: 'An error occurred!', textVisible: true });
				setTimeout(function () { $.mobile.loading('hide'); }, 5000);
			}
		});	        	
	};

HG.WebApp.ProgramEdit.JumpToLine = function (blockType, position) 
	{
		var editor = (blockType == 'TC' ? editor1 : editor2);
		if (blockType == 'TC')
		{
			HG.WebApp.ProgramEdit.SetTab(3);
		}
		else
		{
			HG.WebApp.ProgramEdit.SetTab(2);
		}
		editor.focus();
		editor.setCursor(0);
       	var lc = editor.addLineClass(position.line, null, "center-me");
	    window.setTimeout(function() {
	       var line = $('#page_automation_editprogram .CodeMirror-lines .center-me');
	       if (typeof line.offset() != 'undefined')
	       {
		       var h = line.parent();
		       $('.CodeMirror-scroll').scrollTop(0).scrollTop(line.offset().top - $('.CodeMirror-scroll').offset().top - Math.round($('.CodeMirror-scroll').height()/2));
		       editor.removeLineClass(lc, null, "center-me");
			   editor.setCursor(position);
		    }
	   }, 500);
	};

HG.WebApp.ProgramEdit.ShowProgramErrors = function (message)
    {
        editor1.clearGutter('CodeMirror-lint-markers-1');
        editor2.clearGutter('CodeMirror-lint-markers-2');
        //
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
        /*
		var errs = message.split('\n');
		var msgs = '';
		for (x = 0; x < errs.length; x++)
		{
			if (errs[x].trim() != '')
			{
				msgs += '<b>' + (x + 1) + '.</b> ' + errs[x] + '<br/>';
			}
		}*/
		//
		var errors = null;
		try
		{
		    errors = eval(message);
		} catch (e) { }
        //
		if (errors != null)
		{
		    var currentLine = 0, currentBlock = '', marker = null, message = '', popupMessage = '';
		    for (var e = 0; e < errors.length; e++)
		    {
		        var err = errors[e];
		        if (err.Line > 0)
		        {
                    if (currentLine != err.Line || currentBlock != err.CodeBlock)
                    {
    		            if (marker != null)
    		            {
    		                $(marker).qtip({
    		                    content: { title: 'Error', text: message, button: 'Close' },
    		                    show: { event: 'mouseover', solo: true },
    		                    hide: 'mouseout',
    		                    style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' }
    		                });
    		                message = '';
    		            }
    		            marker = document.createElement('div');
    		            marker.className = 'CodeMirror-lint-marker-error';
    		            if (err.CodeBlock == 'TC') // TC = Trigger Code
    		            {
    		                editor1.setGutterMarker(err.Line - 1, 'CodeMirror-lint-markers-1', marker);
    		            }
    		            else // CR = Code to Run = Program Code
    		            {
    		                editor2.setGutterMarker(err.Line - 1, 'CodeMirror-lint-markers-2', marker);
    		            }
    		            currentLine = err.Line;
    		            currentBlock = err.CodeBlock;
                    }
                    message += '<b>(' + err.Line + ',' + err.Column + '):</b> ' + err.ErrorMessage + '</br>';
                    popupMessage += '<b><a href="javascript:HG.WebApp.ProgramEdit.JumpToLine(\'' + err.CodeBlock + '\', { line: ' + (err.Line - 1) + ', ch: ' + (err.Column - 1) + ' })">Line ' + err.Line + ', Column ' + err.Column + '</a></b> (<font style="color:' + (err.CodeBlock == 'TC' ? 'yellow">Trigger Code' : 'lime">Program Code') + '</font>):<br/>';
		        }
		        popupMessage += '&nbsp;&nbsp;&nbsp;&nbsp;<em>' + err.ErrorMessage.replace(/\n/g, '</br>&nbsp;&nbsp;&nbsp;&nbsp;') + '</em><br /><br />';
            }
		    if (marker != null) {
		        $(marker).qtip({
		            content: { title: 'Error', text: message, button: 'Close' },
		            show: { event: 'mouseover', solo: true },
		            hide: 'mouseout',
		            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' }
		        });
		    }
		    //
		    //$('#program_error_message_text').html(popupMessage);
		    if (popupMessage != '')
		    {
			    $('#program_error_button').show();
			    $('#program_error_button2').show();
			    //
			    $('#program_error_button').qtip({
			            content: { title: 'Error', text: popupMessage, button: 'Close' },
			            show: { event: 'mouseover', ready: (HG.WebApp.ProgramEdit._CurrentTab == 2 ? true : false), delay: 500 },
				        hide: { event: false, inactive: 5000 },
			            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
			            position: { adjust : { screen: true }, my: 'top center', at: 'bottom center' }
			        });
			    //
			    $('#program_error_button2').qtip({
			            content: { title: 'Error', text: popupMessage, button: 'Close' },
			            show: { event: 'mouseover', ready: (HG.WebApp.ProgramEdit._CurrentTab == 3 ? true : false), delay: 500 },
				        hide: { event: false, inactive: 5000 },
			            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
			            position: { adjust : { screen: true }, my: 'top center', at: 'bottom center' }
			        });
			}
			else
			{
			    $('#program_error_button').hide();
			    $('#program_error_button2').hide();
			}
		    //setTimeout(function () {
		    //    if ($('#program_error_message_text').html() != '')
		    //    {
		    //        $('#program_error_message').popup().popup('open');
		    //        $('#program_error_button').show(100);
		    //    }
		    //}, 2000);
        }
        
        HG.WebApp.ProgramEdit.RefreshCodeMirror();                

	};

HG.WebApp.ProgramEdit.HideProgramErrors = function ()
	{
        editor1.clearGutter('CodeMirror-lint-markers-1');
        editor2.clearGutter('CodeMirror-lint-markers-2');
        //$('#program_error_message_text').html('');
        $('#program_error_button').hide();
        $('#program_error_button2').hide();
        $('.qtip').hide();
        //$('#program_error_message').popup().popup('close');
        HG.WebApp.ProgramEdit.RefreshCodeMirror();                
	};

HG.WebApp.ProgramEdit.RefreshCodeMirror = function() {
        
        // refresh editors
        setTimeout(function(){
            editor1.refresh();
            editor2.refresh();
            editor3.refresh();
        }, 500);
        
    };

HG.WebApp.ProgramEdit.CompileProgram = function () {
        HG.WebApp.ProgramEdit.HideProgramErrors();
        var programblock = HG.WebApp.ProgramEdit.SetProgramData();
        //
        if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'arduino')
        {
            // save other opened sketch files before compiling
            HG.WebApp.ProgramEdit.SketchFileSave(function(){
                HG.WebApp.ProgramEdit.UpdateProgram(programblock, true);
            });
        }
        else
        {
            HG.WebApp.ProgramEdit.UpdateProgram(programblock, true);
        }
    };
HG.WebApp.ProgramEdit.SaveProgram = function () {
        $('#program_error_button').hide();
        $('#program_error_button2').hide();
        var programblock = HG.WebApp.ProgramEdit.SetProgramData();
        //
        if (HG.WebApp.ProgramEdit._CurrentProgram.Type.toLowerCase() == 'arduino')
        {
            // save other opened sketch files before compiling
            HG.WebApp.ProgramEdit.SketchFileSave(function(){
                HG.WebApp.ProgramEdit.UpdateProgram(programblock, false);
            });
        }
        else
        {
            HG.WebApp.ProgramEdit.UpdateProgram(programblock, false);
        }
	};
HG.WebApp.ProgramEdit.SetProgramData = function () {
        HG.WebApp.AutomationGroupsList._CurrentGroup = $('#automation_programgroup').val();
        HG.WebApp.ProgramEdit._CurrentProgram.Group = $('#automation_programgroup').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.Name = $('#automation_programname').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.Description = $('#automation_programdescription').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = editor1.getValue(); //$('#automation_program_scriptcondition').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
	    HG.WebApp.ProgramEdit._CurrentProgram.ScriptErrors = '';
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
/*		if (HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource.indexOf('PROGRAM_OPTIONS_STRING') > 0)
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
  		else*/
  		{
			HG.WebApp.ProgramEdit.RunProgram( program.Address, null );
  		}
	};

HG.WebApp.ProgramEdit.BreakProgram = function (pid)
    {
	    $.mobile.loading('show', { text: 'Stopping program', textVisible: true, theme: 'a', html: '' });
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Break/' + pid + '/',
            data: "{ dummy: 'dummy' }",
            success: function (response) {
                HG.WebApp.ProgramEdit.RefreshProgramOptions();
                $.mobile.loading('hide');
            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
            }
        });	        	

    }	
	
HG.WebApp.ProgramEdit.RunProgram = function (pid, options)
	{
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptCondition = editor1.getValue(); //$('#automation_program_scriptcondition').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
		HG.WebApp.ProgramEdit._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
		//		
	    $.mobile.loading('show', { text: 'Running program', textVisible: true, theme: 'a', html: '' });
        HG.Automation.Programs.Run(pid, options, function(res){
            if (res != null) HG.WebApp.ProgramEdit.RefreshProgramOptions();
			$.mobile.loading('hide');
        });
	};

HG.WebApp.ProgramEdit.DeleteProgram = function (program) {
	    $.mobile.loading('show', { text: 'Deleting program', textVisible: true, theme: 'a', html: '' });
	    HG.Automation.Programs.DeleteProgram(program, function () {
	        $.mobile.loading('hide');
	        setTimeout(function () {
	            $.mobile.changePage($('#page_automation_programs'), { transition: 'fade' });
	        }, 200);
	    });
	};


HG.WebApp.ProgramEdit.SetTab = function (tabindex) 
	{
		HG.WebApp.ProgramEdit._CurrentTab = tabindex;
		$('#program_edit_tab1').hide();
		$('#program_edit_tab2').hide();
		$('#program_edit_tab3').hide();
		$('#program_edit_tab1_button').removeClass('ui-btn-active');
		$('#program_edit_tab2_button').removeClass('ui-btn-active');
		$('#program_edit_tab3_button').removeClass('ui-btn-active');
		$('#program_edit_tab' + tabindex).show();
		$('#program_edit_tab' + tabindex + '_button').addClass('ui-btn-active');
        //        
        HG.WebApp.ProgramEdit.RefreshCodeMirror();                
	};

// Arduino Sketch File management
HG.WebApp.ProgramEdit.SketchFileSelect = function(el)
{
    $('#automation_program_sketchfiles li a').removeClass('ui-btn-active');
    $(el).addClass('ui-btn-active');
    $('#automation_program_sketchfiles_edit').removeClass('ui-disabled');
    $('#automation_program_sketchfiles_delete').addClass('ui-disabled');
    if ($(el).attr('data-context-value') != 'main')
    {
        $('#automation_program_sketchfiles_delete').removeClass('ui-disabled');
    }
};
HG.WebApp.ProgramEdit.SketchFileOpen = function(filename)
{
    $.mobile.loading('show', { text: 'Opening file ' + filename, textVisible: true, theme: 'a', html: '' });
    if (filename == 'main')
    {
        // the main sketch file is stored in standard code editor (editor2)
        $('#configure_program_editorfilename').html(filename);
        $(editor3.getWrapperElement()).hide();
        $(editor2.getWrapperElement()).show();
        editor2.refresh();
        $.mobile.loading('hide');
    }
    else
    {
        // all other sketch files are stored in editor3
        $(editor2.getWrapperElement()).hide();
        $(editor3.getWrapperElement()).show();
        HG.Automation.Programs.ArduinoFileLoad(HG.WebApp.ProgramEdit._CurrentProgram.Address, filename, function(src){
            HG.WebApp.ProgramEdit._CurrentSketchFile = filename;
            $('#configure_program_editorfilename').html(filename);
            editor3.setValue(src);
            editor3.refresh();
            $.mobile.loading('hide');
        });                        
    }
};
HG.WebApp.ProgramEdit.SketchFileSave = function(callback)
{
    if (HG.WebApp.ProgramEdit._CurrentSketchFile == '') 
    {
        if (callback != null) callback();
        return;
    }
    $.mobile.loading('show', { text: 'Saving file ' + HG.WebApp.ProgramEdit._CurrentSketchFile, textVisible: true, theme: 'a', html: '' });
    var srcfile = editor3.getValue();
    HG.Automation.Programs.ArduinoFileSave(
        HG.WebApp.ProgramEdit._CurrentProgram.Address, 
        HG.WebApp.ProgramEdit._CurrentSketchFile, 
        srcfile,
        function(src){
            $.mobile.loading('hide');
            if (callback != null) callback();
        }
    );                        
};
HG.WebApp.ProgramEdit.SketchFileList = function()
{
    $('#automation_program_sketchfiles_edit').addClass('ui-disabled');
    $('#automation_program_sketchfiles_delete').addClass('ui-disabled');
    HG.Automation.Programs.ArduinoFileList(HG.WebApp.ProgramEdit._CurrentProgram.Address, function(list){
        $('#automation_program_sketchfiles').empty();
        $('#automation_program_sketchfiles').append('<li data-icon="false"><a onclick="HG.WebApp.ProgramEdit.SketchFileSelect(this)" href="#" data-context-value="main"><strong>Main Sketch Code</strong></a></li>');
        for(var f = 0; f < list.length; f++)
        {
            $('#automation_program_sketchfiles').append('<li data-icon="false"><a data-context-value="' + list[f] + '" onclick="HG.WebApp.ProgramEdit.SketchFileSelect(this)" href="#">' + list[f] + '</a></li>');
        }
        $('#automation_program_sketchfiles').listview('refresh');
    });
};