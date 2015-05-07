[{
    bind: function() {
        var element = this.element;
        var context = this.context;
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, context.parameter.Description);
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
        element.find('[data-ui-field=cronwiz]').on('click', function(){
            $('#automation_group_module_edit').one('popupafterclose', function(){
                HG.Ui.Popup.CronWizard.element.one('popupafterclose', function(){
                    $('#automation_group_module_edit').popup('open');
                });
                HG.Ui.Popup.CronWizard.open();
                HG.Ui.Popup.CronWizard.onChange = function(expr){
                    textInput.val(expr);
                    textInput.trigger('change');
                    textInput.blur();
                };
            });
            $('#automation_group_module_edit').popup('close');
        });
        textInput.on('blur', function(evt){
            textDescription.val(textInput.val());
            $.get('/api/HomeAutomation.HomeGenie/Automation/Scheduling.Describe/'+encodeURIComponent(textInput.val()), function(res){
                res = eval(res)[0];
                if (typeof res != 'undefined' && res.ResponseValue != '') {
                    textDescription.val(res.ResponseValue);
                }
            });
            setTimeout(function(){
                textInput.parent().hide();
                textDescription.parent().show();
            }, 200);
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
    }
}]