HG.WebApp.WidgetEditor = HG.WebApp.WidgetEditor || {};
HG.WebApp.WidgetEditor.PageId = 'page_widgeteditor_editwidget';
HG.WebApp.WidgetEditor._hasError = false;
HG.WebApp.WidgetEditor._editorHtml = null;
HG.WebApp.WidgetEditor._editorJscript = null;
HG.WebApp.WidgetEditor._previewHeight = 245;
HG.WebApp.WidgetEditor._splitDragStartY = 0;

HG.WebApp.WidgetEditor.InitializePage = function () {
    var page = $('#'+HG.WebApp.WidgetEditor.PageId);
    var previewButton = page.find('[data-ui-field=preview-btn]');
    var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    var saveButton = page.find('[data-ui-field=save-btn]');
    var exportButton = page.find('[data-ui-field=export-btn]');
    var previewPanel = page.find('[data-ui-field=preview-panel]');
    var splitBar = page.find('[data-ui-field=split-bar]');
    
    page.on('pageinit', function (e) {
        HG.WebApp.WidgetEditor._editorHtml = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_html'), {
            lineNumbers: true,
            matchBrackets: true,
            autoCloseBrackets: true,
            extraKeys: {
                "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
                "Ctrl-Space": "autocomplete"
            },
            foldGutter: true,
            gutters: ["CodeMirror-lint-markers-4", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
            highlightSelectionMatches: { showToken: /\w/ },
            mode: "text/html",
            matchTags: {bothTags: true},
            theme: 'ambiance'
        });
        HG.WebApp.WidgetEditor._editorJscript = CodeMirror.fromTextArea(document.getElementById('widgeteditor_code_javascript'), {
            lineNumbers: true,
            matchBrackets: true,
            autoCloseBrackets: true,
            extraKeys: {
                "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); },
                "Ctrl-Space": "autocomplete"
            },
            foldGutter: true,
            gutters: ["CodeMirror-lint-markers-5", "CodeMirror-linenumbers", "CodeMirror-foldgutter"],
            highlightSelectionMatches: { showToken: /\w/ },
            mode: "text/javascript",
            theme: 'ambiance'
        });    
    });
    page.on('pagebeforeshow', function (e) {
        page.find('[data-ui-field=title-heading]').html('<span style="font-size:10pt;font-weight:bold">Widget Editor</span><br/>' + HG.WebApp.WidgetsList._currentWidget);
        
        // standard editor/preview size
        page.find('.CodeMirror').css('bottom', (HG.WebApp.WidgetEditor._previewHeight + 5)+'px');
        previewPanel.height(HG.WebApp.WidgetEditor._previewHeight);
        
        // load widget html/js
        $.ajax({
            url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._currentWidget + '.html',
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                HG.WebApp.WidgetEditor._editorHtml.setValue(data);
                HG.WebApp.WidgetEditor._editorHtml.clearHistory();
                HG.WebApp.WidgetEditor._editorHtml.markClean();
                $.ajax({
                    url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._currentWidget + '.js',
                    type: 'GET',
                    dataType: 'text',
                    success: function (data) {
                        HG.WebApp.WidgetEditor._editorJscript.setValue(data);
                        HG.WebApp.WidgetEditor._editorJscript.clearHistory();
                        HG.WebApp.WidgetEditor._editorJscript.markClean();
                        HG.WebApp.WidgetEditor.RefreshCodeMirror();
                    }
                });
            }
        });
        
        // initially the user have to press Run/Preview button in order to activate the widget
        page.find('[data-ui-field=preview-div]').html('');
        HG.WebApp.WidgetEditor._hasError = true;
        // populate "bind module" select menu
        var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
        bindModuleSelect.empty();
        bindModuleSelect.append('<option value="">(select a module)</option>');
        for (var m = 0; m < HG.WebApp.Data.Modules.length; m++)
        {
            var name = HG.WebApp.Data.Modules[m].Name.trim();
            if (name == '')
            {
                name = HG.WebApp.Data.Modules[m].Domain + ':' + HG.WebApp.Data.Modules[m].Address;
            }
            bindModuleSelect.append('<option value="' + m + '">' + name + '</option>');
        }
        bindModuleSelect.trigger('create');
        bindModuleSelect.val('');
        bindModuleSelect.selectmenu('refresh');
        HG.WebApp.WidgetEditor.SetTab(1);
    });
        
    saveButton.bind('click', function(){
        $('#editwidget_actionmenu').popup('close');
        // save html and javascript
        $.mobile.loading('show', { text: 'Saving HTML...', textVisible: true, theme: 'a', html: '' });
        HG.Configure.Widgets.Save(HG.WebApp.WidgetsList._currentWidget, 'html', HG.WebApp.WidgetEditor._editorHtml.getValue(), function(res) { 
            $.mobile.loading('show', { text: 'Saving Javascript...', textVisible: true, theme: 'a', html: '' });
            HG.Configure.Widgets.Save(HG.WebApp.WidgetsList._currentWidget, 'js', HG.WebApp.WidgetEditor._editorJscript.getValue(), function(res) { 
                $.mobile.loading('hide');
            });
        });
    });
    
    exportButton.bind('click', function(){
        $('#editwidget_actionmenu').popup('close');
        // export current widget
        //HG.Configure.Widgets.Export(HG.WebApp.WidgetsList._currentWidget)
        $('#program_import_downloadframe').attr('src', location.protocol + '../HomeAutomation.HomeGenie/Config/Widgets.Export/' + encodeURIComponent(HG.WebApp.WidgetsList._currentWidget) + '/');
    });
    
    previewButton.bind('click', function(){
        HG.WebApp.WidgetEditor._hasError = false;
        HG.WebApp.WidgetEditor._editorJscript.clearGutter('CodeMirror-lint-markers-5');
        HG.WebApp.WidgetEditor.Preview();
    });
    bindModuleSelect.on('change', function(){
        HG.WebApp.WidgetEditor.Preview();
    });
    errorsButton.hide();
    
    splitBar.mousedown(function(event) {
        HG.WebApp.WidgetEditor._splitDragStartY = event.pageY;
        $(window).mousemove(function(ev) {
            var deltaY = HG.WebApp.WidgetEditor._splitDragStartY - ev.pageY;
            var maxHeight = page.height() / 2;
            var newHeight = (previewPanel.height() + deltaY);
            if (newHeight >= 5 && newHeight <= maxHeight)
            {
                previewPanel.height((previewPanel.height() + deltaY));
                page.find('.CodeMirror').css('bottom', (previewPanel.height() + 5)+'px');
                HG.WebApp.WidgetEditor._splitDragStartY = ev.pageY;
            }
            else
            {
                $(window).unbind("mousemove");
                HG.WebApp.WidgetEditor._editorHtml.refresh();
                HG.WebApp.WidgetEditor._editorJscript.refresh();
            }
        });
    })
    .mouseup(function() {
        $(window).unbind("mousemove");
        HG.WebApp.WidgetEditor._editorHtml.refresh();
        HG.WebApp.WidgetEditor._editorJscript.refresh();
    });
    
    window.onerror = function(msg, url, line, col, error) {
        if (url.indexOf('#'+HG.WebApp.WidgetEditor.PageId) > 0)
        {
            HG.WebApp.WidgetEditor.ShowError(error);
        }
        else
        {
            throw error;
        }
    };   
};

