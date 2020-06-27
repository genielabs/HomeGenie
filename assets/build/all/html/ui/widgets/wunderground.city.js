[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.autoCompleteUrl = '/api/HomeAutomation.OpenWeatherMap/2.5/Search.Location/';
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
        textInput.on('change', function(evt) {
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
        textInput.on('keyup', function() {
            // TODO: ?
        });
        textInput.on('blur', function() {
            // TODO: ?
        });
        textInput.autocomplete({
            minLength: 0,
            delay: 500,
            source: function (req, res){
                $.ajax({
                   type: "GET",
                   success: function(data){
                     var locations = [];
                     $.each(data, function(k,v) {
                         var location = v.name + ', ' + v.sys.country;
                         locations.push(location);
                     });
                     res(locations);
                   },
                   url: _this.autoCompleteUrl + req.term
                });
            },
            select: function (event, ui) {
                textInput.val(ui.item.value)
                textInput.trigger('change');
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                event.preventDefault();
                return false;
            },
            response: function (event, ui) {
                //console.log(ui);
            },
            close: function (event, ui) {
                textInput.focus();
                return true;
            }
        }).focus(function () {
            $(this).trigger('keydown.autocomplete');
        });
    }
}]
