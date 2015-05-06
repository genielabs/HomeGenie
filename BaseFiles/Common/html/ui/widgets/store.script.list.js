[{
    // "store.script" feature field, recognized options:
    //   module:<store_name>:<item_name>
    //   program:<store_name>:<item_name>
    init: function(program, module, options) {
        this.program = program;
        this.module = module;
        this.storeLocation = options[0]; // 'program' (shared) or 'module' (private)
        this.storeName = options[1];
        this.sourceDomain = this.storeLocation == 'program' ? this.program.Domain : this.module.Domain;
        this.sourceAddress = this.storeLocation == 'program' ? this.program.Address : this.module.Address;
    },
    bind: function(element, context) {
        this.uiElement = element;
        this.context = context;
        var html = element.html();
        html = html.replace(/{id}/g, context.Index);
        html = html.replace(/{description}/g, context.Description);
        element.html(html);
        var addButton = element.find('[data-ui-field=addbutton]');
        var editButton = element.find('[data-ui-field=editbutton]');
        var deleteButton = element.find('[data-ui-field=deletebutton]');
        var itemSelect = element.find('[data-ui-field=items]');
        var _this = this;
        addButton.on('click', function(evt){
            $('#automation_group_module_edit').popup('close');
            var title = '<small style="color:#efefef">'+_this.module.Name+' '+_this.module.Domain.split('.').pop()+':'+_this.module.Address+'</small><br/>'+_this.program.Name;
            var subtitle = context.Description+' &nbsp;&nbsp; <small style="color:#efefef">'+context.Name+'</small>';
            HG.WebApp.Utility.EditorPopup('', title, subtitle, '', function(res) {
                $('#automation_group_module_edit').popup('open');
                if (!res.isCanceled) {
                    HG.Configure.Stores.ItemSet(_this.sourceDomain, _this.sourceAddress, _this.storeName, res.name, res.text, function(res){
                        _this.refreshItemList();
                    });
                }
            });        
        });
        editButton.on('click', function(evt){
            // open full screen editor
            $('#automation_group_module_edit').popup('close');
            $.mobile.loading('show');
            HG.Configure.Stores.ItemGet(_this.sourceDomain, _this.sourceAddress, _this.storeName, itemSelect.val(), function(res){
                $.mobile.loading('hide');
                var value = eval('['+res+']')[0].Value;
                var title = '<small style="color:#efefef">'+_this.module.Name+' '+_this.module.Domain.split('.').pop()+':'+_this.module.Address+'</small><br/>'+_this.program.Name;
                var subtitle = context.Description+' &nbsp;&nbsp; <small style="color:#efefef">'+context.Name+'</small>';
                HG.WebApp.Utility.EditorPopup(itemSelect.val(), title, subtitle, value, function(res) {
                    $('#automation_group_module_edit').popup('open');
                    if (!res.isCanceled) {
                        HG.Configure.Stores.ItemSet(_this.sourceDomain, _this.sourceAddress, _this.storeName, res.name, res.text, function(res){
                        });
                    }
                });            
            });
        });
        deleteButton.on('click', function(evt){
            HG.Configure.Stores.ItemDelete(_this.sourceDomain, _this.sourceAddress, _this.storeName, itemSelect.val(), function(res){
                _this.refreshItemList();
            });
        });
        deleteButton.addClass('ui-disabled');
        editButton.addClass('ui-disabled');
        itemSelect.on('change', function(){
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
            _this.refreshButtons();
        });
        this.refreshItemList();
    },
    refreshButtons: function(){
        var editButton = this.uiElement.find('[data-ui-field=editbutton]');
        var deleteButton = this.uiElement.find('[data-ui-field=deletebutton]');
        var itemSelect = this.uiElement.find('[data-ui-field=items]');
        if (itemSelect.val() == '') {
            deleteButton.addClass('ui-disabled');
            editButton.addClass('ui-disabled');
        } else {
            deleteButton.removeClass('ui-disabled');
            editButton.removeClass('ui-disabled');
        }
    },
    refreshItemList: function(){
        var _this = this;
        var itemSelect = this.uiElement.find('[data-ui-field=items]');
        itemSelect.find('option:gt(0)').remove();
        HG.Configure.Stores.ItemList(this.sourceDomain, this.sourceAddress, this.storeName, function(res){
            var list = eval(res);
            $.each(list, function(k,v){
                itemSelect.append('<option>'+v.Name+'</option>');
            });
            itemSelect.val(_this.context.Value);
            itemSelect.selectmenu('refresh');
            _this.refreshButtons();
        });
    }
}]