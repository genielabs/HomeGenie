HG.WebApp.ProgramEdit = HG.WebApp.ProgramEdit || new function () {

    var $$ = this;

    $$.PageId = 'page_automation_editprogram';
    $$._CurrentProgram = Array();
    $$._CurrentProgram.Name = 'New Program';
    $$._CurrentProgram.Conditions = Array();
    $$._CurrentProgram.Commands = Array();
    $$._CurrentSketchFile = '';
    $$._CurrentErrors = [];
    $$._IsCapturingConditions = false;
    $$._IsCapturingCommands = false;
    $$._SavePromptCallback = null;
    $$._CurrentTab = 1;

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        page.on('pagehide', function (e) {
            $('[data-ui-field=homegenie_panel_button]').removeClass('ui-disabled');
        });
        page.on('pageshow', function (e) {
            $('[data-ui-field=homegenie_panel_button]').addClass('ui-disabled');
        });
        page.on('pagebeforeshow', function (e) {
            $('#automation_program_scriptsetup').next().css('display', '');
            $('#automation_program_scriptsource').next().css('display', '');
            $$.SetTab(1);
            $$.RefreshProgramEditorTitle();
            automationpage_ConditionsRefresh();
            automationpage_CommandsRefresh();
        });
        page.on('pageinit', function (e) {
            if (HOST_SYSTEM.substring(0, 3) == 'Win') {
                $('#automation_programtype_option_arduino').hide();
            }
            //
            $('#program_delete_button').bind('click', function (event) {
                $$.DeleteProgram($$._CurrentProgram.Address);
                return true;
            });
            //
            $('#editprograms_backbutton').on('click', function () {
                $$.CheckIsClean(function () {
                    $.mobile.pageContainer.pagecontainer('change', '#page_automation_programs');
                });
                return false;
            });
            $('#editprograms_homebutton').on('click', function () {
                $$.CheckIsClean(function () {
                    $.mobile.pageContainer.pagecontainer('change', '#page_control');
                });
                return false;
            });
            $('#program_notsaved_save').on('click', function () {
                $$.SaveProgram(function () {
                    $$._SavePromptCallback();
                });
                return false;
            });
            $('#program_notsaved_dontsave').on('click', function () {
                $$._SavePromptCallback();
                return false;
            });
            //
            $('#automation_program_delete_button').bind('click', function (event) {
                HG.Ui.SwitchPopup('#editprograms_actionmenu', '#automation_program_delete');
                return true;
            });
            //
            $('#configure_program_editorcompilecode').bind('click', function (event) {
                $$.CompileProgram();
                return true;
            });
            //
            $('#configure_program_editorcompilecode2').bind('click', function (event) {
                $$.CompileProgram();
                return true;
            });
            //
            $('#editprograms_actionmenu').on('popupbeforeposition', function (event) {
                $$.RefreshProgramOptions();
            });
            $('#editprograms_code_actionmenu').on('popupbeforeposition', function (event) {
                $$.RefreshProgramOptions();
            });
            //
            $('#automation_capture_condition_popup').popup().on('popupbeforeposition', function (event) {
                $$._IsCapturingConditions = true;
            });
            //
            $('#automation_capture_condition_popup').popup().on('popupafterclose', function (event) {
                $$._IsCapturingConditions = false;
            });
            //
            $('#automation_capture_command_popup').popup().on('popupbeforeposition', function (event) {
                $$._IsCapturingCommands = true;
            });
            //
            $('#automation_capture_command_popup').popup().on('popupafterclose', function (event) {
                $$._IsCapturingCommands = false;
            });
            //
            $('#automation_domain').on('popupbeforeposition', function (event) {
                $('#automation_conditiontarget li:gt(0)').remove();
                var domains = Array();
                for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++) {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain) {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if ($$.GetDomainComparableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
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
                for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++) {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain) {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if ($$.GetDomainComparableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
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
                for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                    var exists = false;
                    for (var d = 0; d < domains.length; d++) {
                        if (domains[d] == HG.WebApp.Data.Modules[m].Domain) {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) {
                        domains.push(HG.WebApp.Data.Modules[m].Domain);
                        if ($$.GetDomainControllableModules(HG.WebApp.Data.Modules[m].Domain, false).length == 0) continue;
                        //
                        var displayname = HG.WebApp.Data.Modules[m].Domain.substring(HG.WebApp.Data.Modules[m].Domain.lastIndexOf('.') + 1);
                        $('#automation_commandtarget').append('<li data-context-value="' + HG.WebApp.Data.Modules[m].Domain + '"><a data-rel="popup" href="#automation_command_target_popup">' + displayname + '</a></li>');
                    }
                }
                $('#automation_commandtarget').listview('refresh');//trigger('create');
            });
            //
            // Arduino Sketch files management
            $('#automation_program_listfiles').on('popupbeforeposition', function (event) {
                $$.SketchFileList();
            });
            $('#automation_program_sketchfiles_add').bind('click', function (event) {
                $('#programfile_new_name').val('');
                HG.Ui.SwitchPopup('#automation_program_listfiles', '#automation_program_fileadd', true);
            });
            $('#programfile_new_button').bind('click', function (event) {
                $('#automation_program_fileadd').popup('close');
                var filename = $('#programfile_new_name').val();
                $.mobile.loading('show', {
                    text: 'Adding file ' + filename,
                    textVisible: true,
                    theme: 'a',
                    html: ''
                });
                HG.Automation.Programs.ArduinoFileAdd($$._CurrentProgram.Address, filename, function (res) {
                    if (res == 'EXISTS') {
                        $.mobile.loading('show', {
                            text: 'A file named "' + filename + '" already exists',
                            textVisible: true,
                            theme: 'a',
                            html: ''
                        });
                        setTimeout(function () {
                            $.mobile.loading('hide');
                        }, 3000);
                    }
                    else if (res == 'INVALID_NAME') {
                        $.mobile.loading('show', {
                            text: 'Invalid file name "' + filename + '". Must ends with .c, .cpp, .h or have no extension.',
                            textVisible: true,
                            theme: 'a',
                            html: ''
                        });
                        setTimeout(function () {
                            $.mobile.loading('hide');
                        }, 3000);
                    }
                    else {
                        $.mobile.loading('hide');
                        $$.SketchFileOpen(filename);
                    }
                });
            });
            $('#automation_program_sketchfiles_edit').bind('click', function (event) {
                $('#automation_program_listfiles').popup('close');
                var filename = $('#automation_program_sketchfiles li a.ui-btn-active').attr('data-context-value');
                $$.SketchFileOpen(filename);
            });
            $('#automation_program_sketchfiles_delete').bind('click', function (event) {
                var filename = $('#automation_program_sketchfiles li a.ui-btn-active').attr('data-context-value');
                $.mobile.loading('show', {
                    text: 'Deleting file ' + filename,
                    textVisible: true,
                    theme: 'a',
                    html: ''
                });
                HG.Automation.Programs.ArduinoFileDelete($$._CurrentProgram.Address, filename, function (res) {
                    $$.SketchFileList();
                    if (filename == $$._CurrentSketchFile) {
                        $$.SketchFileOpen('main');
                    }
                    $.mobile.loading('hide');
                });
            });
        });
    };

    $$.CheckIsClean = function (callback) {
        if (!$$.IsClean()) {
            $$._SavePromptCallback = function () {
                callback();
            }
            $('#automation_program_notsaved').popup('open');
        } else {
            callback();
        }
    };
    $$.IsClean = function () {
        var isClean = ($$._CurrentProgram.Group === $('#automation_programgroup').val());
        isClean = isClean && ($$._CurrentProgram.Name === $('#automation_programname').val());
        isClean = isClean && ($$._CurrentProgram.Description === $('#automation_programdescription').val());
        isClean = isClean && ($$._CurrentProgram.AutoRestartEnabled === $('#automation_program_autorestartenabled').is(':checked') );
        if ($$._CurrentProgram.Type.toLowerCase() == 'wizard') {
            isClean = isClean && ($$._CurrentProgram.ConditionType === $('#automation_conditiontype').val());
        }
        isClean = isClean && editor1.isClean() && editor2.isClean() && editor3.isClean();
        // TODO: add checking of Wizard type programs Conditions and Commands too
        return isClean;
    };
    $$.GetDomainControllableModules = function (domain, showall) {
        var mods = Array();
        if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length) {
            for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                if (HG.WebApp.Data.Modules[m].Domain == domain) {
                    if ($$.IsModuleControllable(HG.WebApp.Data.Modules[m]) || showall) {
                        mods.push(HG.WebApp.Data.Modules[m]);
                    }
                }
            }
        }
        return mods;
    };

    $$.IsModuleControllable = function (module) {
        return (module.DeviceType != 'Generic' && module.DeviceType != 'Sensor' && module.DeviceType != 'DoorWindow' && module.DeviceType != 'Temperature')
            || (module.Address == "RF" || module.Address == "IR");
    };

    $$.GetDomainComparableModules = function (domain, showall) {
        var mods = Array();
        if (HG.WebApp.Data.Modules && HG.WebApp.Data.Modules.length) {
            for (m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                if (HG.WebApp.Data.Modules[m].Domain == domain) {
                    if ($$.GetModuleComparableProperties(HG.WebApp.Data.Modules[m]).length > 0 || showall) {
                        mods.push(HG.WebApp.Data.Modules[m]);
                    }
                }
            }
        }
        return mods;
    };

    $$.GetModuleComparableProperties = function (module) {
        var props = Array();
        for (var p = 0; p < module.Properties.length; p++) {
            var prop = module.Properties[p];
            if (prop.Name.substring(0, 17) == 'ConfigureOptions.' || prop.Name.substring(0, 14) == 'VirtualModule.' || prop.Name.substring(0, 7) == 'Widget.') continue;
            props.push(prop);
        }
        return props;
    };

    $$.RefreshProgramEditorTitle = function () {
        var editMode = 'wizard';
        if (typeof ($$._CurrentProgram.Type) != 'undefined') editMode = $$._CurrentProgram.Type.toLowerCase();
        if (editMode != 'wizard') {
            var errors = $$._CurrentProgram.ScriptErrors;
            if (typeof errors != 'undefined' && errors.trim() != '' && errors.trim() != '[]') {
                $$.ShowProgramErrors(errors);
            } else {
                $$.HideProgramErrors();
            }
        } else {
            $$.HideProgramErrors();
        }
        // update title
        var status = $$.GetProgramStatusColor($$._CurrentProgram);
        var statusImage = '<img src="images/common/led_' + status + '.png" style="width:24px;height:24px;vertical-align:middle;margin-bottom:5px;margin-right:5px;" /> ';
        $('#page_automation_program_title').html('<span style="font-size:9pt;font-weight:bold">PROGRAM EDITOR (' + editMode + ')</span><br />' + statusImage + $$._CurrentProgram.Address + ' ' + $$._CurrentProgram.Name);
    };

    $$.GetProgramStatusColor = function (prog) {
        var statusColor = 'black';
        var statusProperty = '';
        var hasErrors = (typeof (prog.Type) != 'undefined' && prog.Type.toLowerCase() != 'wizard' && typeof (prog.ScriptErrors) != 'undefined' && prog.ScriptErrors.trim() != '' && prog.ScriptErrors.trim() != '[]');
        //
        var module = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.HomeGenie.Automation', prog.Address);
        if (module != null) {
            var propObj = HG.WebApp.Utility.GetModulePropertyByName(module, 'Program.Status');
            if (propObj != null) statusProperty = propObj.Value;
        }
        //
        if (statusProperty == 'Running') {
            statusColor = 'green';
        } else if (statusProperty == 'Background') {
            statusColor = 'blue';
        } else if (prog.IsEnabled) {
            if (hasErrors)
                statusColor = 'red';
            else
                statusColor = 'yellow';
        } else if (hasErrors) {
            statusColor = 'brown';
        }
        //
        return statusColor;
    };

    $$.RefreshProgramOptions = function () {
        $('[id=editprograms_actionmenu_run]').each(function () {
            $(this).show();
        });
        $('[id=editprograms_actionmenu_break]').each(function () {
            $(this).hide();
        });
        $('[id=editprograms_actionmenu_run]').each(function () {
            $(this).addClass('ui-disabled');
        });
        $('[id=editprograms_actionmenu_compile]').each(function () {
            $(this).addClass('ui-disabled');
        });
        //
        setTimeout(function () {
            HG.Automation.Programs.List(function () {
                $('[id=editprograms_actionmenu_compile]').each(function () {
                    $(this).removeClass('ui-disabled');
                });
                $('[id=editprograms_actionmenu_run]').each(function () {
                    $(this).removeClass('ui-disabled');
                });
                $('[id=editprograms_actionmenu_run]').each(function () {
                    $(this).hide();
                });
                //
                var cp = HG.WebApp.Utility.GetProgramByAddress($$._CurrentProgram.Address);
                if (cp != null) {
                    if (cp.IsRunning) {
                        $('[id=editprograms_actionmenu_break]').each(function () {
                            $(this).show();
                        });
                    } else {
                        $('[id=editprograms_actionmenu_run]').each(function () {
                            $(this).show();
                        });
                    }
                    if (cp.ScriptErrors.trim() != '' && cp.ScriptErrors.trim() != '[]') {
                        $$._CurrentProgram.ScriptErrors = cp.ScriptErrors;
                    } else {
                        $$._CurrentProgram.ScriptErrors = '';
                    }
                    $$.RefreshProgramEditorTitle();
                }
            });
        }, 500);
    };

    $$.ProgramEnable = function (pid, isenabled) {
        var fn = (isenabled ? 'Enable' : 'Disable');
        var action = (isenabled ? 'Enabling' : 'Disabling');
        $.mobile.loading('show', {text: action + ' program', textVisible: true, theme: 'a', html: ''});
        $('#control_groupslist').empty();
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + fn + '/' + pid + '/',
            type: 'GET',
            success: function (response) {
                $.mobile.loading('hide');
            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
            }
        });
    };

    $$.UpdateProgram = function (programblock, compile, callback) {
        //$('#configure_program_editorruncode').addClass('ui-disabled');
        $('#configure_program_editorcompilecode').addClass('ui-disabled');
        $('#configure_program_editorcompilecode2').addClass('ui-disabled');
        $.mobile.loading('show', {
            text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_saving'),
            textVisible: true,
            theme: 'a',
            html: ''
        });
        $('#control_groupslist').empty();
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.' + (compile ? 'Compile' : 'Update') + '/',
            dataType: 'text',
            data: JSON.stringify(programblock),
            success: function (response) {
                $.mobile.loading('hide');
                editor1.markClean();
                editor2.markClean();
                editor3.markClean();
                $('#configure_program_editorcompilecode').removeClass('ui-disabled');
                $('#configure_program_editorcompilecode2').removeClass('ui-disabled');
                if (response.trim() != '' && response.trim() != '[]') {
                    $.mobile.loading('show', {
                        text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_error_updating'),
                        textVisible: true
                    });
                    $$.ShowProgramErrors(response);
                } else {
                    $.mobile.loading('show', {
                        text: HG.WebApp.Locales.GetLocaleString('configure_editprogram_saving_succeed'),
                        textVisible: true
                    });
                    $$.RefreshProgramEditorTitle();
                }
                setTimeout(function () {
                    $.mobile.loading('hide');
                    if (callback != null && typeof callback != 'undefined') callback();
                }, 2000);
                //
                // update module list from server
                //TODO: make this better...
                setTimeout(function () {
                    HG.Configure.Modules.List(function (data) {
                        //
                        try {
                            HG.WebApp.Data.Modules = data;
                        } catch (e) {
                        }
                    });
                }, 3000);

            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
                $('#configure_program_editorcompilecode').removeClass('ui-disabled');
                $('#configure_program_editorcompilecode2').removeClass('ui-disabled');
                //
                $.mobile.loading('show', {text: 'An error occurred!', textVisible: true});
                setTimeout(function () {
                    $.mobile.loading('hide');
                }, 5000);
            }
        });
    };

    $$.JumpToLine = function (blockType, position) {
        var editor = (blockType == 'TC' ? editor1 : editor2);
        if (blockType == 'TC') {
            $$.SetTab(3);
        } else {
            $$.SketchFileOpen('main');
            $$.SetTab(2);
        }
        window.setTimeout(function () {
            editor.setCursor(position);
            var height = editor.getScrollInfo().clientHeight;
            var coords = editor.charCoords(position, "local");
            editor.scrollTo(null, (coords.top + coords.bottom - height) / 2);
            editor.focus();
        }, 500);
    };

    $$.ShowProgramErrors = function (message) {
        $$._CurrentErrors = [];
        editor1.clearGutter('CodeMirror-lint-markers-1');
        editor2.clearGutter('CodeMirror-lint-markers-2');
        editor3.clearGutter('CodeMirror-lint-markers-3');
        //
        if (typeof (message) == 'undefined') message == '';
        $$._CurrentProgram.ScriptErrors = message;
        if (message == '') return;
        //
        for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
            if (HG.WebApp.Data.Programs[i].Address == $$._CurrentProgram.Address) {
                HG.WebApp.Data.Programs[i].ScriptErrors = message;
                break;
            }
        }
        //
        var errors = null;
        try {
            errors = eval(message);
        } catch (e) {
        }
        //
        if (errors != null) {
            $$._CurrentErrors = errors;
            var currentLine = 0, currentBlock = '', marker = null, message = '', popupMessage = '';
            for (var e = 0; e < $$._CurrentErrors.length; e++) {
                var err = $$._CurrentErrors[e];
                if (err.Line > 0) {
                    if (currentLine != err.Line || currentBlock != err.CodeBlock) {
                        if (marker != null) {
                            $(marker).qtip({
                                content: {title: 'Error', text: message, button: 'Close'},
                                show: {event: 'mouseover', solo: true},
                                hide: 'mouseout',
                                style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'}
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
                    message += '<b>(' + err.Line + ',' + err.Column + '):</b> ' + err.ErrorMessage + '<br/>';
                    popupMessage += '<b><a href="javascript:HG.WebApp.ProgramEdit.JumpToLine(\'' + err.CodeBlock + '\', { line: ' + (err.Line - 1) + ', ch: ' + (err.Column - 1) + ' })">Line ' + err.Line + ', Column ' + err.Column + '</a></b> (<font style="color:' + (err.CodeBlock == 'TC' ? 'yellow">Trigger' : 'lime">Code') + '</font>):<br/>';
                }
                popupMessage += '&nbsp;&nbsp;&nbsp;&nbsp;<em>' + err.ErrorMessage.replace(/\n/g, '<br/>&nbsp;&nbsp;&nbsp;&nbsp;') + '</em><br /><br />';
            }
            if (marker != null) {
                $(marker).qtip({
                    content: {title: 'Error', text: message, button: 'Close'},
                    show: {event: 'mouseover', solo: true},
                    hide: 'mouseout',
                    style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'}
                });
            }
            //
            // Build external file errors (editor3 used for external sketch files)
            //
            $$.ShowExternalErrors();
            //
            // Set message on "Errors" button
            if (popupMessage != '') {
                $('#program_error_button').show();
                $('#program_error_button2').show();
                //
                // message popup on "Error" button (Tab 2)
                $('#program_error_button').qtip({
                    content: {title: 'Error', text: popupMessage, button: 'Close'},
                    show: {
                        event: 'mouseover',
                        ready: ($$._CurrentTab == 2 ? true : false),
                        delay: 500
                    },
                    hide: {event: false, inactive: 5000},
                    style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                    position: {adjust: {screen: true}, my: 'top center', at: 'bottom center'}
                });
                //
                // message popup on "Error" button (Tab 3)
                $('#program_error_button2').qtip({
                    content: {title: 'Error', text: popupMessage, button: 'Close'},
                    show: {
                        event: 'mouseover',
                        ready: ($$._CurrentTab == 3 ? true : false),
                        delay: 500
                    },
                    hide: {event: false, inactive: 5000},
                    style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                    position: {adjust: {screen: true}, my: 'top center', at: 'bottom center'}
                });
            } else {
                $('#program_error_button').hide();
                $('#program_error_button2').hide();
            }
        } else {
            $$._CurrentErrors = [];
        }

        $$.RefreshCodeMirror();

    };

    $$.ShowExternalErrors = function () {
        editor3.clearGutter('CodeMirror-lint-markers-3');
        var currentLine = 0, currentBlock = '', marker = null, message = '';
        for (var l = 0; l < $$._CurrentErrors.length; l++) {
            var errors = $$._CurrentErrors[l].ErrorMessage.split('\n');
            for (var e = 0; e < errors.length; e++) {
                var err = errors[e];
                var lineParts = err.split(':');

                if (lineParts.length > 3 && lineParts[0] == $$._CurrentSketchFile && $.isNumeric(lineParts[1]) && $.isNumeric(lineParts[2])) {
                    if (currentLine != lineParts[1] || currentBlock != err.CodeBlock) {
                        if (marker != null) {
                            $(marker).qtip({
                                content: {title: 'Error', text: message, button: 'Close'},
                                show: {event: 'mouseover', solo: true},
                                hide: 'mouseout',
                                style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'}
                            });
                            message = '';
                        }
                        marker = document.createElement('div');
                        marker.className = 'CodeMirror-lint-marker-error';
                        editor3.setGutterMarker(lineParts[1] - 1, 'CodeMirror-lint-markers-3', marker);
                        currentLine = lineParts[1];
                        currentBlock = err.CodeBlock;
                    }
                    message += err + '<br/>';
                }
            }
        }
        if (marker != null) {
            $(marker).qtip({
                content: {title: 'Error', text: message, button: 'Close'},
                show: {event: 'mouseover', solo: true},
                hide: 'mouseout',
                style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'}
            });
        }
    };

    $$.HideProgramErrors = function () {
        $$._CurrentErrors = [];
        if (editor1 != null) editor1.clearGutter('CodeMirror-lint-markers-1');
        if (editor2 != null) editor2.clearGutter('CodeMirror-lint-markers-2');
        if (editor3 != null) editor3.clearGutter('CodeMirror-lint-markers-3');
        //$('#program_error_message_text').html('');
        $('#program_error_button').hide();
        $('#program_error_button2').hide();
        $('.qtip').hide();
        //$('#program_error_message').popup().popup('close');
        $$.RefreshCodeMirror();
    };

    $$.RefreshCodeMirror = function () {

        // refresh editors
        setTimeout(function () {
            if (editor1 != null) editor1.refresh();
            if (editor2 != null) editor2.refresh();
            if (editor3 != null) editor3.refresh();
        }, 500);

    };

    $$.CompileProgram = function () {
        $$.HideProgramErrors();
        var programblock = $$.SetProgramData();
        //
        if ($$._CurrentProgram.Type.toLowerCase() == 'arduino') {
            // save other opened sketch files before compiling
            $$.SketchFileSave(function () {
                $$.UpdateProgram(programblock, true);
            });
        } else {
            $$.UpdateProgram(programblock, true);
        }
    };
    $$.SaveProgram = function (callback) {
        $('#program_error_button').hide();
        $('#program_error_button2').hide();
        var programblock = $$.SetProgramData();
        switch ($$._CurrentProgram.Type.toLowerCase()) {
            case 'arduino':
                // save other opened sketch files before compiling
                $$.SketchFileSave(function () {
                    $$.UpdateProgram(programblock, false, callback);
                });
                break;
            case 'wizard':
                programblock.ScriptSource = JSON.stringify({
                    'ConditionType': $$._CurrentProgram.ConditionType,
                    'Conditions': $$._CurrentProgram.Conditions,
                    'Commands': $$._CurrentProgram.Commands
                });
            default:
                $$.UpdateProgram(programblock, true, callback);
        }
    };
    $$.SetProgramData = function () {
        HG.WebApp.AutomationGroupsList._CurrentGroup = $('#automation_programgroup').val();
        $$._CurrentProgram.Group = $('#automation_programgroup').val();
        $$._CurrentProgram.Name = $('#automation_programname').val();
        $$._CurrentProgram.Description = $('#automation_programdescription').val();
        $$._CurrentProgram.AutoRestartEnabled = $('#automation_program_autorestartenabled').is(':checked');
        $$._CurrentProgram.ScriptSetup = editor1.getValue(); //$('#automation_program_scriptsetup').val();
        $$._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
        $$._CurrentProgram.ScriptErrors = '';
        $$._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
        var programblock = {
            'Address': $$._CurrentProgram.Address,
            'Type': $$._CurrentProgram.Type,
            'Group': $$._CurrentProgram.Group,
            'Name': $$._CurrentProgram.Name,
            'Description': $$._CurrentProgram.Description,
            'AutoRestartEnabled': $$._CurrentProgram.AutoRestartEnabled,
            'IsEnabled': $$._CurrentProgram.IsEnabled,
            'ScriptSetup': $$._CurrentProgram.ScriptSetup,
            'ScriptSource': $$._CurrentProgram.ScriptSource
        };
        return programblock;
    };

    $$.CheckAndRunProgram = function (program) {
        $$._CurrentProgram.ScriptSetup = editor1.getValue(); //$('#automation_program_scriptsetup').val();
        $$._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
        $$._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
        if (!$$.IsClean()) {
            $$.SaveProgram(function () {
                $$.RunProgram(program.Address, null);
            });
        } else {
            $$.RunProgram(program.Address, null);
        }
    };

    $$.BreakProgram = function (pid) {
        $.mobile.loading('show', {text: 'Stopping program', textVisible: true, theme: 'a', html: ''});
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Automation/Programs.Break/' + pid + '/',
            type: 'GET',
            success: function (response) {
                $$.RefreshProgramOptions();
                $.mobile.loading('hide');
            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
            }
        });

    };

    $$.RunProgram = function (pid, options) {
        $$._CurrentProgram.ScriptSetup = editor1.getValue(); //$('#automation_program_scriptsetup').val();
        $$._CurrentProgram.ScriptSource = editor2.getValue(); //$('#automation_program_scriptsource').val();
        $$._CurrentProgram.ConditionType = $('#automation_conditiontype').val();
        //
        $.mobile.loading('show', {text: 'Running program', textVisible: true, theme: 'a', html: ''});
        HG.Automation.Programs.Run(pid, options, function (res) {
            if (res != null) $$.RefreshProgramOptions();
            $.mobile.loading('hide');
        });
    };

    $$.DeleteProgram = function (program) {
        $.mobile.loading('show', {text: 'Deleting program', textVisible: true, theme: 'a', html: ''});
        HG.Automation.Programs.DeleteProgram(program, function () {
            $.mobile.loading('hide');
            setTimeout(function () {
                $.mobile.changePage($('#page_automation_programs'), {transition: 'fade', changeHash: true});
            }, 200);
        });
    };

    $$.SetTab = function (tabindex) {
        $$._CurrentTab = tabindex;
        $('#program_edit_tab1').hide();
        $('#program_edit_tab2').hide();
        $('#program_edit_tab3').hide();
        $('#program_edit_tab1_button').removeClass('ui-btn-active');
        $('#program_edit_tab2_button').removeClass('ui-btn-active');
        $('#program_edit_tab3_button').removeClass('ui-btn-active');
        $('#program_edit_tab' + tabindex).show();
        $('#program_edit_tab' + tabindex + '_button').addClass('ui-btn-active');
        //
        $$.RefreshCodeMirror();
    };

    // Arduino Sketch File management
    $$.SketchFileSelect = function (el) {
        $('#automation_program_sketchfiles li a').removeClass('ui-btn-active');
        $(el).addClass('ui-btn-active');
        $('#automation_program_sketchfiles_edit').removeClass('ui-disabled');
        $('#automation_program_sketchfiles_delete').addClass('ui-disabled');
        if ($(el).attr('data-context-value') != 'main') {
            $('#automation_program_sketchfiles_delete').removeClass('ui-disabled');
        }
    };
    $$.SketchFileOpen = function (filename) {
        // first save any other currently opened file
        $$.SketchFileSave(function () {
            $.mobile.loading('show', {text: 'Opening file ' + filename, textVisible: true, theme: 'a', html: ''});
            if (filename == null || typeof (filename) == 'undefined' || filename == '' || filename == 'main') {
                // the main sketch file is stored in standard code editor (editor2)
                $$._CurrentSketchFile = '';
                $('#configure_program_editorfilename').html(filename);
                $(editor3.getWrapperElement()).hide();
                $(editor2.getWrapperElement()).show();
                editor2.clearHistory();
                editor2.markClean();
                editor2.refresh();
                $.mobile.loading('hide');
            } else {
                // all other sketch files are stored in editor3
                $(editor2.getWrapperElement()).hide();
                $(editor3.getWrapperElement()).show();
                // load specified file into editor
                HG.Automation.Programs.ArduinoFileLoad($$._CurrentProgram.Address, filename, function (src) {
                    $$._CurrentSketchFile = filename;
                    $('#configure_program_editorfilename').html(filename);
                    editor3.setValue(src);
                    editor3.clearHistory();
                    editor3.markClean();
                    editor3.refresh();
                    $$.ShowExternalErrors();
                    $.mobile.loading('hide');
                });
            }
        });
    };
    $$.SketchFileSave = function (callback) {
        if ($$._CurrentSketchFile == '') {
            if (callback != null) callback();
            return;
        }
        $.mobile.loading('show', {
            text: 'Saving file ' + $$._CurrentSketchFile,
            textVisible: true,
            theme: 'a',
            html: ''
        });
        var srcfile = editor3.getValue();
        HG.Automation.Programs.ArduinoFileSave(
            $$._CurrentProgram.Address,
            $$._CurrentSketchFile,
            srcfile,
            function (src) {
                $.mobile.loading('hide');
                if (callback != null) callback();
            }
        );
    };
    $$.SketchFileList = function () {
        $('#automation_program_sketchfiles_edit').addClass('ui-disabled');
        $('#automation_program_sketchfiles_delete').addClass('ui-disabled');
        HG.Automation.Programs.ArduinoFileList($$._CurrentProgram.Address, function (list) {
            $('#automation_program_sketchfiles').empty();
            $('#automation_program_sketchfiles').append('<li data-icon="false"><a ondblclick="HG.WebApp.ProgramEdit.SketchFileOpen(\'main\');$(\'#automation_program_listfiles\').popup(\'close\');" onclick="HG.WebApp.ProgramEdit.SketchFileSelect(this)" href="#" data-context-value="main"><strong>Main Sketch Code</strong></a></li>');
            if (typeof (list) != 'undefined' && list != null)
                for (var f = 0; f < list.length; f++) {
                    $('#automation_program_sketchfiles').append('<li data-icon="false"><a data-context-value="' + list[f] + '" ondblclick="HG.WebApp.ProgramEdit.SketchFileOpen(\'' + list[f] + '\');$(\'#automation_program_listfiles\').popup(\'close\');" onclick="HG.WebApp.ProgramEdit.SketchFileSelect(this)" href="#">' + list[f] + '</a></li>');
                }
            $('#automation_program_sketchfiles').listview('refresh');
        });
    };
};
