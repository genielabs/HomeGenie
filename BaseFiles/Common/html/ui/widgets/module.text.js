[{
    init: function(program, module, options) {
        this.program = program;
        this.module = module;
        this.matchDomain = ((typeof options[0] == 'undefined' || options[0].toLowerCase() == 'any') ? '' : options[0].toLowerCase());
        this.matchType = ((typeof options[1] == 'undefined' || options[1].toLowerCase() == 'any') ? '' : options[1].toLowerCase());
        this.matchField = ((typeof options[2] == 'undefined' || options[2].toLowerCase() == 'any') ? '' : options[2].toLowerCase());
    },
    bind: function(element, context) {
        this.uiElement = element;
        this.context = context;
        var html = element.html();
        html = html.replace(/{id}/g, context.Index);
        html = html.replace(/{description}/g, context.Description);
        element.html(html);
        var _this = this;
        var textInput = element.find('[data-ui-field=textinput]');
        textInput.data('module-address', context.Value);
        textInput.on('change', function(evt){
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        }); 
        textInput.on('keyup', function() {
            _this.checkInput();            
        });
        textInput.on('blur', function() {
            _this.checkInput();            
        });
        textInput.autocomplete({
            minLength: 0,
            delay: 500,
            source: function (req, res){
                res(_this.searchModule(req.term));
            },
            select: function (event, ui) {
                //TODO: insert element
                textInput.val(ui.item.value);
                textInput.data('module-address', ui.item.module.Domain + ':' + ui.item.module.Address);
                _this.checkInput();
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                event.preventDefault();
                return false;
            },
            response: function (event, ui) {
                var domainAddress = typeof ui.content[0] != 'undefined' ? ui.content[0].module.Domain + ':' + ui.content[0].module.Address : '';
                if (ui.content.length == 1 && ((domainAddress.toLowerCase() == textInput.val().toLowerCase()) || (textInput.val().toLowerCase() == ui.content[0].value.toLowerCase()))) {
                    textInput.data('module-address', domainAddress);
                } else if (ui.content.length != 1) {
                    textInput.data('module-address', '');
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
    isOneOf: function(items, value){
        var matching = true;
        var words = items.split(',');
        if (typeof value == 'undefined') value = '';
        $.each(words, function(k, v){
            matching = (value.toLowerCase().indexOf(v) >= 0);
            return !matching;
        });
        return matching;
    },
    checkInput: function() {
        var textInput = this.uiElement.find('[data-ui-field=textinput]');
        var address = textInput.data('module-address');
        var verified = false;
        if (textInput.val() == '' && (typeof address == 'undefined' || address == '')) {
            verified = true;
        } else if (typeof address != 'undefined' && address.indexOf(':') > 0) {
            var ref = address.split(':');
            var mod = HG.WebApp.Utility.GetModuleByDomainAddress(ref[0], ref[1]);
            if (mod != null && (textInput.val() == '' || (textInput.val().toLowerCase() == mod.Name.toLowerCase()) || (textInput.val().toLowerCase() == address.toLowerCase()))) {
                textInput.val(mod.Name!=''?mod.Name:mod.Domain+':'+mod.Address);
                verified = true;
            }
        }
        if (verified) {
            textInput.parent().css('border-color', '');
        } else {
            address = '';
            textInput.data('module-address', '');
            textInput.parent().css('border-color', 'red');
        }
        //TODO: signal onChange event
        if (typeof this.onChange == 'function') {
            this.onChange(address);
        }
    },
    searchModule: function(filter) {
        var itemList = [];
        for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
            var groupName = HG.WebApp.Data.Groups[i].Name;
            var groupModules = HG.Configure.Groups.GetGroupModules(groupName);
            for (var m = 0; m < groupModules.Modules.length; m++) {
                var mod = groupModules.Modules[m];
                var label = mod.Name;
                if (label == null || label == '') label = mod.Domain+':'+mod.Address;
                var matchFilter = mod.Name + ':' + mod.Domain + ':' + mod.Address;
                var matchSearch = matchFilter.indexOf(filter) >= 0;
                if (this.matchDomain != '')
                    matchSearch = matchSearch && this.isOneOf(this.matchDomain, mod.Domain);
                if (this.matchType != '')
                    matchSearch = matchSearch && this.isOneOf(this.matchType, mod.DeviceType);
                if (this.matchField != '') {
                    var matching = false;
                    if (typeof mod.Properties != 'undefined') {
                        for(var p = 0; p < mod.Properties.length; p++) {
                            if (this.isOneOf(this.matchField, mod.Properties[p].Name)) {
                                matching = true;
                                break;
                            }
                        }
                    }
                    matchSearch = matchSearch && matching;
                }
                if (matchSearch)
                    itemList.push({ label: label, value: label, module: mod });
            }
        }
        return itemList;    
    }
}]