HG.WebApp.WidgetEditor.SetTab = function(tabIndex) {
    var page = $('#'+HG.WebApp.WidgetEditor.PageId);
    page.find('[data-ui-field=tab1-div]').hide();
    page.find('[data-ui-field=tab2-div]').hide();
    page.find('[data-ui-field=tab3-div]').hide();
    page.find('[data-ui-field=tab1-btn]').removeClass('ui-btn-active');
    page.find('[data-ui-field=tab2-btn]').removeClass('ui-btn-active');
    page.find('[data-ui-field=tab3-btn]').removeClass('ui-btn-active');
    page.find('[data-ui-field=tab' + tabIndex + '-div]').show();
    page.find('[data-ui-field=tab' + tabIndex + '-btn]').addClass('ui-btn-active');
    HG.WebApp.WidgetEditor.RefreshCodeMirror();
};

HG.WebApp.WidgetEditor.RefreshCodeMirror = function() {
    setTimeout(function () {
        HG.WebApp.WidgetEditor._editorHtml.refresh();
        HG.WebApp.WidgetEditor._editorJscript.refresh();
    }, 500);                 
};

HG.WebApp.WidgetEditor.Preview = function() {
    if (HG.WebApp.WidgetEditor._hasError) return;
    var page = $('#'+HG.WebApp.WidgetEditor.PageId);
    var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    errorsButton.hide();
    var htmlCode = '<div id="widget_preview_instance" data-ui-field="preview-wrapper-div" align="left" style="display:table-cell">';
    htmlCode += HG.WebApp.WidgetEditor._editorHtml.getValue();
    htmlCode += '</div>';
    page.find('[data-ui-field=preview-div]').html(htmlCode);
    page.find('[data-ui-field=preview-wrapper-div]').trigger('create');
    var javascriptCode = HG.WebApp.WidgetEditor._editorJscript.getValue();
    try
    {
        HG.WebApp.WidgetEditor._hasError = false;
        var widgetInstance = eval(javascriptCode)[0];
        widgetInstance.RenderView('#widget_preview_instance', HG.WebApp.Data.Modules[bindModuleSelect.val()]);
    } catch (e) {
        HG.WebApp.WidgetEditor._hasError = true;
        HG.WebApp.WidgetEditor.ShowError(e);
    }
};

