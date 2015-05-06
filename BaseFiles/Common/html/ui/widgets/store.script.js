[{
    // "store.script" feature field, recognized options:
    //   module:<store_name>:<item_name>
    //   program:<store_name>:<item_name>
    init: function(program, module, options) {
        this.program = program;
        this.module = module;
        this.storeLocation = options[0]; // 'program' (shared) or 'module' (private)
        this.storeName = options[1];
        this.bindItem = options[2];
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
        var editButton = element.find('[data-ui-field=editbutton]');
        var _this = this;
        editButton.on('click', function(evt){
            // open full screen editor
            $('#automation_group_module_edit').popup('close');
            $.mobile.loading('show');
            HG.Configure.Stores.ItemGet(_this.sourceDomain, _this.sourceAddress, _this.storeName, _this.bindItem, function(res){
                $.mobile.loading('hide');
                var value = eval('['+res+']')[0].Value;
                var title = '<small style="color:#efefef">'+_this.module.Name+' '+_this.module.Domain.split('.').pop()+':'+_this.module.Address+'</small><br/>'+_this.program.Name;
                var subtitle = context.Description+' &nbsp;&nbsp; <small style="color:#efefef">'+context.Name+'</small>';
                HG.WebApp.Utility.EditorPopup(_this.bindItem, title, subtitle, value, function(res) {
                    $('#automation_group_module_edit').popup('open');
                    if (!res.isCanceled) {
                        HG.Configure.Stores.ItemSet(_this.sourceDomain, _this.sourceAddress, _this.storeName, _this.bindItem, res.text, function(res){
                        });
                    }
                    //if (typeof _this.onChange == 'function') {
                    //    _this.onChange(...);
                    //}
                });            
            });
        });
    }
}]