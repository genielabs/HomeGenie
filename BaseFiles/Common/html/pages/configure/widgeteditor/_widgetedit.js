HG.WebApp.WidgetEditor = HG.WebApp.WidgetEditor || {};
HG.WebApp.WidgetEditor.PageId = '#page_widgeteditor_editwidget';
HG.WebApp.WidgetEditor.hasError = false;

HG.WebApp.WidgetEditor.InitializePage = function () {
    var page = $(HG.WebApp.WidgetEditor.PageId);
    var previewButton = page.find('[data-ui-field=preview-btn]');
    var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    
    page.on('pagebeforeshow', function (e) {
        editor4.setValue('');
        editor4.markClean();
        editor5.setValue('');
        editor5.markClean();
        $.ajax({
            url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._CurrentWidget + '.html',
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                editor4.setValue(data);
                editor4.markClean();
                $.ajax({
                    url: '/hg/html/pages/control/widgets/' + HG.WebApp.WidgetsList._CurrentWidget + '.js',
                    type: 'GET',
                    dataType: 'text',
                    success: function (data) {
                        editor5.setValue(data);
                        editor5.markClean();
                        HG.WebApp.WidgetEditor.RefreshCodeMirror();
                    }
                });
            }
        });
        //
        var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
        bindModuleSelect.empty();
        for (var m = 0; m < HG.WebApp.Data.Modules.length; m++)
        {
            bindModuleSelect.append('<option value="' + m + '">' + HG.WebApp.Data.Modules[m].Name + '</option>');
        }
        bindModuleSelect.trigger('create');
    });
        
    previewButton.bind('click', function(){
        HG.WebApp.WidgetEditor.hasError = false;
        editor5.clearGutter('CodeMirror-lint-markers-5');
        HG.WebApp.WidgetEditor.Preview();
    });
    errorsButton.hide();
    
    bindModuleSelect.on('change', function(){
        HG.WebApp.WidgetEditor.Preview();
    });
    
    window.onerror = function(msg, url, line, col, error) {
        if (url.indexOf('#page_widgeteditor_editwidget') > 0)
        {
            HG.WebApp.WidgetEditor.ShowError(error);
        }
        else
        {
            throw error;
        }
    };
    
    HG.WebApp.WidgetEditor.SetTab(1);
};

HG.WebApp.WidgetEditor.SetTab = function(tabIndex) {
    var page = $(HG.WebApp.WidgetEditor.PageId);
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
        editor4.refresh();
        editor5.refresh();
    }, 500);                 
};

HG.WebApp.WidgetEditor.Preview = function() {
    if (HG.WebApp.WidgetEditor.hasError) return;
    var page = $(HG.WebApp.WidgetEditor.PageId);
    var bindModuleSelect = page.find('[data-ui-field=bindmodule-sel]');
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    errorsButton.hide();
    var htmlCode = '<div id="widget_preview_instance" data-ui-field="preview-wrapper-div" align="left" style="display:table-cell">';
    htmlCode += editor4.getValue();
    htmlCode += '</div>';
    page.find('[data-ui-field=preview-div]').html(htmlCode);
    page.find('[data-ui-field=preview-wrapper-div]').trigger('create');
    var javascriptCode = editor5.getValue();
    try
    {
        HG.WebApp.WidgetEditor.hasError = false;
        var widgetInstance = eval(javascriptCode)[0];
        widgetInstance.RenderView('#widget_preview_instance', HG.WebApp.Data.Modules[bindModuleSelect.val()]);
    } catch (e) {
        HG.WebApp.WidgetEditor.hasError = true;
        HG.WebApp.WidgetEditor.ShowError(e);
    }
};

HG.WebApp.WidgetEditor.ShowError = function(e) {
    var page = $(HG.WebApp.WidgetEditor.PageId);
    var errorsButton = page.find('[data-ui-field=errors-btn]');
    var stack = ErrorStackParser.parse(e);
    var message = e + '<br/> <a href="javascript:HG.WebApp.WidgetEditor.JumpToLine({ line: ' + (stack[0].lineNumber - 1) + ', ch: ' + (stack[0].columnNumber - 1) + ' })">Line <strong>' + stack[0].lineNumber + '</strong>, Column <strong>' + stack[0].columnNumber + '</strong></a>';
    var marker = document.createElement('div');
    HG.WebApp.WidgetEditor.SetTab(2);
    editor5.clearGutter('CodeMirror-lint-markers-5');
    marker.className = 'CodeMirror-lint-marker-error';
    editor5.setGutterMarker(stack[0].lineNumber - 1, 'CodeMirror-lint-markers-5', marker);
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
        editor5.setCursor(position);
        var myHeight = editor5.getScrollInfo().clientHeight; 
        var coords = editor5.charCoords(position, "local"); 
        editor5.scrollTo(null, (coords.top + coords.bottom - myHeight) / 2);             
        editor5.focus();
    }, 500);
};