HG.WebApp.WidgetEditor.ShowError = function(e) {
    var page = $('#'+HG.WebApp.WidgetEditor.PageId);
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    var stack = ErrorStackParser.parse(e);
    var message = e + '<br/> <a href="javascript:HG.WebApp.WidgetEditor.JumpToLine({ line: ' + (stack[0].lineNumber - 1) + ', ch: ' + (stack[0].columnNumber - 1) + ' })">Line <strong>' + stack[0].lineNumber + '</strong>, Column <strong>' + stack[0].columnNumber + '</strong></a>';
    var marker = document.createElement('div');
    HG.WebApp.WidgetEditor.SetTab(2);
    HG.WebApp.WidgetEditor._editorJscript.clearGutter('CodeMirror-lint-markers-5');
    marker.className = 'CodeMirror-lint-marker-error';
    HG.WebApp.WidgetEditor._editorJscript.setGutterMarker(stack[0].lineNumber - 1, 'CodeMirror-lint-markers-5', marker);
    $(marker).qtip({
        content: { title: 'Error', text: message, button: 'Close' },
        show: { event: 'mouseover', solo: true },
        hide: 'mouseout',
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' }
    });
    errorsButton.show();
    errorsButton.qtip({
        content: { title: 'Error', text: message, button: 'Close' },
        show: { event: 'mouseover', ready: true, delay: 500 },
        hide: { event: false, inactive: 5000 },
        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
        position: { adjust: { screen: true }, my: 'top center', at: 'bottom center' }
    });
    console.log(message);
    console.log(stack);
};

HG.WebApp.WidgetEditor.JumpToLine = function(position) {
    window.setTimeout(function () {
        HG.WebApp.WidgetEditor._editorJscript.setCursor(position);
        var myHeight = HG.WebApp.WidgetEditor._editorJscript.getScrollInfo().clientHeight; 
        var coords = HG.WebApp.WidgetEditor._editorJscript.charCoords(position, "local"); 
        HG.WebApp.WidgetEditor._editorJscript.scrollTo(null, (coords.top + coords.bottom - myHeight) / 2);             
        HG.WebApp.WidgetEditor._editorJscript.focus();
    }, 500);
};