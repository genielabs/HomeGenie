HG.WebApp.Control = HG.WebApp.Control || new function() { var $$ = this;

    $$._widgetConfiguration = [];
    $$._widgetList = [];
    $$._placeHolder = $('<div class="freewall" style="background: white; opacity: 0.2; position: absolute;" data-index="-1"></div>');
    $$._grid = null;

    $$.InitializePage = function () {
        var page = $$.getContainer();
        page.on('pageinit', function (e) {
            $$.toolbarMacroRecord = $('#toolbar_macrorecord');
            $$.toolbarMacroRecord.hide();
            $$.toolbarWidgetsSort = $('#toolbar_widgetssort');
            $$.toolbarWidgetsSort.hide();
            $$.toolbarControl = $('#toolbar_control');
            $$.toolbarControl.show();
            $$.recordDelayNone = $('#macrorecord_delay_none');
            $$.recordDelayMimic = $('#macrorecord_delay_mimic');
            $$.recordDelayFixed = $('#macrorecord_delay_fixed');
            $('#control_macrorecord_optionspopup').bind('popupafterclose', function () {
                if ($$.recordDelayNone.prop('checked')) {
                    HG.Automation.Macro.SetDelay('None', '');
                }
                else if ($$.recordDelayMimic.prop('checked')) {
                    HG.Automation.Macro.SetDelay('Mimic', '');
                }
                else if ($$.recordDelayFixed.prop('checked')) {
                    HG.Automation.Macro.SetDelay('Fixed', $$.field('#macrorecord_delay_seconds', true).val());
                }
            });
            $$.controlGroupSelect = $('#control_groupselect');
            $$.controlGroupSelect.change(function () {
                var gid = $$.controlGroupSelect.val();
                $$.ShowGroup(gid);
            });
            $.ajax({
                url: "pages/control/widgets/configuration.json",
                type: 'GET',
                success: function (data) {
                    $$._widgetConfiguration = eval(data);
                },
                error: function (data) {
                    alert('error loading widgets configuration');
                }
            });
        });
        page.on('pagehide', function (e) {
            widgetsloadqueue = [];
            // hide wallpaper
            $$.field('div[data-ui-field="wallpaper"]', true).hide();
        });
        page.on('pagebeforeshow', function (e) {
            $$.clearCache();
            $.mobile.loading('show');
            HG.Configure.Groups.List('Control', function ()
            {
                if ($$.field('#control_groupslist', true).children().length == 0)
                {
                    $$.RenderGroups();
                }
                $$.UpdateModules();
            });
            HG.Automation.Macro.GetDelay(function(data){
                $$.recordDelayNone.prop('checked', false).checkboxradio( 'refresh' );
                $$.recordDelayMimic.prop('checked', false).checkboxradio( 'refresh' );
                $$.recordDelayFixed.prop('checked', false).checkboxradio( 'refresh' );
                $$.field('#macrorecord_delay_' + data.DelayType.toLowerCase(), true).prop('checked', true).checkboxradio( 'refresh' );
                $$.field('#macrorecord_delay_seconds', true).val(data.DelayOptions);
            });
        });
        $$.field('#control_bottombar_voice_button', true).on('click', function() {
            var btn = $(this);
            var speechInput = $$.field('#speechinput', true);
            if (speechInput.css('display') == 'none') {
                btn.removeClass('ui-icon-carat-u');
                btn.addClass('ui-icon-carat-d');
                speechInput.show(150);
                speechInput.find('input').focus();
            } else {
                btn.removeClass('ui-icon-carat-d');
                btn.addClass('ui-icon-carat-u');
                speechInput.hide(150);
                speechInput.find('input').blur();
            }
            setTimeout(function(){btn.removeClass('ui-btn-active');}, 200);
        });
        $$.field('#voicerecognition_text', true).on('change', function() {
            HG.VoiceControl.InterpretInput($(this).val());
        }).on('keyup', function(event) {
            if(event.keyCode == 13) {
                HG.VoiceControl.InterpretInput($(this).val());
            }
        });
        $$.field('#groups_panel', true).panel().trigger('create');
    };

    $$.ShowGroup = function (gid) {
        $.mobile.loading('show');
        HG.WebApp.Data._CurrentGroup = HG.WebApp.GroupModules.CurrentGroup = HG.WebApp.Data.Groups[gid].Name;
        HG.WebApp.Data._CurrentGroupIndex = gid;
        // set current group wallpaper
        $$.field('div[data-ui-field="wallpaper"]', true).show();
        if (typeof HG.WebApp.Data.Groups[gid].Wallpaper != 'undefined' && HG.WebApp.Data.Groups[gid].Wallpaper != '')
            $$.field('div[data-ui-field="wallpaper"]', true).css('background-image', 'url(images/wallpapers/'+HG.WebApp.Data.Groups[gid].Wallpaper+')');
        else
            $$.field('div[data-ui-field="wallpaper"]', true).css('background-image', '');
        $$.RefreshGroupIndicators();
        $$.field('#control_groupcontent', true).children('div').hide();
        $$.field('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex, true).show();
        setTimeout(function(){ $$.RenderGroupModules(gid); }, 500);
    };

    $$.UpdateModules = function () {
        $.mobile.loading('show');
        HG.Configure.Modules.List(function (data) {
            //
            try {
                HG.WebApp.Data.Modules = data;
            } catch (e) { }
            //
            HG.Automation.Programs.List(function () {
                $.mobile.loading('hide');
                $$.ShowGroup(HG.WebApp.Data._CurrentGroupIndex);
            });
        });
    };

    $$.ConfigureGroup = function () {
        HG.WebApp.GroupsList.ConfigureGroup(HG.WebApp.Data._CurrentGroupIndex);
    };

    $$.ShowAltToolbar = function(callback) {
        $$.field('#control_actionmenu', true).popup('close');
        $('[data-ui-field=homegenie_panel_button]').addClass('ui-disabled');
        $$.toolbarControl.hide('slidedown', callback);
    };
    $$.HideAltToolbar = function(callback) {
        $('[data-ui-field=homegenie_panel_button]').removeClass('ui-disabled');
        $$.toolbarControl.show('slideup', callback);
    };

    $$.RecordMacroStart = function () {
        $$.ShowAltToolbar(function () {
            $$.toolbarMacroRecord.show('slideup');
        });
        HG.Automation.Macro.Record();
        $$.field('#btn_control_macrorecord', true).qtip({
            content: {
                title: HG.WebApp.Locales.GetLocaleString('control_macrorecord_recording'),
                text: HG.WebApp.Locales.GetLocaleString('control_macrorecord_description'),
                button: HG.WebApp.Locales.GetLocaleString('control_macrorecord_close'),
            },
            show: { event: false, ready: true, delay: 1500 },
            events: {
                hide: function () {
                    $(this).qtip('destroy');
                }
            },
            hide: { event: false, inactive: 5000 },
            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
            position: { my: 'bottom center', at: 'top center' }
        });
    };

    $$.RecordMacroSave = function (mode) {
        $.mobile.loading('show');
        HG.Automation.Macro.Save(mode, function (data) {
            HG.Automation.Programs.List(function () {
                HG.WebApp.AutomationGroupsList._CurrentGroup = '';
                HG.WebApp.ProgramEdit._CurrentProgram.Address = data;
                HG.Configure.Groups.List('Automation', function () {
                    HG.WebApp.ProgramsList.EditProgram();
                    $.mobile.changePage($$.field('#page_automation_editprogram', true), { transition: 'fade', changeHash: true });
                    $.mobile.loading('hide');
                });
            });
        });
        $$.toolbarMacroRecord.hide('slidedown', function () {
            $$.HideAltToolbar();
        });
    };

    $$.RecordMacroDiscard = function () {
        HG.Automation.Macro.Discard();
        $$.toolbarMacroRecord.hide('slidedown', function () {
            $$.HideAltToolbar();
        });
    };

    $$.RenderGroups = function () {
        // destroy any previous instance of isotope
        $.each($$.field('#control_groupcontent', true).find('div[class=isotope]'), function(i, l){
            $(this).isotope('destroy');
        });
        // render groups
        $$.field('#control_groupcontent', true).empty();
        for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            if (i == 0) {
                HG.WebApp.Data._CurrentGroup = HG.WebApp.Data.Groups[i].Name;
            }
            $$.field('#control_groupcontent', true).append('<div class="modules-grid" id="groupdiv_modules_' + i + '" />');
        }
    };

    $$.GetWidget = function (widgetpath, callback) {
        var widgetcached = false;
        for (var o = 0; o < $$._widgetList.length; o++) {
            if ($$._widgetList[o].Widget == widgetpath) {
                widgetcached = true;
                if (typeof callback == 'function') {
                    var w = $$._widgetList[o];
                    var widget = HG.WebApp.WidgetEditor.GetInstance(w.Json);
                    callback({ Widget: w.Widget, Instance: widget, Json: w.Json, Model: w.Model });
                }
                break;
            }
        }
        if (widgetpath != '' && !widgetcached) {
            $.ajax({
                url: "pages/control/widgets/" + widgetpath + ".js",
                type: 'GET',
                dataType: 'text',
                success: function (jsData) {
                    var widget = null;
                    var widgetjson = jsData;
                    $.get("pages/control/widgets/" + widgetpath + ".html", function (htmlData) {
                        var widgetobj = { Widget: widgetpath, Instance: null, Json: widgetjson, Model: htmlData };
                        $$._widgetList.push(widgetobj);
                        try {
                            widget = HG.WebApp.WidgetEditor.GetInstance(widgetjson);
                        } catch (e) {
                            alert(widgetpath + " Loading Error:\n" + e);
                        }
                        if (typeof callback == 'function')
                            callback({ Widget: widgetpath, Instance: widget, Json: widgetjson, Model: htmlData });
                    });
                },
                error: function (jsData) {
                    console.log(jsData);
                    if (typeof callback == 'function')
                        callback(null);
                }
            });
        } else if (!widgetcached) {
            if (typeof callback == 'function')
                callback(null);
        }
    };

    var widgetsloadqueue = [];
    $$.RenderModule = function () {
        if (widgetsloadqueue.length > 0) {
            // extract and render element
            var rendermodule = widgetsloadqueue.splice(0, 1)[0];
            var widget = $$.field('#'+rendermodule.ElementId, true).data('homegenie.widget');
            if (widget != null && widget != 'undefined') {
                HG.WebApp.WidgetEditor.RenderWidget('#'+rendermodule.ElementId, widget, widget.module);
                $$.RenderModule();
            } else {
                var html = '<div class="freewall modules-grid-item"><div id="' + rendermodule.ElementId + '" style="display:none" class="hg-widget-container" data-context-group="' + rendermodule.GroupName + '" data-context-value="' + HG.WebApp.Utility.GetModuleIndexByDomainAddress(rendermodule.Module.Domain, rendermodule.Module.Address) + '">';
                $$.GetWidget(rendermodule.Module.Widget, function (w) {
                    if (typeof w != 'undefined' && w != null) {
                        html += w.Model;
                        html += '</div></div>';
                        rendermodule.GroupElement.append(html);
                        rendermodule.GroupElement.trigger('create');
                        //rendermodule.GroupElement.listview('refresh');
                        if (w.Json != null) {
                            var myinstance = HG.WebApp.WidgetEditor.GetInstance(w.Json);
                            // store reference to this widget instance
                            $$.field('#'+rendermodule.ElementId, true).data('homegenie.widget', myinstance);
                            rendermodule.Module.WidgetInstance = myinstance;
                            // localize widget
                            HG.WebApp.Locales.LocalizeWidget(rendermodule.Module.Widget, rendermodule.ElementId, function() {
                                $$.field('#'+rendermodule.ElementId, true).show();
                                // render widget view and load next widget
                                var mod = rendermodule.Module;
                                HG.WebApp.WidgetEditor.RenderWidget('#'+rendermodule.ElementId, mod.WidgetInstance, mod);
                                $$.RenderModule();
                            });
                        } else {
                            alert(rendermodule.Module.Widget + " Widget Instance Error:\n" + e);
                            // an error occurred, continue loading next widget
                            $$.RenderModule();
                        }
                    } else {
                        console.log(rendermodule.Module.Widget + " Widget Error.");
                        // an error occurred, continue loading next widget
                        $$.RenderModule();
                    }
                });
            }
        } else {
            $$._grid = $$.field('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex, true).isotope({
                itemSelector: '.freewall',
                layoutMode: 'fitRows'
            }).isotope({transitionDuration: '300ms'}).addClass('isotope');
            $$._grid.on( 'arrangeComplete', function() {
                $$._grid.find('.freewall').removeClass('drag-collide drag-collide-target');
            });
            setTimeout(function() {
                $$._grid.isotope('layout');
            }, 50);
            $$.UpdateActionsMenu();
            $.mobile.loading('hide');
        }

    };

    $$.WidgetsManager_dragStartHandler = function(event, pointer) {
        var $item = $(this);
        // insert placeholder div before dragged element
        $$._placeHolder.css('left', $item.css('left'));
        $$._placeHolder.css('top', $item.css('top'));
        $$._placeHolder.css('width', $item.width());
        $$._placeHolder.css('height', $item.height());
        $$._placeHolder.insertBefore($item);
        // remove dragged item
        $item.removeClass('freewall').addClass('drag-collide');
        $item.attr('old-z-index', $item.css('z-index'));
        $item.css('z-index', 1000);
    };
    $$.WidgetsManager_dragMoveHandler = function(event, pointer, moveVector) {
        var $item = $(this);
        var gid = HG.WebApp.Data._CurrentGroupIndex;
        var currentGroup = HG.WebApp.Data.Groups[gid];
        var grid = $$.field('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex, true);
        grid.find('.modules-grid-item').each(function(i2, gridItem2) {
            if ($item.get(0) !== gridItem2) {
                var hitTestResult = $item.hitTestObject(gridItem2, 0.5);
                if (hitTestResult) {
                    var $target = $(gridItem2);
                    // re-arrange elements (insert sourceIndex before/after targetIndex)
                    var sourceIndex = +$item.attr('data-index');
                    var targetIndex = +$target.attr('data-index');
                    if (hitTestResult === 2 && !$target.hasClass('drag-collide-target')) {
                        $target.addClass('drag-collide drag-collide-target');
                        if ($item.index() < $target.index()) { // insert after
                            currentGroup.Modules.move(sourceIndex, targetIndex);
                            $item.insertAfter($target);
                        } else { // inser before
                            currentGroup.Modules.move(sourceIndex, targetIndex);
                            $item.insertBefore($target);
                        }
                        // move placeholder div before $item
                        $$._placeHolder.insertBefore($item);
                        // update data-indexes
                        $item.attr('data-index', targetIndex); // (the element being dragged has `.freewall` class disabled)
                        grid.find('.freewall').each((function(is, nextElement) {
                            $(nextElement).attr('data-index', is);
                        }));
                        // wait for target element animation to complete
                        $target.one('webkitTransitionEnd otransitionend oTransitionEnd msTransitionEnd transitionend', function(e) {
                            $target.removeClass('drag-collide');
                            $target.removeClass('drag-collide-target');
                        });
                        // relayout
                        if ($$._relayoutTimeout) {
                            clearTimeout($$._relayoutTimeout);
                        }
                        $$._relayoutTimeout = setTimeout(function() {
                            $$._grid.isotope('reloadItems');
                            $$._grid.isotope({transitionDuration: '300ms'});
                        }, 350);
                        return false;
                    }
                } else {
                    $(gridItem2).removeClass('drag-collide');
                    $(gridItem2).removeClass('drag-collide-target');
                }
            }
        });
    };
    $$.WidgetsManager_dragEndHandler = function(event, pointer) {
        var $item = $(this);
        // remove placeholder div
        $$._placeHolder.remove();
        $$._placeHolder.css('transform', 'translate3d(0px, 0px, 0px)');
        // restore dragged item
        $$._grid.find('.freewall').removeClass('drag-collide');
        $item.addClass('freewall').removeClass('drag-collide');
        // relayout
        $$._grid.isotope('reloadItems');
        $$._grid.isotope({transitionDuration: '300ms'});
        $item.css('z-index', $item.attr('old-z-index'));
    };

    $$.WidgetsManager = function () {
        $$.ShowAltToolbar(function () {
            $$.toolbarWidgetsSort.show('slideup');
        });
        var grid = $$.field('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex, true);
        grid.find('.modules-grid-item').each(function(i, gridItem) {
            var $item = $(gridItem);
            if (!$item.data('draggabilly')) {
                if (!$item.attr('data-index')) {
                    $item.attr('data-index', $item.index());
                }
                $item.draggabilly({});
            }
            // enable dragging
            $item.on('dragStart', $$.WidgetsManager_dragStartHandler);
            $item.on('dragMove', $$.WidgetsManager_dragMoveHandler);
            $item.on('dragEnd', $$.WidgetsManager_dragEndHandler);
        });
    };
    $$.WidgetsManagerCancel = function() {
        $$._grid.find('.modules-grid-item').each(function(i, gridItem) {
            var $item = $(gridItem);
            // disable dragging
            $item.off('dragStart', $$.WidgetsManager_dragStartHandler);
            $item.off('dragMove', $$.WidgetsManager_dragMoveHandler);
            $item.off('dragEnd', $$.WidgetsManager_dragEndHandler);
            $item.draggabilly('destroy');
        });
        $$._grid.isotope('reloadItems');
        $$._grid.isotope({transitionDuration: 0});
        $$.toolbarWidgetsSort.hide('slidedown', function () {
            $$.HideAltToolbar();
        });
    };
    $$.WidgetsManagerSave = function () {
        $.mobile.loading('show');
        $.ajax({
            type: 'POST',
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Config/Groups.Save/',
            data: JSON.stringify(HG.WebApp.Data.Groups),
            success: function (response) {
                $.mobile.loading('hide');
            },
            error: function (a, b, c) {
                $.mobile.loading('hide');
            }
        });
        $$.WidgetsManagerCancel(); // disable drag'n'drop
        $$.toolbarWidgetsSort.hide('slidedown', function () {
            $$.HideAltToolbar();
        });
    };


    $$.EditModule = function (module) {
        HG.WebApp.GroupModules.CurrentGroup = HG.WebApp.Data._CurrentGroup;
        HG.WebApp.GroupModules.CurrentModule = module;
        var oldtype = module.DeviceType;
        HG.WebApp.GroupModules.ModuleEdit(function () {
            if (oldtype != module.DeviceType) {
                var grp = $$.field('#groupdiv_modules_' + HG.WebApp.Data._CurrentGroupIndex, true);
                grp.empty();
                $$.clearCache();
                $$.ShowGroup(HG.WebApp.Data._CurrentGroupIndex);
            } else {
                $$.UpdateModuleWidget(module.Domain, module.Address);
            }
        });
    };

    $$.EditModuleParams = function (module) {
        HG.WebApp.GroupModules.CurrentGroup = HG.WebApp.Data._CurrentGroup;
        HG.WebApp.GroupModules.CurrentModule = module;
        $$.field('#page_configure_groupmodules_propspopup', true).popup('open', { transition: 'pop', positionTo: 'window' });
    };

    $$.GetModuleUid = function (module) {
        var domain = module.Domain.substring(module.Domain.lastIndexOf('.') + 1).replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
        var address = module.Address.replace(/[\.,-\/#!$%\^&\*;:{}=\-_`~() ]/g, '_');
        var id = domain + '_' + address;
        return id;
    };

    $$.UpdateActionsMenu = function () {
        $$.field('#control_custom_actionmenu', true).empty();
        for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            if (i == HG.WebApp.Data._CurrentGroupIndex) {
                var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
                for (var m = 0; m < groupmodules.Modules.length; m++) {
                    var module = groupmodules.Modules[m];
                    if (module.Widget == 'homegenie/generic/program') {
                        // add item to actions menu
                        $$.field('#control_custom_actionmenu', true).append('<li><a class="ui-btn ui-icon-fa-play-circle-o ui-btn-icon-right" onclick="HG.Automation.Programs.Toggle(\'' + module.Address + '\', \'' + HG.WebApp.Data._CurrentGroup + '\')">' + HG.WebApp.Locales.GetProgramLocaleString(module.Address, 'Title', module.Name) + '</a></li>');
                    }
                }
            }
        }
        $$.field('#control_custom_actionmenu', true).listview('refresh');
    };

    $$.UpdateModuleWidget = function (domain, address, parameter, value) {
        for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
            for (var m = 0; m < groupmodules.Modules.length; m++) {
                var module = groupmodules.Modules[m];
                if (module.Domain == domain && module.Address == address) {
                    var uid = 'groupdiv_modules_' + groupmodules.Index + '_module_' + $$.GetModuleUid(module);
                    var cuid = '#' + uid;
                    var modui = $$.field(cuid, true);
                    var type = module.DeviceType + ''; type = type.toLowerCase();
                    if (modui.length != 0) {
                        if (modui.data('homegenie.widget')) {
                            module.WidgetInstance = modui.data('homegenie.widget');
                            HG.WebApp.WidgetEditor.RenderWidget(cuid, module.WidgetInstance, module, { 'Property': parameter, 'Value': value });
                        }
                    }
                }
            }
        }
    };

    $$.RenderGroupModules = function (groupIndex) {
        var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[groupIndex].Name);
        var grp = $$.field('#groupdiv_modules_' + groupmodules.Index, true);
        for (var m = 0; m < groupmodules.Modules.length; m++) {
            var module = groupmodules.Modules[m];
            var uid = (grp.attr('id') + '_module_' + $$.GetModuleUid(module));
            var cuid = '#' + uid;
            var modui = $$.field(cuid, true);
            var type = module.DeviceType + ''; type = type.toLowerCase();
            //
            var widgetfound = false;

            // look for UI Group Label (fake module with domain HomeGenie.UI.GroupLabel
            if (module.Domain == HG.WebApp.GroupModules.SeparatorItemDomain) {
                module.Widget = 'homegenie/generic/grouplabel';
                widgetfound = true;
            }

            // look for explicit widget display module parameter
            if (!widgetfound) {
                var displaymodule = HG.WebApp.Utility.GetModulePropertyByName(module, "Widget.DisplayModule");
                if (displaymodule != null && displaymodule.Value != '') {
                    module.Widget = displaymodule.Value;
                    widgetfound = true;
                }
            }
            // fallback to configuration.json widgets mapping
            if (!widgetfound) {
                for (var wi = 0; wi < $$._widgetConfiguration.length; wi++) {
                    var widgetobj = $$._widgetConfiguration[wi];
                    var modprop = HG.WebApp.Utility.GetModulePropertyByName(module, widgetobj.MatchProperty);
                    if (modprop != null && (widgetobj.MatchValue == "*" || modprop.Value == widgetobj.MatchValue)) {
                        module.Widget = widgetobj.Widget;
                        widgetfound = true;
                        break;
                    }
                }
            }
            // last fall back.... select a generic widget based on DeviceType if no category specific widget has been found
            if (!widgetfound) {
                module.Widget = 'homegenie/generic/' + (type == 'undefined' ? 'unknown' : type);
            }
            //
            if (modui.length == 0) {
                widgetsloadqueue.push({ GroupName: HG.WebApp.Data.Groups[groupIndex].Name, GroupElement: grp, ElementId: uid, Module: module });
            } else {
                if (modui.data('homegenie.widget')) {
                    module.WidgetInstance = modui.data('homegenie.widget');
                    HG.WebApp.WidgetEditor.RenderWidget(cuid, module.WidgetInstance, module);
                }
            }
        }
        $$.RenderModule();
        if (widgetsloadqueue.length === 0) {
            $$.UpdateActionsMenu();
        }
    };

    $$._renderMenuTimeout = null;
    $$.RenderMenu = function () {
        if ($$._renderMenuTimeout != null)
            clearTimeout($$._renderMenuTimeout);
        $$._renderMenuTimeout = setTimeout($$._RenderMenu, 500);
    };

    $$._RenderMenu = function () {
        //$$.field('#control_groupsmenu', true).find("li:gt(0)").remove();
        $$.field('#control_groupsmenu', true).empty();
        for (i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var indicators = '<div class="ui-body-inherit ui-body-a" style="margin-top:6px;border:0"><div style="white-space:normal;max-width:290px" id="control_groupindicators_' + i + '"></div>';
            var item = $('<li data-context-idx="' + i + '" style="height:auto;cursor:pointer;font-size:12pt"><strong>' + HG.WebApp.Data.Groups[i].Name + '</strong>'+indicators+'</li>');
            item.on('click', function(){
                var idx = $(this).attr('data-context-idx');
                $$.ShowGroup(idx);
                $.mobile.pageContainer.pagecontainer('change', '#page_control', { transition: 'none' });
            });
            $$.field('#control_groupsmenu', true).append(item);
        }
        $$.field('#control_groupsmenu', true).listview().listview('refresh');
        $$._RefreshGroupIndicators();
    };

    $$._refreshIndicatorsTimeout = null;
    $$.RefreshGroupIndicators = function () {
        if ($$._refreshIndicatorsTimeout != null)
            clearTimeout($$._refreshIndicatorsTimeout);
        $$._refreshIndicatorsTimeout = setTimeout($$._RefreshGroupIndicators, 500);
    };

    $$._RefreshGroupIndicators = function () {
        for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var groupmodules = HG.Configure.Groups.GetGroupModules(HG.WebApp.Data.Groups[i].Name);
            var grouploadkw = 0;
            var operating_lights = 0;
            var operating_switches = 0;
            var group_temperature = null;
            var group_humidity = null;
            var group_luminance = null;
            var group_doorwindow = null;
            //
            var grp = $$.field('#groupdiv_modules_' + groupmodules.Index, true);
            for (var m = 0; m < groupmodules.Modules.length; m++) {
                var module = groupmodules.Modules[m];
                var type = module.DeviceType + ''; type = type.toLowerCase();
                //
                var w = HG.WebApp.Utility.GetModulePropertyByName(module, "Meter.Watts");
                var l = HG.WebApp.Utility.GetModulePropertyByName(module, "Status.Level");
                if (w != null && l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                    grouploadkw += (parseFloat(w.Value.replace(',', '.')) / 1000.0);
                }
                if (l != null && parseFloat(l.Value.replace(',', '.')) != 0) {
                    switch (type) {
                        case 'dimmer':
                        case 'light':
                            operating_lights++;
                            break;
                        case 'switch':
                            operating_switches++;
                            break;
                    }
                }
                if (group_temperature == null) {
                    var t = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Temperature");
                    if (t != null && t.Value != '') {
                        group_temperature = parseFloat(t.Value.replace(',', '.'));
                    }
                }
                if (group_humidity == null) {
                    var h = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Humidity");
                    if (h != null && h.Value != '') {
                        group_humidity = parseFloat(h.Value.replace(',', '.'));
                    }
                }
                if (group_luminance == null) {
                    var l = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.Luminance");
                    if (l != null && l.Value != '') {
                        group_luminance = parseFloat(l.Value.replace(',', '.'));
                    }
                }

                var d = HG.WebApp.Utility.GetModulePropertyByName(module, "Sensor.DoorWindow");
                if (d != null && d.Value != '' && d.Value != '0') {
                    if (group_doorwindow == null)
                        group_doorwindow = 1;
                    else
                        group_doorwindow++;
                }
            }
            //

            //'<td align="center"><img src="images/indicators/door.png" style="vertical-align:middle" /> <span style="font-size:12pt;color:whitesmoke">1</span></td>'+

            var indicators = '';
            if (group_doorwindow != null) {
                indicators += '<span class="hg-indicator-door">' + (group_doorwindow * 1).toFixed(0) + '</span> ';
            }
            if (grouploadkw > 0) {
                indicators += '<span class="hg-indicator-energy">' + (grouploadkw * 1000).toFixed(1) + '</span> ';
            }
            if (operating_switches > 0) {
                indicators += '<span class="hg-indicator-plug">' + operating_switches + '</span> ';
            }
            if (operating_lights > 0) {
                indicators += '<span class="hg-indicator-bulb">' + operating_lights + '</span> ';
            }
            if (group_temperature != null) {
                displayvalue = HG.WebApp.Utility.FormatTemperature(group_temperature);
                indicators += '<span class="hg-indicator-temperature">' + displayvalue + '</span> ';
            }
            if (group_luminance != null) {
                indicators += '<span class="hg-indicator-luminance">' + (group_luminance * 1).toFixed(0) + '</span> ';
            }
            if (group_humidity != null) {
                indicators += '<span class="hg-indicator-humidity">' + (group_humidity * 1).toFixed(0) + '</span> ';
            }
            $('#control_groupindicators_' + i).html(indicators);
            if (i == HG.WebApp.Data._CurrentGroupIndex) {
                $$.field('#control_groupmenutitle', true).html(HG.WebApp.Data.Groups[i].Name);
                $$.field('#control_groupindicators', true).html(indicators);
            }
        }
        $$.field('#control_groupsmenu', true).listview().listview('refresh');
    };

    $$.LoadGroups = function () {
        $.mobile.loading('show');
        HG.Configure.Groups.List('Control', function () {
            $$.RenderGroups();
        });
    };

};
HG.Ui.CreatePage(HG.WebApp.Control, 'page_control');
