//
// namespace : HG.WebApp.Utility namespace
// info      : global utility functions
//
HG.WebApp.Utility = HG.WebApp.Utility || new function(){ var $$ = this;

    // code mirror full screen editor popup
    $$._cmFsEditor = null;

    $$.EditorPopup = function(name, title, subtitle, content, callback) {
        if ($$._cmFsEditor == null) {
            $$._cmFsEditor = CodeMirror.fromTextArea(document.getElementById('fullscreen_edit_text'), {
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
                mode: { name: "javascript", globalVars: true },
                theme: 'ambiance'
            });
        }
        var editor = $('#fullscreen_edit_box');
        editor.find('[data-ui-field=title]').html(title);
        editor.find('[data-ui-field=subtitle]').html(subtitle);
        var nameLabel = editor.find('[data-ui-field=namelabel]').html(name);
        var nameInputDiv = editor.find('[data-ui-field=nameinput]');
        var nameInputText = nameInputDiv.find('input').val(name);
        var confirmButton = editor.find('[data-ui-field=confirm]');
        var cancelButton = editor.find('[data-ui-field=cancel]');
        if (name == null || name == '') {
            nameLabel.hide();
            nameInputDiv.show();
        } else {
            nameLabel.show();
            nameInputDiv.hide();
        }
        cancelButton.on('click', function() {
            var response = { 'name': nameInputText.val(), 'text': $$._cmFsEditor.getValue(), 'isCanceled': true };
            cancelButton.off('click');
            confirmButton.off('click');
            $('#fullscreen_edit_box').hide(150);
            callback(response);
        });
        confirmButton.on('click', function() {
            var response = { 'name': nameInputText.val(), 'text': $$._cmFsEditor.getValue(), 'isCanceled': false };
            if (nameInputText.val() == '') {
                nameInputText.qtip({
                    content: {
                        text: HG.WebApp.Locales.GetLocaleString('fullscreen_editor_entervalidname', 'Enter a valid name.')
                    },
                    show: { event: false, ready: true, delay: 200 },
                    hide: { event: false, inactive: 2000 },
                    style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                    position: { my: 'bottom center', at: 'top center' }
                })
                .parent().stop().animate({ borderColor: "#FF5050" }, 250)
                    .animate({ borderColor: "#FFFFFF" }, 250)
                    .animate({ borderColor: "#FF5050" }, 250)
                    .animate({ borderColor: "#FFFFFF" }, 250);
            } else {
                cancelButton.off('click');
                confirmButton.off('click');
                $('#fullscreen_edit_box').hide(150);
                callback(response);
            }
        });
        $$._cmFsEditor.setValue(content);
        setTimeout(function(){
            $$._cmFsEditor.refresh();
            $$._cmFsEditor.focus();
            $$._cmFsEditor.setCursor({ line: 0, ch: 0 });
        }, 500);
        $('#fullscreen_edit_box').show(150);
    };

    $$.ConfirmPopup = function(title, description, callback) {
        var confirmPopup = $('#actionconfirm_popup');
        confirmPopup.buttonProceed = $('#actionconfirm_confirm_button');
        confirmPopup.buttonCancel = $('#actionconfirm_cancel_button');
        confirmPopup.find('h3').html(title);
        confirmPopup.find('p').html(description);
        confirmPopup.buttonCancel.focus();
        var canceled = function( event, ui ) {
            callback(false);
        };
        var confirmed = function( event, ui ) {
            callback(true);
        };
        confirmPopup.buttonCancel.on('click', canceled);
        confirmPopup.buttonProceed.on('click', confirmed);
        confirmPopup.on( "popupafterclose", function(){
            confirmPopup.buttonCancel.off('click', canceled);
            confirmPopup.buttonProceed.off('click', confirmed);
        });
        setTimeout(function(){ confirmPopup.popup('open', { transition: 'pop' }); }, 250);
    };

    $$.GetElapsedTimeText = function (timestamp)
    {
        var ret = "";
        timestamp = (new Date() - timestamp) / 1000;
        if (timestamp > 0)
        {
            var days = Math.floor(timestamp / 86400);
            var hours = Math.floor((timestamp - (days * 86400 )) / 3600);
            var minutes = Math.floor((timestamp - (days * 86400 ) - (hours * 3600 )) / 60);
            var secs = Math.floor((timestamp - (days * 86400 ) - (hours * 3600 ) - (minutes * 60)));
            //
            if (days > 0) ret += days + "d ";
            if (hours > 0) ret += hours + "h ";
            if (minutes > 0) ret += minutes + "m ";
            else if (secs > 0) ret += secs + "s";
        }
        return ret;
    };

    $$.ParseModuleDomainAddress = function (domainAddress)
    {
        var result = null;
        if (domainAddress.indexOf(':') > 0) {
            result = {
                Domain: domainAddress.substring(0, domainAddress.lastIndexOf(':')),
                Address: domainAddress.substring(domainAddress.lastIndexOf(':') + 1)
            };
        }
        return result;
    };

    $$.GetModuleByDomainAddress = function (domain, address)
    {
        var module = null;
        for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
            if (HG.WebApp.Data.Modules[m].Domain == domain && HG.WebApp.Data.Modules[m].Address == address) {
                module = HG.WebApp.Data.Modules[m];
                break;
            }
        }
        return module;
    };

    $$.GetModuleIndexByDomainAddress = function (domain, address)
    {
        var moduleidx = -1;
        for (var m = 0; m < HG.WebApp.Data.Modules.length; m++) {
            if (HG.WebApp.Data.Modules[m].Domain == domain && HG.WebApp.Data.Modules[m].Address == address) {
                moduleidx = m;
                break;
            }
        }
        return moduleidx;
    };

    $$.GetModulePropertyByName = function (module, prop) {
        var value = null;
        if (module.Properties != null) {
            for (var p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name == prop) {
                    value = module.Properties[p];
                    break;
                }
            }
        }
        return value;
    };

    $$.SetModulePropertyByName = function (module, prop, value, timestamp) {
        var found = false;
        if (module.Properties != null) {
            for (var p = 0; p < module.Properties.length; p++) {
                if (module.Properties[p].Name == prop) {
                    module.Properties[p].Value = value;
                    if (typeof timestamp != 'undefined')
                    {
                        module.Properties[p].UpdateTime = timestamp;
                    }
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                module.Properties.push({ 'Name' : prop, 'Value' : value });
            }
        }
    };

    $$.GetProgramByAddress = function (paddr)
    {
        var cp = null;
        for (i = 0; i < HG.WebApp.Data.Programs.length; i++) {
            if (HG.WebApp.Data.Programs[i].Address == paddr)
            {
                cp = HG.WebApp.Data.Programs[i];
                break;
            }
        }
        return cp;
    };

    $$.GetCommandFromEvent = function (module, event)
    {
        var commandobj = null;
        if ((module.DeviceType == 'Switch' || module.DeviceType == 'Light' || module.DeviceType == 'Dimmer' || module.DeviceType == 'Siren' || module.DeviceType == 'Fan' || module.DeviceType == 'Shutter') && event.Property == 'Status.Level')
        {
            var command = 'Control.Level';
            var arg = event.Value;
            if (parseFloat(arg.replace(',', '.')) == 0)
            {
                command = 'Control.Off';
                arg = '';
            }
            else if (parseFloat(arg.replace(',', '.')) == 1)
            {
                command = 'Control.On';
                arg = '';
            }
            commandobj = {
                'Domain' : module.Domain,
                'Target' : module.Address,
                'CommandString' : command,
                'CommandArguments' : arg
            };
        }
        else if (module.Domain == 'Controllers.LircRemote' && module.Address == 'IR')
        {
            commandobj = {
                'Domain' : module.Domain,
                'Target' : module.Address,
                'CommandString' : 'Control.IrSend',
                'CommandArguments' : event.Value
            };
        }
        return commandobj;
    };

    $$.GetLocaleTemperature = function (temp) {
        var temperatureUnit = HG.WebApp.Store.get('UI.TemperatureUnit');
        if (temperatureUnit != 'C' && (temperatureUnit == 'F' || HG.WebApp.Locales.GetDateEndianType() == 'M')) {
            // display as Fahrenheit
            temp = Math.round((temp * 1.8 + 32) * 10) / 10;
        } else {
            // display as Celsius
            temp = Math.round(temp * 10) / 10;
        }
        return (temp * 1).toFixed(2);
    };

    $$.FormatTemperature = function (temp) {
        var displayvalue = '';
        var temperatureUnit = HG.WebApp.Store.get('UI.TemperatureUnit');
        if (temperatureUnit != 'C' && (temperatureUnit == 'F' || HG.WebApp.Locales.GetDateEndianType() == 'M')) {
            // display as Fahrenheit
            temp = Math.round((temp * 1.8 + 32) * 10) / 10;
            displayvalue = (temp * 1).toFixed(1) + '&deg;'; //'&#8457;';
        } else {
            // display as Celsius
            temp = Math.round(temp * 10) / 10;
            displayvalue = (temp * 1).toFixed(1) + '&deg;'; //'&#8451;';
        }
        return displayvalue;
    };

    $$.FormatDate = function (date)
    {
        var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
        var dt = null;
        if (dateFormat != 'DMY24' && (dateFormat == 'MDY12' || HG.WebApp.Locales.GetDateEndianType() == 'M'))
            dt = $.datepicker.formatDate('D, mm/dd/yy', date);
        else
            dt = $.datepicker.formatDate('D, dd/mm/yy', date);
        return dt;
    };

    // if options contains 's' show seconds
    // if options contains 'sm' show seconds and milliseconds
    $$.FormatDateTime = function (date, options)
    {
        var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
        var dt = null;
        var h = date.getHours();
        var mm = date.getMinutes().toString(); if (mm.length == 1) mm = '0' + mm;
        var ss = date.getSeconds().toString(); if (ss.length == 1) ss = '0' + ss;
        if (typeof options == 'undefined' || options == null) options = '';
        if (dateFormat != 'DMY24' && (dateFormat == 'MDY12' || HG.WebApp.Locales.GetDateEndianType() == 'M'))
        {
            var ampm = (h >= 12 ? 'PM' : 'AM');
            h = h % 12; h = (h ? h : 12);
            dt = h + ':' + mm + (options.indexOf('s')==0 ? ':' + ss + (options == 'sm' ? '.' + date.getMilliseconds() : '') : '') + ' ' + ampm;
        }
        else
        {
            dt = h + ':' + mm + (options.indexOf('s')==0 ? ':' + ss + (options == 'sm' ? '.' + date.getMilliseconds() : '') : '');
        }
        return dt;
    };

    $$.GetDateBoxLocale = function () {
        var locale = 'default';
        var dateboxLanguages = jQuery.jtsage.datebox.prototype.options.lang; //jQuery.mobile.datebox.prototype.options.lang;
        var userLanguage = HG.WebApp.Locales.GetUserLanguage();
        if (dateboxLanguages) {
            // Is the user's preferred language supported?
            if (dateboxLanguages[userLanguage]) {
                locale = userLanguage;
            } else if (userLanguage.length === 2) {
                // Is there a locale available with the user's language in it? Take the first one.
                $.each(dateboxLanguages, function (supportedLocale) {
                    if (supportedLocale.substring(0, 2) === userLanguage) {
                        locale = supportedLocale;
                        return false;
                    }
                });
            }
        }
        return locale;
    };

    // TODO: deprecate these two aliases at some point
    $$.SwitchPopup = HG.Ui.SwitchPopup;
    $$.JScrollToElement = HG.Ui.ScrollTo;

};