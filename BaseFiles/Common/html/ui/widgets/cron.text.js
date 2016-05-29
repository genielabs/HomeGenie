[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.nowizard = ((typeof options[0] != 'undefined' && options[0].toLowerCase() == 'nowizard') ? true : false);
    },
    bind: function() {
        var element = this.element;
        var context = this.context;
        var description = HG.WebApp.Locales.GetProgramLocaleString(context.program.Address, context.parameter.Name, context.parameter.Description);
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, description);
        element.html(html);
        var _this = this;
        this.config = HG.WebApp.Utility.GetModulePropertyByName(this.module, context.parameter.Name+'.Data');
        if (this.config != null && this.config.Value != '')
            this.config = $.parseJSON(this.config.Value);
        var textDescription = element.find('[data-ui-field=description]');
        var textInput = element.find('[data-ui-field=textinput]');
        textInput.val(context.parameter.Value);
        textInput.on('change', function(evt){
            if (_this.config != null) {
                HG.WebApp.Utility.SetModulePropertyByName(_this.module, context.parameter.Name+'.Data', '');
                _this.config = null;
            }
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
        textDescription.qtip({
            content: {
                text: function(){ return textDescription.val(); }
            },
            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
            position: { my: 'bottom center', at: 'top center' }
        });
        if (this.nowizard) {
            element.find('[data-ui-field=cronwiz]').hide();
        } else {
            element.find('[data-ui-field=cronwiz]').on('click', function(){
                $('#automation_group_module_edit').one('popupafterclose', function(){
                    HG.Ui.Popup.CronWizard.element.one('popupafterclose', function(){
                        $('#automation_group_module_edit').popup('open');
                    });

                    if (_this.config != null)
                        HG.Ui.Popup.CronWizard.config = _this.config;

                    HG.Ui.Popup.CronWizard.open();
                    HG.Ui.Popup.CronWizard.onChange = function(expr, cfg){
                        //var ctxt = textInput.val();
                        //ctxt = ctxt.substring(0, _this.getLastSeparator(ctxt));
                        //textInput.val(ctxt+expr);
                        textInput.val(expr);
                        textInput.trigger('change');
                        _this.config = cfg; // <-- this has to be placed after 'change' and before 'blur'
                        textInput.blur();
                        HG.WebApp.Utility.SetModulePropertyByName(_this.module, context.parameter.Name+'.Data', JSON.stringify(cfg));
                    };
                });
                $('#automation_group_module_edit').popup('close');
            });
        }
        textInput.on('blur', function(evt){
            if (textInput.val() == '') {
                textDescription.val(HG.WebApp.Locales.GetLocaleString('common_status_notset', 'Not set'));
                textDescription.css('color', 'gray');
            } else {
                textDescription.val(textInput.val());
                textDescription.css('color', '');
                if (_this.config != null && typeof _this.config.description != 'undefined' && _this.config.description != '') {
                    textDescription.val(_this.config.description);
                } else {
                    if(_this.getLastSeparator(textInput.val()) == 0) {
                        $.get('/api/HomeAutomation.HomeGenie/Automation/Scheduling.Describe/'+encodeURIComponent(textInput.val()), function(res){
                            if (typeof res != 'undefined' && res.ResponseValue != '') {
                                textDescription.val(res.ResponseValue);
                            }
                        });
                    }
                }
            }
            setTimeout(function(){
                textInput.parent().hide();
                textDescription.parent().show();
            }, 200);
        });

        textInput.autocomplete({
            minLength: 0,
            delay: 500,
            source: function (req, res){
                $.ajax({
                    url: '/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.HomeGenie/Automation/Scheduling.List',
                    type: 'GET',
                    success: function (data) {
                        var filter = _this.getFilter(req.term);
                        var itemList = [];
                        $.each(data, function(idx, item){
                            if (item.IsEnabled && (item.Name.toLowerCase().indexOf(filter) >= 0 || item.Description.toLowerCase().indexOf(filter) >= 0))
                                itemList.push({ label: '@'+item.Name+' ('+item.Description+')', value: '@'+item.Name });
                        });
                        res(itemList);
                    },
                    failure: function (data) {
                        res([]);
                    }
                });
            },
            select: function (event, ui) {
                var ctxt = textInput.val();
                ctxt = ctxt.substring(0, _this.getLastSeparator(ctxt));
                textInput.val(ctxt+ui.item.value);
                textInput[0].setSelectionRange(textInput.val().length, textInput.val().length);
                textInput.trigger('change');
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                event.preventDefault();
                return false;
            },
            response: function (event, ui) {
                textInput.trigger('change');
            },
            close: function (event, ui) {
                textInput.focus();
                return true;
            }
        }).focus(function () {
            $(this).trigger('keydown.autocomplete');
        });

        textDescription.on('focus', function(evt){
            $(this).trigger('click');
        });
        textDescription.on('click', function(evt){
            textDescription.parent().hide();
            textInput.parent().show();
            setTimeout(function(){
                textInput.focus();
            }, 500);
        });
        setTimeout(function(){
            textInput.blur();
        }, 200);
    },
    getLastSeparator: function(s) {
        var lastIdx = 0;
        var idxS1 = s.lastIndexOf(':');
        var idxS2 = s.lastIndexOf(';');
        if (idxS1 > idxS2)
            lastIdx = idxS1;
        else
            lastIdx = idxS2;
        idxS1 = s.lastIndexOf('(');
        idxS2 = s.lastIndexOf(')');
        if (idxS1 > idxS2 && idxS1 > lastIdx)
            lastIdx = idxS1;
        else if (idxS2 > lastIdx)
            lastIdx = idxS2;
        return lastIdx+1;
    },
    getFilter: function(terms) {
        var filter = terms.toLowerCase();
        filter = filter.substring(this.getLastSeparator(filter));
        return filter.replace('@', '');
    },
    setValue: function(expr) {
        var textInput = this.element.find('[data-ui-field=textinput]');
        textInput.val(expr);
        textInput.trigger('change');
        textInput.blur();
    }
}]