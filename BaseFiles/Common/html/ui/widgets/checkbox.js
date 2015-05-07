[{
    bind: function() {
        var element = this.element;
        var context = this.context;
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, context.parameter.Description);
        element.html(html);
        var _this = this;
        var checkBox = element.find('[data-ui-field=checkbox]');
        checkBox.prop('checked', context.parameter.Value != '' ? true : false);
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