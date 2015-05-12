[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.min = ((typeof options[0] == 'undefined') ? 0 : parseFloat(options[0]));
        this.max = ((typeof options[1] == 'undefined') ? 100 : parseFloat(options[1]));
        this.step = ((typeof options[2] == 'undefined') ? 5 : parseFloat(options[2]));
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
        var valueLabel = element.find('[data-ui-field=value]');
        var sliderInput = element.find('[data-ui-field=sliderinput]');
        // reference to the slider object is only valid after it is created
        setTimeout(function() {
            var slider = element.find('[data-ui-field=sliderinput]')
            slider.val(context.parameter.Value)
            slider.attr('min', _this.min);
            slider.attr('max', _this.max);
            slider.attr('step', _this.step);
            slider.slider('refresh');        
            slider.on('change', function(evt){
                if (typeof _this.onChange == 'function') {
                    _this.onChange($(this).val());
                }
            });
        }, 500);
    }
}]