[{
    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
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
        this.cronSelect = element.find('[data-ui-field=cronselect]').qtip({
            prerender: true,
            content: {
                text: function(){ 
                    var desc = _this.cronSelect.find('option:selected').attr('desc');
                    if (typeof desc == 'undefined')
                        desc = _this.cronSelect.find('option:selected').attr('value');
                    return desc; 
                }
            },
            show: { delay: 500 },
            hide: { inactive: 3000 },
            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
            position: { my: 'bottom center', at: 'top center' }
        }).on('change', function(){
            if ($(this).val()=='')
                element.find('[data-ui-field=cronwiz-edit]').addClass('ui-disabled');
            else
                element.find('[data-ui-field=cronwiz-edit]').removeClass('ui-disabled');
            if (typeof _this.onChange == 'function') {
                _this.onChange($(this).val());
            }
        });
        element.find('[data-ui-field=cronwiz-addnew]').on('click', function(){
            HG.Ui.Popup.CronWizard.onChange = function(item){
                HG.Automation.Scheduling.Update(item.Name, item.CronExpression, item.Data, item.Description, item.Script, function () {
                    _this.refreshCronList('@'+item.Name);
                    _this.cronSelect.trigger('change');
                });
            };
            if ($('#automation_group_module_edit-popup').hasClass('ui-popup-active')) {
                $('#automation_group_module_edit').one('popupafterclose', function(){
                    HG.Ui.Popup.CronWizard.element.one('popupafterclose', function(){
                        $('#automation_group_module_edit').popup('open');
                    });
                    HG.Ui.Popup.CronWizard.open();
                });
                $('#automation_group_module_edit').popup('close');
            } else {
                HG.Ui.Popup.CronWizard.open();
            }
        });
        element.find('[data-ui-field=cronwiz-edit]').on('click', function(){
            HG.Ui.Popup.CronWizard.onChange = function(item){
                HG.Automation.Scheduling.Update(item.Name, item.CronExpression, item.Data, item.Description, item.Script, function () {
                    _this.refreshCronList('@'+item.Name);
                    _this.cronSelect.trigger('change');
                });
            };
            if ($('#automation_group_module_edit-popup').hasClass('ui-popup-active')) {
                $('#automation_group_module_edit').one('popupafterclose', function(){
                    HG.Ui.Popup.CronWizard.element.one('popupafterclose', function(){
                        $('#automation_group_module_edit').popup('open');
                    });
                    HG.Ui.Popup.CronWizard.open(_this.cronSelect.find('option:selected').text());
                });
                $('#automation_group_module_edit').popup('close');
            } else {
                HG.Ui.Popup.CronWizard.open(_this.cronSelect.find('option:selected').text());
            }
        });
        this.refreshCronList(context.parameter.Value);
        this.cronSelect.trigger('change');
    },
    setValue: function(expr) {
        this.cronSelect.val(expr).selectmenu('refresh');
    },
    refreshCronList: function(currentItem) {
        var _this = this;
        $.mobile.loading('show');
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.HomeGenie/Automation/Scheduling.List',
            type: 'GET',
            success: function (data) {
                _this.schedulerItems = data;
                _this.cronSelect.empty();
                var notSet = $('<option />');
                var notSetText = HG.WebApp.Locales.GetLocaleString('common_status_notset', 'Not set');
                notSet.attr({ 'value': '', 'desc': notSetText }).text(notSetText);
                _this.cronSelect.append(notSet);
                var found = false;
                $.each(data, function(k,v) {
                    var entry = $('<option/>')
                        .attr({'value': '@'+v.Name, 'desc': v.Description })
                        .text(v.Name);
                    if (currentItem == '@'+v.Name)
                        found = true;
                    _this.cronSelect.append(entry);
                });
                if (!found && currentItem != '' && typeof currentItem != 'undefined') {
                    var entry = $('<option/>')
                        .attr({'value': currentItem, 'desc': currentItem })
                        .text(currentItem);
                    _this.cronSelect.append(entry);
                }
                _this.cronSelect.val(currentItem).selectmenu('refresh');
                if (typeof currentItem != 'undefined' || currentItem == '')
                    _this.element.find('[data-ui-field=cronwiz-edit]').addClass('ui-disabled');

                $.mobile.loading('hide');
            },
            failure: function (data) {
                // TODO: handle this...
            }
        });
    }
}]