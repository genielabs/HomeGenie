[{
    bind: function(element, context) {
        this.uiElement = element;
        this.context = context;
        var html = element.html();
        html = html.replace(/{id}/g, context.Index);
        html = html.replace(/{description}/g, context.Description);
        element.html(html);
        var _this = this;
        var textInput = element.find('[data-ui-field=textinput]');
        textInput.val(context.Value);
        textInput.on('change', function(evt){
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
    }
}]