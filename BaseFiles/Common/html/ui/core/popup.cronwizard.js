[{
    bind: function() {
        var element = this.element;
        var context = this.context;
        var _this = this;
        
        // navbar buttons
        element.find('div[data-role=navbar]').find('li a').on('click', function(){
            var idx = $(this).attr('data-index');
            _this.element.find('[data-ui-field*="options-tabs-"]').hide();            
            _this.element.find('[data-ui-field=options-tabs-'+idx+']').show();            
        });
        
        // ok button
        element.find('[data-ui-field=confirm-button]').on('click', function(){
            if (typeof _this.onChange == 'function') {
                _this.onChange(_this._buildCron());
            }
        });
        
        // "each minute" control group
        var cg = $('<fieldset data-role="controlgroup" data-type="horizontal" data-mini="true" />');
        for(var d = 0; d < 60; d++) {
            var mn = d.toString(); if (mn.length==1) mn = '0'+mn;
            var cr = $('<input type="checkbox" id="minute-n-'+mn+'" value="'+d+'" /><label for="minute-n-'+mn+'">'+mn+'</label>');
            cg.append(cr);    
        }
        element.find('[data-ui-field*="minute-tab3"]').append(cg);
        element.find('[data-ui-field*="minute-tab3"]').trigger('create');
        // expression type select
        this._minuteTypeSelect = element.find('[data-ui-field=minute-type-select]').on('change', function(){
            element.find('[data-ui-field*="minute-tab"]').hide();
            element.find('[data-ui-field="minute-tab'+$(this).val()+'"]').show();    
            _this._buildCron();
        });       
        // every minute value change
        this._everyMinuteSlider = element.find('[data-ui-field="minute-tab2"]').find('input').on('change', function(){
            _this._buildCron();
        });
        // each minute selection change
        this._eachMinuteGroup = cg.on('change', function(){
            $(this).find("input[type=checkbox]:checked").each(function() {
                // ...
            });
            _this._buildCron();
        });
        
        // hour ui
        cg = $('<fieldset data-role="controlgroup" data-type="horizontal" data-mini="true" />');
        for(var d = 0; d < 24; d++) {
            var hr = d.toString(); if (hr.length==1) hr = '0'+hr;
            var cr = $('<input type="checkbox" id="hour-n-'+hr+'" value="'+d+'" /><label for="hour-n-'+hr+'">'+hr+'</label>');
            cg.append(cr);    
        }
        element.find('[data-ui-field*="hour-tab3"]').append(cg);
        element.find('[data-ui-field*="hour-tab3"]').trigger('create');
        // expression type select
        this._hourTypeSelect = element.find('[data-ui-field=hour-type-select]').on('change', function(){
            element.find('[data-ui-field*="hour-tab"]').hide();
            element.find('[data-ui-field="hour-tab'+$(this).val()+'"]').show();    
            _this._buildCron();
        });               
        // every hour value change
        this._everyHourSlider = element.find('[data-ui-field="hour-tab2"]').find('input').on('change', function(){
            _this._buildCron();
        });
        // each hour selection change
        this._eachHourGroup = cg.on('change', function(){
            $(this).find("input[type=checkbox]:checked").each(function() {
                // ...
            });
            _this._buildCron();
        });
        
        // day of month
        cg = $('<fieldset data-role="controlgroup" data-type="horizontal" data-mini="true" />');
        for(var d = 1; d <= 31; d++) {
            var hr = d.toString(); if (hr.length==1) hr = '0'+hr;
            var cr = $('<input type="checkbox" id="dayom-n-'+hr+'" value="'+d+'" /><label for="dayom-n-'+hr+'">'+hr+'</label>');
            cg.append(cr);    
        }
        element.find('[data-ui-field*="dayom-tab2"]').append(cg);
        element.find('[data-ui-field*="dayom-tab2"]').trigger('create');        
        // expression type select
        this._dayomTypeSelect = element.find('[data-ui-field=dayom-type-select]').on('change', function(){
            element.find('[data-ui-field*="dayom-tab"]').hide();
            element.find('[data-ui-field="dayom-tab'+$(this).val()+'"]').show();
            _this._buildCron();
        });               
        // each day of month selection change
        this._eachDayomGroup = cg.on('change', function(){
            $(this).find("input[type=checkbox]:checked").each(function() {
                // ...
            });
            _this._buildCron();
        });
        
        // month
        // expression type select
        this._monthTypeSelect = element.find('[data-ui-field=month-type-select]').on('change', function(){
            element.find('[data-ui-field*="month-tab"]').hide();
            element.find('[data-ui-field="month-tab'+$(this).val()+'"]').show();
            _this._buildCron();
        });
        // each month selection change
        this._eachMonthGroup = element.find('[data-ui-field="month-tab2"]').find('fieldset').on('change', function(){
            $(this).find("input[type=checkbox]:checked").each(function() {
                // ...
            });
            _this._buildCron();
        });
        
        // day of week
        // expression type select
        this._dayowTypeSelect = element.find('[data-ui-field=dayow-type-select]').on('change', function(){
            element.find('[data-ui-field*="dayow-tab"]').hide();
            element.find('[data-ui-field="dayow-tab'+$(this).val()+'"]').show();
            _this._buildCron();
        });               
        // each day of week selection change
        this._eachDayowGroup = element.find('[data-ui-field="dayow-tab2"]').find('fieldset').on('change', function(){
            $(this).find("input[type=checkbox]:checked").each(function() {
                // ...
            });
            _this._buildCron();
        });
    },
    open: function() {
        this._init();
        this.element.popup('open');
    },
    _init: function() {
        // initial values
        this.onChange = null;
        this.element.find('div[data-role=navbar]').find('li a').removeClass('ui-btn-active');
        this.element.find('div[data-role=navbar]').find('li:first a').addClass('ui-btn-active');
        this.element.find('[data-ui-field*="-tab"]').hide();
        this.element.find('[data-ui-field*="-tab1"]').show();
        this.element.find('[data-ui-field="options-tabs-1"]').show();
        this._minuteTypeSelect.val(1).selectmenu().selectmenu('refresh');
        this._hourTypeSelect.val(1).selectmenu().selectmenu('refresh');
        this._dayomTypeSelect.val(1).selectmenu().selectmenu('refresh');
        this._monthTypeSelect.val(1).selectmenu().selectmenu('refresh');
        this._dayowTypeSelect.val(1).selectmenu().selectmenu('refresh');
        this._buildCron();
    },
    _buildCron: function() {
        var _this = this;
        // minute field
        var min = '';
        var selection = this._minuteTypeSelect.val();
        if (selection == 1) {
            min = '*';
        } else if (selection == 2) {
            min = '*/'+this._everyMinuteSlider.val();
        } else if (selection == 3) {
            this._eachMinuteGroup.find("input[type=checkbox]:checked").each(function() {
                min += $(this).val()+',';
            });
            if (min == '')
                min = '*';
            else
                min = min.substring(0, min.length -1);
        }
        // hour field
        var hour = '';
        var selection = this._hourTypeSelect.val();
        if (selection == 1) {
            hour = '*';
        } else if (selection == 2) {
            hour = '*/'+this._everyHourSlider.val();
        } else if (selection == 3) {
            this._eachHourGroup.find("input[type=checkbox]:checked").each(function() {
                hour += $(this).val()+',';
            });
            if (hour == '')
                hour = '*';
            else
                hour = hour.substring(0, hour.length -1);
        }
        // dayom field
        var dayom = '';
        var selection = this._dayomTypeSelect.val();
        if (selection == 1) {
            dayom = '*';
        } else if (selection == 2) {
            this._eachDayomGroup.find("input[type=checkbox]:checked").each(function() {
                dayom += $(this).val()+',';
            });
            if (dayom == '')
                dayom = '*';
            else
                dayom = dayom.substring(0, dayom.length -1);
        }
        // month field
        var month = '';
        var selection = this._monthTypeSelect.val();
        if (selection == 1) {
            month = '*';
        } else if (selection == 2) {
            this._eachMonthGroup.find("input[type=checkbox]:checked").each(function() {
                month += $(this).val()+',';
            });
            if (month == '')
                month = '*';
            else
                month = month.substring(0, month.length -1);
        }
        // dayow field
        var dayow = '';
        var selection = this._dayowTypeSelect.val();
        if (selection == 1) {
            dayow = '*';
        } else if (selection == 2) {
            this._eachDayowGroup.find("input[type=checkbox]:checked").each(function() {
                dayow += $(this).val()+',';
            });
            if (dayow == '')
                dayow = '*';
            else
                dayow = dayow.substring(0, dayow.length -1);
        }
        var cronexpr = min + ' ' + hour + ' ' + dayom + ' ' + month + ' ' + dayow;
        $.get('/api/HomeAutomation.HomeGenie/Automation/Scheduling.Describe/'+encodeURIComponent(cronexpr), function(res){
            res = eval(res)[0];
            var displayExpr = cronexpr.replace(/ /g, '&nbsp;&nbsp;&nbsp;&nbsp;');
            _this.element.find('[data-ui-field=cron-expr]').html(displayExpr);
            _this.element.find('[data-ui-field=cron-desc]').html(res.ResponseValue);
        });
        return cronexpr;
    }
}]