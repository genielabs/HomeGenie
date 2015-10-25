[{
    maxPrograms: 10,
    textNoProgram: 'no program',
    textProgramNr: 'program #',

    init: function(options) {
        this.program = this.context.program;
        this.module = this.context.module;
    },

    bind: function() {
        var _this = this;
        var element = this.element;
        var context = this.context;

        // TODO: change this upon context.module.DeviceType
        this.TableGroup = 'Level';

        // Text localization
        var description = HG.WebApp.Locales.GetProgramLocaleString(context.program.Address, context.parameter.Name, context.parameter.Description);
        var html = element.html();
        html = html.replace(/{id}/g, context.parameter.Index);
        html = html.replace(/{description}/g, '<strong>'+description+'</strong>');
        element.html(html);
        this.textNoProgram = HG.WebApp.Locales.GetProgramLocaleString(context.program.Address, "TimeTable.UI.NoProgram", this.textNoProgram);
        this.textProgramNr = HG.WebApp.Locales.GetProgramLocaleString(context.program.Address, "TimeTable.UI.ProgramNr", this.textProgramNr);

        this.element.find('[data-ui-field=prev]').on('click', function(){
            var p = parseInt(_this.context.parameter.Value);
            if (p == 0)
                p = '';
            else
                p--;
            _this.context.parameter.Value = p;
            _this.render();
        });
        this.element.find('[data-ui-field=next]').on('click', function(){
            var p = parseInt(_this.context.parameter.Value);
            if (isNaN(p))
                p = 0;
            else if (p >= _this.maxPrograms-1)
                p = '';
            else
                p++;
            _this.context.parameter.Value = p;
            _this.render();
        });


        console.log('Initializing...');
        HG.WebApp.Control.GetWidget('homegenie/generic/timetable', function(w) {
            if (w != null) {
                _this.timetableWidget = w.Instance;
                _this.timetableWidget.TimebarHeight = 15;
                _this.timetableWidget.TimebarWidth = 270;
                //
                _this.timebar_s = _this.element.find('[data-ui-field=timebar_s]');
                _this.timebar_s.group = 0;
                _this.timebar_s.paper = Raphael(_this.timebar_s.get(0), _this.timetableWidget.TimebarWidth, _this.timetableWidget.TimebarHeight);
                _this.timebar_d = _this.element.find('[data-ui-field=timebar_d]');
                _this.timebar_d.group = 0;
                _this.timebar_d.paper = Raphael(_this.timebar_d.get(0), _this.timetableWidget.TimebarWidth, _this.timetableWidget.TimebarHeight);
                //
                _this.render();
            }
        });
    },

    render: function() {
        var _this = this;
        var p = parseInt(this.context.parameter.Value);
        if (isNaN(p)) {
            this.element.find('[data-ui-field=prev]').addClass('ui-disabled');
            this.element.find('[data-ui-field=program]').html(': '+this.textNoProgram);
        } else {
            this.element.find('[data-ui-field=prev]').removeClass('ui-disabled');
            this.element.find('[data-ui-field=program]').html(': '+this.textProgramNr+' '+(p+1));
        }
        $.mobile.loading('show', { text: 'Loading table', textVisible: true });
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + this.timetableWidget.ApiDomain + '/Timetable.Get/' + this.TableGroup + '.' + this.context.parameter.Value,
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                var table = eval(data)[0].ResponseValue;
                _this.timetableWidget.DrawTimetable(_this.timebar_s, false, table);
                $.mobile.loading('hide');
            }
        }); 
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + this.timetableWidget.ApiDomain + '/Timetable.Get/' + this.TableGroup + '.' + this.context.parameter.Value+'.DST',
            type: 'GET',
            dataType: 'text',
            success: function (data) {
                var table = eval(data)[0].ResponseValue;
                _this.timetableWidget.DrawTimetable(_this.timebar_d, false, table);
                $.mobile.loading('hide');
            }
        }); 
    }
}]