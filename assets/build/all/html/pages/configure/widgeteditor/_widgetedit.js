HG.WebApp.WidgetEditor = HG.WebApp.WidgetEditor || new function () { var $$ = this;

    // TODO: $.extend(this, new HG.Ui.Page())
    // and use $$.field(name) method instead of jQuery.find

    $$.PageId = 'page_widgeteditor_editwidget';
    $$._hasError = false;
    $$._editorHtml = null;
    $$._editorJscript = null;
    $$._widgetInstance = null;
    $$.previewWidth = 420; //245;
    $$._splitDragStartX = 0;
    $$._savePromptCallback = null;

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        var runPreviewButton = page.find('[data-ui-field=preview-btn]');
        var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
        var editParamsButton = page.find('[data-ui-field=module-params-edit]');
        var errorsButton = page.find('[data-ui-field=errors-btn]');
        var saveButton = page.find('[data-ui-field=save-btn]');
        var exportButton = page.find('[data-ui-field=export-btn]');
        var deleteButton = page.find('[data-ui-field=delete-btn]');
        var deleteConfirmButton = page.find('[data-ui-field=deleteconfirm-btn]');
        var deletePopup = page.find('[data-ui-field=delete-popup]');
        var previewPanel = page.find('[data-ui-field=preview-panel]');
        var splitBar = page.find('[data-ui-field=split-bar]');
        var backButton = page.find('[data-ui-field=back-btn]');
        var homeButton = page.find('[data-ui-field=home-btn]');
        var notSavedPopup = page.find('[data-ui-field=notsaved-popup]');
        var saveConfirmButton = page.find('[data-ui-field=saveconfirm-btn]');
        var saveCancelButton = page.find('[data-ui-field=savecancel-btn]');

        page.on('pagehide', function (e) {
            $('[data-ui-field=homegenie_panel_button]').removeClass('ui-disabled');
        });
        page.on('pageshow', function (e) {
            $('[data-ui-field=homegenie_panel_button]').addClass('ui-disabled');
        });
        page.on('pageinit', function (e) {
            $$._editorHtml = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_html'), {
                lineNumbers: true,
                matchBrackets: true,
                autoCloseBrackets: true,
                extraKeys: {
                    "Ctrl-S": function (cm) {
                        $$.SaveWidget(function () {
                            $$._editorHtml.markClean();
                            $$._editorJscript.markClean();
                        });
                    },
                    "Ctrl-Q": function (cm) {
                        cm.foldCode(cm.getCursor());
                    },
                    "Ctrl-Space": "autocomplete"
                },
                foldGutter: true,
                gutters: ["CodeMirror-lint-markers-4", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
                highlightSelectionMatches: {showToken: /\w/},
                mode: "text/html",
                matchTags: {bothTags: true},
                theme: 'ambiance'
            });
            $$._editorJscript = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_javascript'), {
                lineNumbers: true,
                matchBrackets: true,
                autoCloseBrackets: true,
                extraKeys: {
                    "Ctrl-S": function (cm) {
                        $$.SaveWidget(function () {
                            $$._editorHtml.markClean();
                            $$._editorJscript.markClean();
                        });
                    },
                    "Ctrl-Q": function (cm) {
                        cm.foldCode(cm.getCursor());
                    },
                    "Ctrl-Space": "autocomplete"
                },
                foldGutter: true,
                gutters: ["CodeMirror-lint-markers-5", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
                highlightSelectionMatches: {showToken: /\w/},
                mode: "text/javascript",
                theme: 'ambiance'
            });
            deletePopup.popup();
            notSavedPopup.popup();
        });
        page.on('pagebeforeshow', function (e) {
            page.find('[data-ui-field=title-heading]').html('<span style="font-size:10pt;font-weight:bold">' + HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_title', false, this.Locale) + '</span><br/>' + HG.WebApp.WidgetsList._currentWidget);
            // standard editor/preview size
            page.find('.CodeMirror').css('right', ($$.previewWidth + 5));
            splitBar.css('right', $$.previewWidth);
            previewPanel.width($$.previewWidth);
            // load widget html/js
            $.ajax({
                url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._currentWidget + '.html',
                type: 'GET',
                dataType: 'text',
                success: function (data) {
                    $$._editorHtml.setValue(data);
                    $$._editorHtml.clearHistory();
                    $$._editorHtml.markClean();
                    $.ajax({
                        url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._currentWidget + '.js',
                        type: 'GET',
                        dataType: 'text',
                        success: function (data) {
                            $$._editorJscript.setValue(data);
                            $$._editorJscript.clearHistory();
                            $$._editorJscript.markClean();
                            $$.RefreshCodeMirror();
                        }
                    });
                }
            });

            // initially the user have to press Run/Preview button in order to activate the widget
            page.find('[data-ui-field=preview-div]').empty();
            $$._hasError = true;
            // populate "bind module" select menu
            var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
            bindModuleSelect.empty();
            bindModuleSelect.append('<option value="">' + HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_selectmodule_placeholder', false, this.Locale) + '</option>');
            var selected = '', selectedDomain = '';
            for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                var module = HG.WebApp.Data.Modules[m];
                var widget = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayModule');
                if (widget != null) widget = widget.Value;
                var devType = module.DeviceType.toLowerCase();
                var widgetType = HG.WebApp.WidgetsList._currentWidget.toLowerCase().split('/');
                widgetType = widgetType[widgetType.length - 1];
                if (widget == HG.WebApp.WidgetsList._currentWidget || widgetType == devType) {
                    selected = m;
                    // prefer modules to programs as default bind module
                    if (module.Domain != 'HomeAutomation.HomeGenie.Automation') break;
                }
            }
            // if no valid bind module has been found then put in the list all module
            // otherwise only those having a matching widget
            for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
                var module = HG.WebApp.Data.Modules[m];
                var name = module.Name.trim();
                if (name == '') {
                    name = module.Domain + ':' + module.Address;
                }
                var widget = HG.WebApp.Utility.GetModulePropertyByName(module, 'Widget.DisplayModule');
                if (widget != null) widget = widget.Value;
                var devType = module.DeviceType.toLowerCase();
                var widgetType = HG.WebApp.WidgetsList._currentWidget.toLowerCase().split('/');
                widgetType = widgetType[widgetType.length - 1];
                if (widget == HG.WebApp.WidgetsList._currentWidget || widgetType == devType || selected == '') {
                    bindModuleSelect.append('<option value="' + m + '">' + name + '</option>');
                }
            }
            bindModuleSelect.change(function() {
                $$.Run();
            });
            bindModuleSelect.trigger('create');
            bindModuleSelect.val(selected);
            bindModuleSelect.selectmenu('refresh');
            $$.SetTab(1);
            if (selected != '' || selected == '0') {
                setTimeout(function () {
                    $$.Run();
                }, 1000);
            } else {
                bindModuleSelect.qtip({
                    content: {
                        title: HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_nobindmodule_title', 'Select a module'),
                        text: HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_nobindmodule_text', 'No valid bind-module has been found, please select one.'),
                        button: HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_nobindmodule_close', 'Close')
                    },
                    show: {event: false, ready: true, delay: 1000},
                    events: {
                        hide: function () {
                            $(this).qtip('destroy');
                        }
                    },
                    hide: {event: false, inactive: 3000},
                    style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
                    position: {my: 'bottom center', at: 'top center'}
                });
            }
        });

        saveButton.bind('click', function () {
            $('#editwidget_actionmenu').popup('close');
            // save html and javascript
            $$.SaveWidget(function () {
                $$._editorHtml.markClean();
                $$._editorJscript.markClean();
            });
        });

        exportButton.bind('click', function () {
            $('#editwidget_actionmenu').popup('close');
            // export current widget
            window.open(location.protocol + '../../api/HomeAutomation.HomeGenie/Config/Widgets.Export/' + encodeURIComponent(HG.WebApp.WidgetsList._currentWidget) + '/');
        });

        deleteButton.bind('click', function () {
            HG.Ui.SwitchPopup('#editwidget_actionmenu', deletePopup);
        });
        deleteConfirmButton.bind('click', function () {
            $.mobile.loading('show', {text: 'Deleting Widget...', textVisible: true, theme: 'a', html: ''});
            HG.Configure.Widgets.Delete(HG.WebApp.WidgetsList._currentWidget, function (res) {
                $.mobile.loading('hide');
                $.mobile.pageContainer.pagecontainer('change', '#' + HG.WebApp.WidgetsList.PageId);
            });
            return false;
        });

        backButton.bind('click', function () {
            $$.CheckIsClean(function () {
                $.mobile.pageContainer.pagecontainer('change', '#' + HG.WebApp.WidgetsList.PageId);
            });
            return false;
        });
        homeButton.bind('click', function () {
            $$.CheckIsClean(function () {
                $.mobile.pageContainer.pagecontainer('change', '#page_control');
            });
            return false;
        });
        saveCancelButton.bind('click', function () {
            $$._savePromptCallback();
            return false;
        });
        saveConfirmButton.bind('click', function () {
            $$.SaveWidget(function () {
                $$._savePromptCallback();
            });
            return false;
        });

        runPreviewButton.bind('click', function () {
            $$.Run();
        });
        bindModuleSelect.on('change', function () {
            if ($(this).val() == '')
                editParamsButton.addClass('ui-disabled');
            else
                editParamsButton.removeClass('ui-disabled');
            $$.RenderView();
        });
        editParamsButton.on('click', function () {
            if (bindModuleSelect.val() != '') {
                var module = HG.WebApp.Data.Modules[bindModuleSelect.val()];
                HG.WebApp.Control.EditModuleParams(module);
            }
        });
        errorsButton.hide();

        splitBar.mousedown(function (event) {
                $$._splitDragStartX = event.pageX;
                $(window).mousemove(function (ev) {
                    var deltaX = $$._splitDragStartX - ev.pageX;
                    var maxHeight = page.width() / 2;
                    var newHeight = (previewPanel.width() + deltaX);
                    if (newHeight >= 5 && newHeight <= maxHeight) {
                        previewPanel.width((previewPanel.width() + deltaX));
                        page.find('.CodeMirror').css('right', (previewPanel.width() + 5));
                        splitBar.css('right', previewPanel.width());
                        $$._splitDragStartX = ev.pageX;
                    } else {
                        $(window).unbind("mousemove");
                        $$._editorHtml.refresh();
                        $$._editorJscript.refresh();
                    }
                });
            })
            .mouseup(function () {
                $(window).unbind("mousemove");
                $$._editorHtml.refresh();
                $$._editorJscript.refresh();
            });

        window.onerror = function (msg, url, line, col, error) {
            if (url.indexOf('#' + $$.PageId) > 0) {
                $$.ShowError(error);
            }
            else {
                throw error;
            }
        };
    };

    $$.SetTab = function (tabIndex) {
        var page = $('#' + $$.PageId);
        page.find('[data-ui-field=tab1-div]').hide();
        page.find('[data-ui-field=tab2-div]').hide();
        page.find('[data-ui-field=tab3-div]').hide();
        page.find('[data-ui-field=tab1-btn]').removeClass('ui-btn-active');
        page.find('[data-ui-field=tab2-btn]').removeClass('ui-btn-active');
        page.find('[data-ui-field=tab3-btn]').removeClass('ui-btn-active');
        page.find('[data-ui-field=tab' + tabIndex + '-div]').show();
        page.find('[data-ui-field=tab' + tabIndex + '-btn]').addClass('ui-btn-active');
        $$.RefreshCodeMirror();
    };

    $$.CheckIsClean = function (callback) {
        if (!$$._editorHtml.isClean() || !$$._editorJscript.isClean()) {
            var page = $('#' + $$.PageId);
            $$._savePromptCallback = function () {
                callback();
            }
            page.find('[data-ui-field=notsaved-popup]').popup('open');
        } else {
            callback();
        }
    };

    $$.SaveWidget = function (callback) {
        $$.SaveWidgetHtml(function () {
            $$.SaveWidgetJavascript(function () {
                $$.Run();
                if (callback) callback();
            });
        });
    };
    $$.SaveWidgetHtml = function (callback) {
        $.mobile.loading('show', {
            text: HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_savinghtml', false, this.Locale),
            textVisible: true,
            theme: 'a',
            html: ''
        });
        HG.Configure.Widgets.Save(HG.WebApp.WidgetsList._currentWidget, 'html', $$._editorHtml.getValue(), function (res) {
            $.mobile.loading('hide');
            if (callback) callback();
        });
    };
    $$.SaveWidgetJavascript = function (callback) {
        $.mobile.loading('show', {
            text: HG.WebApp.Locales.GetLocaleString('configure_widgeteditor_savingjavascript', false, this.Locale),
            textVisible: true,
            theme: 'a',
            html: ''
        });
        HG.Configure.Widgets.Save(HG.WebApp.WidgetsList._currentWidget, 'js', $$._editorJscript.getValue(), function (res) {
            $.mobile.loading('hide');
            if (callback) callback();
        });
    };

    $$.RefreshCodeMirror = function () {
        setTimeout(function () {
            $$._editorHtml.refresh();
            $$._editorJscript.refresh();
        }, 500);
    };

    $$.Render = function () {
        var page = $('#' + $$.PageId);
        var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
        var errorsButton = page.find('[data-ui-field=errors-btn]');
        errorsButton.hide();
        var htmlCode = '<div id="widget_preview_instance" data-ui-field="preview-wrapper-div" align="left" style="display:table-cell">';
        htmlCode += $$._editorHtml.getValue();
        htmlCode += '</div>';
        page.find('[data-ui-field=preview-div]').html(htmlCode);
        page.find('[data-ui-field=preview-wrapper-div]').trigger('create');
    };

    $$.RenderView = function (eventData) {
        if ($$._hasError) return;
        var page = $('#' + $$.PageId);
        var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
        var module = HG.WebApp.Data.Modules[bindModuleSelect.val()];
        if (eventData != null && module != null && (eventData.Domain != module.Domain || eventData.Source != module.Address))
            return;
        $$.RenderWidget('#widget_preview_instance', $$._widgetInstance, module, eventData);
    };

    $$.RenderWidget = function (cuid, widgetInstance, module, eventData) {
        try {
            if (widgetInstance.v2) {
                if (typeof widgetInstance._bind == 'function') {
                    widgetInstance._bind(cuid, module);
                    widgetInstance._bind = null;
                } else {
                    widgetInstance.setModule(module);
                }
                if (typeof widgetInstance.onStart == 'function' && !widgetInstance._started) {
                    widgetInstance.onStart();
                    widgetInstance._started = true;
                    if (typeof widgetInstance.onRefresh == 'function')
                        widgetInstance.onRefresh();
                }
                if (typeof eventData != 'undefined' && typeof eventData.Property != 'undefined' && typeof widgetInstance.onUpdate == 'function')
                    widgetInstance.onUpdate(eventData.Property, eventData.Value);
                else if (typeof widgetInstance.onRefresh == 'function')
                    widgetInstance.onRefresh();
            } else {
                widgetInstance.RenderView(cuid, module);
            }
        } catch (e) {
            console.log(e);
            $$._hasError = true;
            $$.ShowError(e);
        }
    };

    $$.GetInstance = function (javascriptCode) {
        if (!javascriptCode.trim().startsWith('[')) {
            var commonJs = "";
            commonJs += "    var $$ = this;";
            commonJs += "    $$._fieldCache = [];";
            commonJs += "    $$.v2 = true;";
            commonJs += "    $$.apiCall = HG.Control.Modules.ServiceCall;";
            commonJs += "    $$.locales = HG.WebApp.Locales;";
            commonJs += "    $$.util = HG.WebApp.Utility;";
            commonJs += "    $$.ui = HG.Ui;";
            commonJs += "    $$.signalActity = function(fieldName) {";
            commonJs += "      if (typeof fieldName != 'undefined' && fieldName != '')";
            commonJs += "        $$.ui.BlinkAnim($$.field(fieldName));";
            commonJs += "      if ($$.field('led').length) {";
            commonJs += "          $$.field('led').attr('src', 'images/common/led_green.png');";
            commonJs += "          setTimeout(function() {";
            commonJs += "            $$.field('led').attr('src', 'images/common/led_black.png');";
            commonJs += "          }, 100);";
            commonJs += "      }";
            commonJs += "    };";
            commonJs += "    $$.field = function(field, globalSearch){";
            commonJs += "        var f = globalSearch ? '@'+field : field;";
            commonJs += "        var el = null;";
            commonJs += "        if (typeof $$._fieldCache[f] == 'undefined') {";
            commonJs += "            el = globalSearch ? $(field) : $$._widget.find('[data-ui-field='+field+']');";
            commonJs += "            if (el.length)";
            commonJs += "                $$._fieldCache[f] = el;";
            commonJs += "        } else {";
            commonJs += "            el = $$._fieldCache[f];";
            commonJs += "        }";
            commonJs += "        return el"; 
            commonJs += "    };";
            commonJs += "    $$.clearCache = function() {";
            commonJs += "        $$._fieldCache.length = 0;";
            commonJs += "    };";
            commonJs += "    $$.setModule = function(module) {";
            commonJs += "        $$.module = module;";
            commonJs += "        if ($$.module) {";
            commonJs += "            $$.module.prop = function(propName, value) {";
            commonJs += "                var p = HG.WebApp.Utility.GetModulePropertyByName(this, propName);";
            commonJs += "                if (typeof value != 'undefined')";
            commonJs += "                    p.Value = value;";
            commonJs += "                return p;";
            commonJs += "            };";
            commonJs += "            $$.module.command = function(cmd, opt, callback) {";
            commonJs += "                HG.Control.Modules.ServiceCall(cmd, this.Domain, this.Address, opt, function (response) { ";
            commonJs += "                    if (typeof callback == 'function')";
            commonJs += "                        callback(response);";
            commonJs += "                });";
            commonJs += "            };";
            commonJs += "        }";
            commonJs += "    };";
            commonJs += "    $$._bind = function(cuid, module) {";
            commonJs += "        $$.setModule(module);";
            commonJs += "        $$.container = $(cuid);";
            commonJs += "        $$.popup = $$.container.find('[data-ui-field=controlpopup]');";
            commonJs += "        $$.popup.popup();";
            commonJs += "        $$.popup.trigger('create');";
            commonJs += "        $$.popup.field = function(f){ return $$.popup.find('[data-ui-field='+f+']'); };";
            commonJs += "        $$._widget = $$.container.find('[data-ui-field=widget]');";
            commonJs += "        $$._widget.data('ControlPopUp', $$.popup);";
            commonJs += "    };";
            commonJs = commonJs.replace(/(\r\n|\n|\r)/gm, "");
            javascriptCode = 'new function(){' + commonJs + javascriptCode + '}';
            return eval(javascriptCode);
        } else {
            // old widget json format
            return eval(javascriptCode)[0];
        }
    };

    $$.Run = function () {
        $$._hasError = false;
        $$._editorJscript.clearGutter('CodeMirror-lint-markers-5');
        // create widget instance
        var javascriptCode = $$._editorJscript.getValue();
        $.mobile.loading('show', {text: 'Checking Javascript code...', textVisible: true, theme: 'a', html: ''});
        HG.Configure.Widgets.Parse(javascriptCode, function (msg) {
            $.mobile.loading('hide');
            if (msg.ResponseValue !== 'OK' && msg.Status !== 'Ok') {
                var message = msg.ResponseValue || msg.Message;
                var position = message.substr(message.indexOf('(') + 1);
                position = position.substr(0, position.indexOf(')')).split(',');
                message = message.substr(message.indexOf(':') + 2);
                message = message + '<br/> <a href="javascript:HG.WebApp.WidgetEditor.JumpToLine({ line: ' + (position[0] - 1) + ', ch: ' + (position[1] - 1) + ' })">Line <strong>' + position[0] + '</strong>, Column <strong>' + position[1] + '</strong></a>';
                $$.ShowErrorTip(message, position[0]);
            } else {
                try {
                    $$._widgetInstance = $$.GetInstance(javascriptCode);
                    // render HTML
                    $$.Render();
                    // execute widget RenderView method
                    $$.RenderView();
                } catch (e) {
                    $$._hasError = true;
                    $$.ShowError(e);
                }
            }
        });
    };

    $$.ShowError = function (e) {
        var stack = ErrorStackParser.parse(e);
        if (navigator.userAgent.toLowerCase().indexOf('firefox') > -1) {
            // FireFox already gives lineNumber and columnNumber properties in error object
            stack[0] = e;
        }
        var message = e + '<br/> <a href="javascript:HG.WebApp.WidgetEditor.JumpToLine({ line: ' + (stack[0].lineNumber - 1) + ', ch: ' + (stack[0].columnNumber - 1) + ' })">Line <strong>' + stack[0].lineNumber + '</strong>, Column <strong>' + stack[0].columnNumber + '</strong></a>';
        $$.ShowErrorTip(message, stack[0].lineNumber);
        console.log(message);
        console.log(stack);
    };

    $$.ShowErrorTip = function (message, lineNumber) {
        if ($$._editorJscript == null) return;
        var page = $('#' + $$.PageId);
        var errorsButton = page.find('[data-ui-field=errors-btn]');
        var marker = document.createElement('div');
        $$.SetTab(2);
        $$._editorJscript.clearGutter('CodeMirror-lint-markers-5');
        marker.className = 'CodeMirror-lint-marker-error';
        $$._editorJscript.setGutterMarker(lineNumber - 1, 'CodeMirror-lint-markers-5', marker);
        $(marker).qtip({
            content: {title: 'Error', text: message, button: 'Close'},
            show: {event: 'mouseover', solo: true},
            hide: 'mouseout',
            style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'}
        });
        errorsButton.show();
        errorsButton.qtip({
            content: {title: 'Error', text: message, button: 'Close'},
            show: {event: 'mouseover', ready: true, delay: 500},
            hide: {event: false, inactive: 5000},
            style: {classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap'},
            position: {adjust: {screen: true}, my: 'top center', at: 'bottom center'}
        });
    };

    $$.JumpToLine = function (position) {
        window.setTimeout(function () {
            $$._editorJscript.setCursor(position);
            var myHeight = $$._editorJscript.getScrollInfo().clientHeight;
            var coords = $$._editorJscript.charCoords(position, "local");
            $$._editorJscript.scrollTo(null, (coords.top + coords.bottom - myHeight) / 2);
            $$._editorJscript.focus();
        }, 500);
    };

};
