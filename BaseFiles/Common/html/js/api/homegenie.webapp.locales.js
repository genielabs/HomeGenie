HG.WebApp.Data._DefaultLocale = {};
HG.WebApp.Data._CurrentLocale = {};
//
// namespace : HG.WebApp.Locales namespace
// info      : UI localization methods
//
HG.WebApp.Locales = HG.WebApp.Locales || new function(){ var $$ = this;

    $$.GetUserLanguage = function()
    {
        var userLang = (navigator.languages ? navigator.languages[0] : (navigator.language || navigator.userLanguage));
        if (userLang.length > 2) userLang = userLang.substring(0, 2);
        return userLang;
    };

    $$.GetDateEndianType = function()
    {
        // L = Little Endian -> DMY
        // M = Middle Endian -> MDY
        var endianType = 'L';
        var testDate = new Date(98326800000);
        var localeDateParts = testDate.toLocaleDateString().replace(/[\u200E]/g, "").split('/');
        if (localeDateParts[0] == '2') endianType = 'M';
        return endianType;
    };

    $$.GetTemperatureUnit = function()
    {
        var temperatureUnit = dataStore.get('UI.TemperatureUnit');
        if (temperatureUnit != 'C' && (temperatureUnit == 'F' || $$.GetDateEndianType() == 'M'))
            return 'Fahrenheit';
        else
            return 'Celsius';
    };

    $$.GetDefault = function(callback) {
        $.ajax({
            url: './locales/en.json',
            type: 'GET',
            success: function (data) {
                HG.WebApp.Data._DefaultLocale = $.parseJSON( data );
                callback();
                $.ajax({
                    url: './locales/en.programs.json',
                    type: 'GET',
                    success: function (pdata) {
                        HG.WebApp.Data._DefaultLocale = $.extend(HG.WebApp.Data._DefaultLocale, $.parseJSON( pdata ));
                    }
                });
            }
        });
    };

    $$.Load = function(langurl, callback) {
        // get data via ajax
        // store it in HG.WebApp.Data._CurrentLocale
        // and replace locales strings in the current page
        $$.GetDefault(function(){
            $.ajax({
                url: langurl,
                type: 'GET',
                success: function (data) {
                    HG.WebApp.Data._CurrentLocale = $.extend(HG.WebApp.Data._CurrentLocale, $.parseJSON( data ));
                    if (typeof callback == 'function') callback(true);
                },
                error: function(xhr, status, error) {
                    console.log('WARNING (Locales.Load): "'+langurl+'" '+xhr.status+' '+xhr.statusText);
                    if (typeof callback == 'function') callback(false);
                }
            });
        });
    };

    $$.Localize = function(container) {
        $(container).find('[data-locale-id]').each(function(index){
            var stringid = $(this).attr('data-locale-id');
            var text = $$.GetLocaleString(stringid);
            if (text != null) {
                $this = $(this);
                if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                    $('span.ui-btn-text', $this).text(text);
                }
                else if( $this.is('input') ) {
                    $this.attr("placeholder", text);
                }
                else {
                    $(this).html(text);
                }
            }
        });
    };

    $$.LocalizeElement = function(elementId, locale) {
        $(elementId).find('[data-locale-id]').each(function(index){
            var stringid = $(this).attr('data-locale-id');
            var text = $$.GetLocaleString(stringid, false, locale);
            if (text != null) {
                $this = $(this);
                if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                    $('span.ui-btn-text', $this).text(text);
                }
                else if( $this.is('input') ) {
                    $this.attr("placeholder", text);
                }
                else {
                    $(this).html(text);
                }
            }
        });

    };

    $$.GetLocaleString = function(stringid, defaultValue, locale)
    {
        var retval = null;
        // try user provided locale
        if (locale)
        $.each(locale, function(key, value) {
            if (key == stringid)
            {
                retval = value;
                return false; // break each
            }
        });
        // try current locale
        if (retval == null)
        $.each(HG.WebApp.Data._CurrentLocale, function(key, value) {
            if (key == stringid)
            {
                retval = value;
                return false; // break each
            }
        });
        // fallback to default locale
        if (retval == null)
        $.each(HG.WebApp.Data._DefaultLocale, function(key, value) {
            if (key == stringid)
            {
                retval = value;
                return false; // break each
            }
        });
        if (retval == null)
            console.log('WARNING (Locales.GetLocaleString): "' + stringid + '" is undefined.');
        return (retval == null && defaultValue ? defaultValue : retval);
    };

    $$.LocalizeWidget = function(widgetpath, elementid, callback) {
        var userLang = $$.GetUserLanguage();
        widgetpath = widgetpath.substring(0, widgetpath.lastIndexOf('/'));
        var container = '#' + elementid;
        var langurl = 'pages/control/widgets/' + widgetpath + '/locales/' + userLang.toLowerCase().substring(0, 2) + '.json';
        $.ajax({
            url: langurl,
            type: 'GET',
            success: function (data) {
                var locale = $.parseJSON( data );
                $(container).find('[data-ui-field=widget]').data('Locale', locale);

                $(container).find('[data-locale-id]').each(function(index){
                    var stringid = $(this).attr('data-locale-id');
                    var text = $$.GetLocaleString(stringid, false, locale);
                    if (text != null) {
                        $this = $(this);
                        if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                            $('span.ui-btn-text', $this).text(text);
                        }
                        else if( $this.is('input') ) {
                            $this.attr("placeholder", text);
                        }
                        else {
                            $(this).html(text);
                        }
                    }
                });
                // localizable strings
                $(container).find('[data-localizable]').each(function(index){
                    var stringid = $(this).text();
                    var text = $$.GetLocaleString(stringid, false, locale);
                    if (text != null) {
                        $(this).text(text);
                    }
                });
                // try to localize widget's popups if they were already processed by jQuery popup() function
                var popups = $(container).find('[data-ui-field=widget]').data('ControlPopUp');
                if (popups)
                popups.each(function (index) {
                    var popup = $(this);
                    $(popup).find('[data-locale-id]').each(function(index){
                        var stringid = $(this).attr('data-locale-id');
                        var text = $$.GetLocaleString(stringid, false, locale);
                        if (text != null) {
                            $this = $(this);
                            if( $this.is('a') && $('span.ui-btn-text', $this).is('span') ) {
                                $('span.ui-btn-text', $this).text(text);
                            }
                            else if( $this.is('input') ) {
                                $this.attr("placeholder", text);
                            }
                            else {
                                $(this).html(text);
                            }
                        }
                    });
                });
                if (typeof callback == 'function') 
                    callback();
            },
            error: function(xhr, status, error) {
                console.log('WARNING (Locales.LocalizeWidget): "'+langurl+'" '+xhr.status+' '+xhr.statusText);
                if (typeof callback == 'function') 
                    callback();
            }
        });
    };

    $$.GetWidgetLocaleString = function(widget, stringid, defaultValue) {
        var retval = null;
        if(widget == null || typeof(widget) == 'undefined' || typeof(widget.data('Locale')) == 'undefined')
            return (defaultValue ? defaultValue : null);
        retval = $$.GetLocaleString(stringid, false, widget.data('Locale'));
        return (retval == null && defaultValue ? defaultValue : retval);
    };

    $$.GetProgramLocaleString = function(programAddress, stringId, defaultValue) {
        var response = defaultValue;
        var plocale;
        var hasLocale = eval('(HG.WebApp.Data._CurrentLocale.Programs && HG.WebApp.Data._CurrentLocale.Programs['+programAddress+'])');
        if (hasLocale)
            plocale = eval('HG.WebApp.Data._CurrentLocale.Programs['+programAddress+']');
        else {
            hasLocale = eval('(HG.WebApp.Data._DefaultLocale.Programs && HG.WebApp.Data._DefaultLocale.Programs['+programAddress+'])');
            if (hasLocale)
                plocale = eval('HG.WebApp.Data._DefaultLocale.Programs['+programAddress+']');
        }
        if (typeof plocale != 'undefined') {
            response = $$.GetLocaleString(stringId, defaultValue, plocale);
        }
        return response;
    };

    // use this to re-create the UI localization template
    $$.GenerateTemplate = function()
    {
        var localestring = '';
        $(document).find('[data-locale-id]').each(function(index){
            var stringid = $(this).attr('data-locale-id');
            var value = $(this).html().trim();
            if (localestring.indexOf('"' + stringid + '\"') < 0)
            {
                localestring += '\t\"' + stringid + '\": \n\t\t \"' + value + '\",\n';
            }
        });
        console.log( localestring );
    };

};