[{
    // "store.script" feature field, recognized options:
    //   module:<store_name>:<item_name>
    //   program:<store_name>:<item_name>
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
        this.storeLocation = options[0]; // 'program' (shared) or 'module' (private)
        this.storeName = options[1];
        this.sourceDomain = this.storeLocation == 'program' ? this.program.Domain : this.module.Domain;
        this.sourceAddress = this.storeLocation == 'program' ? this.program.Address : this.module.Address;
    },
    bind: function() {
        var element = this.element;
        var context = this.context;
        var description = HG.WebApp.Locales.GetLocaleString(context.parameter.Name, context.parameter.Description);
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, description);
        element.html(html);
        var addButton = element.find('[data-ui-field=addbutton]');
        var editButton = element.find('[data-ui-field=editbutton]');
        var deleteButton = element.find('[data-ui-field=deletebutton]');
        var itemSelect = element.find('[data-ui-field=items]');
        var _this = this;
        addButton.on('click', function(evt){
            $('#automation_group_module_edit').popup('close');
            var title = '<small style="color:#efefef">'+_this.module.Name+' '+_this.module.Domain.split('.').pop()+':'+_this.module.Address+'</small><br/>'+_this.program.Name;
            var subtitle = context.parameter.Description+' &nbsp;&nbsp; <small style="color:#efefef">'+context.parameter.Name+'</small>';
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
                var subtitle = context.parameter.Description+' &nbsp;&nbsp; <small style="color:#efefef">'+context.parameter.Name+'</small>';
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
        var editButton = this.element.find('[data-ui-field=editbutton]');
        var deleteButton = this.element.find('[data-ui-field=deletebutton]');
        var itemSelect = this.element.find('[data-ui-field=items]');
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
        var itemSelect = this.element.find('[data-ui-field=items]');
        itemSelect.find('option:gt(0)').remove();
        HG.Configure.Stores.ItemList(this.sourceDomain, this.sourceAddress, this.storeName, function(res){
            var list = eval(res);
            $.each(list, function(k,v){
                itemSelect.append('<option>'+v.Name+'</option>');
            });
            itemSelect.val(_this.context.parameter.Value);
            itemSelect.selectmenu('refresh');
            _this.refreshButtons();
        });
    }
}]