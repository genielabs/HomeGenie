[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.eventParameter = ((typeof options[0] == 'undefined') ? '' : options[0]);;
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
        var textInput = element.find('[data-ui-field=textinput]');
        textInput.val(context.parameter.Value);
        textInput.on('change', function(evt){
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
        var captureButton = element.find('[data-ui-field=capture]');
        captureButton.on('click', function() {
            _this._captureEnabled = true;
            captureButton.qtip({ 
                content: "Waiting for a '"+_this.eventParameter+"' event",
                show: { event: false, ready: true, delay: 500 },
                hide: { event: false, inactive: 2500 },
                style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                position: { my: 'bottom center', at: 'top center' }
            });
            // listen to HG events for 10 seconds
            HG.WebApp.Events.AddListener(_this);
            setTimeout(function(){
                if (_this._captureEnabled) {
                    _this._captureEnabled = false;
                    HG.WebApp.Events.RemoveListener(_this);
                    captureButton.qtip({ 
                        content: "Capture timeout!",
                        show: { event: false, ready: true, delay: 500 },
                        hide: { event: false, inactive: 2500 },
                        style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                        position: { my: 'bottom center', at: 'top center' }
                    });
                }
            }, 10000);
        });
        $('#automation_group_module_edit').on('popupafterclose', function(){
            _this._captureEnabled = false;
            HG.WebApp.Events.RemoveListener(_this);
        });
    },
    parameterEventCallback: function(module, event) {
        if (this._captureEnabled && event.Property.indexOf(this.eventParameter) == 0) {
            this._captureEnabled = false;
            HG.WebApp.Events.RemoveListener(this);
            var textInput = this.element.find('[data-ui-field=textinput]');
            textInput.val(event.Value).trigger('change');
            textInput.qtip({ 
                content: "Value captured",
                show: { event: false, ready: true, delay: 500 },
                hide: { event: false, inactive: 2500 },
                style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
                position: { my: 'right center', at: 'left center' }
            });
        }
    }
}]