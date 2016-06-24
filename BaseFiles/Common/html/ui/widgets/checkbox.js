[{
    bind: function() {
        var element = this.element;
        var context = this.context;
        var description = HG.WebApp.Locales.GetProgramLocaleString(context.program.Address, context.parameter.Name, context.parameter.Description);
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, description);
        element.html(html);
        var _this = this;
        var checkBox = element.find('[data-ui-field=checkbox]');
        checkBox.prop('checked', context.parameter.Value != '' && context.parameter.Value.toLowerCase() != 'false' && context.parameter.Value != '0'  ? true : false);
        checkBox.on('change', function(evt){
            if (typeof _this.onChange == 'function') {
                if ($(this).is(':checked'))
                    _this.onChange('On');
                else
                    _this.onChange('');
            }
        });
    }
}]