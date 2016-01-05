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
        var textDescription = element.find('[data-ui-field=description]');
        var textInput = element.find('[data-ui-field=textinput]');
        textInput.val(context.parameter.Value);
        textInput.on('change', function(evt){
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
        if (this.nowizard) {
            element.find('[data-ui-field=cronwiz]').hide();
        } else {
            element.find('[data-ui-field=cronwiz]').on('click', function(){
                $('#automation_group_module_edit').one('popupafterclose', function(){
                    HG.Ui.Popup.CronWizard.element.one('popupafterclose', function(){
                        $('#automation_group_module_edit').popup('open');
                    });
                    HG.Ui.Popup.CronWizard.open();
                    HG.Ui.Popup.CronWizard.onChange = function(expr){
                        var ctxt = textInput.val();
                        textInput.val(ctxt+expr);
                        textInput.trigger('change');
                        textInput.blur();
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
                $.get('/api/HomeAutomation.HomeGenie/Automation/Scheduling.Describe/'+encodeURIComponent(textInput.val()), function(res){
                    if (typeof res != 'undefined' && res.ResponseValue != '') {
                        textDescription.val(res.ResponseValue);
                    }
                });
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
                        var itemList = [];
                        $.each(data, function(idx, item){
                            if (item.IsEnabled)
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
                textInput.val(ctxt+ui.item.value);
                textInput.trigger('change');
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                event.preventDefault();
                return false;
            },
            response: function (event, ui) {
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
    setValue: function(expr) {
        var textInput = this.element.find('[data-ui-field=textinput]');
        textInput.val(expr);
        textInput.trigger('change');
        textInput.blur();
    }
}]