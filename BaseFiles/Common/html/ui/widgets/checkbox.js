[{
    bind: function(element, context) {
        this.uiElement = element;
        this.context = context;
        var html = element.html();
        html = html.replace(/{id}/g, context.Index);
        html = html.replace(/{description}/g, context.Description);
        element.html(html);
        var _this = this;
        var checkBox = element.find('[data-ui-field=checkbox]');
        checkBox.prop('checked', context.Value != '' ? true : false);
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