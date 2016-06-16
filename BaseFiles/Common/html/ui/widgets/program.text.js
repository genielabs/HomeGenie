[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.initialValue = this.context.parameter.Value;
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
        textInput.data('program-address', context.parameter.Value);
        textInput.on('keyup', function() {
            _this.checkInput();
        });
        textInput.on('blur', function() {
            _this.checkInput();
            var inputValue = textInput.data('program-address');
            if (_this.initialValue != inputValue && typeof _this.onChange == 'function') {
                _this.onChange(inputValue);
                _this.initialValue = inputValue;
            }
        });
        textInput.autocomplete({
            minLength: 0,
            delay: 500,
            source: function (req, res){
                res(_this.searchProgram(req.term));
            },
            select: function (event, ui) {
                textInput.val(ui.item.label);
                textInput.data('program-address', ui.item.program.Address.toString());
                _this.checkInput();
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                event.preventDefault();
                return false;
            },
            response: function (event, ui) {
                var address = typeof ui.content[0] != 'undefined' ? ui.content[0].program.Address.toString() : '';
                if (ui.content.length == 1 && ((address.toLowerCase() == textInput.val().toLowerCase()) || (textInput.val().toLowerCase() == ui.content[0].value.toLowerCase()))) {
                    textInput.data('program-address', address);
                } else if (ui.content.length != 1) {
                    textInput.data('program-address', '');
                }
                _this.checkInput();
            },
            close: function (event, ui) {
                textInput.focus();
                return true;
            }
        }).focus(function () {
            $(this).trigger('keydown.autocomplete');
        });
        this.checkInput();
    },
    checkInput: function() {
        var textInput = this.element.find('[data-ui-field=textinput]');
        var address = textInput.data('program-address');
        var verified = false;
        if (textInput.val() == '' && (typeof address == 'undefined' || address == '')) {
            verified = true;
        } else if (typeof address != 'undefined') {
            var prog = HG.WebApp.Utility.GetProgramByAddress(address);
            if (prog != null && (textInput.val() == '' || (textInput.val().toLowerCase() == prog.Name.toLowerCase()))) {
                if (textInput.val() != prog.Name)
                    textInput.val(prog.Name);
                verified = true;
            }
        }
        if (verified) {
            textInput.parent().css('border-color', '');
        } else {
            address = '';
            textInput.data('program-address', '');
            textInput.parent().css('border-color', 'red');
        }
    },
    searchProgram: function(filter) {
        var itemList = [];
        for (var i = 0; i < HG.WebApp.Data.Programs.length; i++) {
            var prog = HG.WebApp.Data.Programs[i];
            var matchFilter = (prog.Address+':'+prog.Name+':'+prog.Description).toLowerCase();
            var matchSearch = matchFilter.indexOf(filter.toLowerCase()) >= 0;
            if (matchSearch)
                itemList.push({ label: prog.Name, value: prog.Address.toString(), program: prog });
        }
        return itemList;
    }
}]