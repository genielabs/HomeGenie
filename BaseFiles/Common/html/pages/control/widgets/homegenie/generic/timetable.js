[{
  Name: "Timetable",
  Author: "Generoso Martello",
  Version: "2015-01-26",

  GroupName: '',
  IconImage: 'images/scheduler.png',
  StatusText: '',
  Description: '',
  ApiDomain: 'HomeAutomation.HomeGenie/Timetable',
  Widget: null,
  Module: null,
  ControlPopup: null,
  CalendarPopup: null,
  HelpPopup: null,
  Initialized: false,
  
  Timebar: null,
  TimebarElement: null,
  TimebarWidth: 530,
  TimebarHeight: 40,
  TimebarResolution: 5,
  
  CalendarData: '',
  CurrentTableId: 0,
  Groups: [ 
    { 
        'Table': 'Level', 
        'Actions': [
            { id: '0', localeId: 'timetable_levelmode_off', description: 'Off', color: '#222' },
            { id: '1', localeId: 'timetable_levelmode_10', description: '10%', color: '#663' },
            { id: '2', localeId: 'timetable_levelmode_20', description: '20%', color: '#773' },
            { id: '3', localeId: 'timetable_levelmode_30', description: '30%', color: '#884' },
            { id: '4', localeId: 'timetable_levelmode_40', description: '40%', color: '#994' },
            { id: '5', localeId: 'timetable_levelmode_50', description: '50%', color: '#AA4' },
            { id: '6', localeId: 'timetable_levelmode_60', description: '60%', color: '#BB5' },
            { id: '7', localeId: 'timetable_levelmode_70', description: '70%', color: '#CC5' },
            { id: '8', localeId: 'timetable_levelmode_80', description: '80%', color: '#DD6' },
            { id: '9', localeId: 'timetable_levelmode_90', description: '90%', color: '#EE7' },
            { id: 'A', localeId: 'timetable_levelmode_100', description: '100%', color: '#FF8' }
        ]
    },
    {
        'Table': 'Therm',
        'Actions': [ 
            { id: '0', localeId: 'timetable_thermmode_off', description: 'Off', color: '#222' },
            { id: '1', localeId: 'timetable_thermmode_heat', description: 'Heat', color: '#E33' },
            { id: '2', localeId: 'timetable_thermmode_cool', description: 'Cool', color: '#77F' },
            { id: '3', localeId: 'timetable_thermmode_auto', description: 'Auto', color: '#493' },
            { id: '4', localeId: 'timetable_thermmode_fanonly', description: 'Fan Only', color: '#8A8' },
            { id: '5', localeId: 'timetable_thermmode_autochangeover', description: 'Auto Changeover', color: '#A6F' },
            { id: '6', localeId: 'timetable_thermmode_heateco', description: 'Heat Economy', color: '#B55' },
            { id: '7', localeId: 'timetable_thermmode_cooleco', description: 'Cool Economy', color: '#66A' },
            { id: '8', localeId: 'timetable_thermmode_away', description: 'Away', color: '#CCC' }
        ]
    },
    {
        'Table': 'OnOff',
        'Actions': [
            { id: '0', localeId: 'timetable_onoffmode_off', description: 'Off', color: '#222' },
            { id: '1', localeId: 'timetable_onoffmode_on', description: 'On', color: '#5F5' }
        ]
    }
  ],
  
  CurrentSlice: null,

  RenderView: function (cuid, module) {
    var container = $(cuid);
    //
    var lastupdatetime = new Date();
    var statusInfo = 'Timetable Widget alpha (work in progress)';
    //
    if (!this.Initialized)
    {
      var _this = this;      
      this.Initialized = true;
      this.Module = module;
      this.Widget = container.find('[data-ui-field=widget]');
      this.ControlPopup = container.find('[data-ui-field=controlpopup]').popup();
      this.ControlPopup.trigger('create');
      this.HelpPopup = container.find('[data-ui-field=helppopup]').popup();
      this.HelpPopup.trigger('create');
      this.CalendarPopup = container.find('[data-ui-field=calendarpopup]').popup();
      this.CalendarPopup.trigger('create');
      // BEGIN jQuery UI Calendar initialization
      var year = (new Date).getFullYear();
      while(this.CalendarData.length < 366) this.CalendarData += '0';
      this.CalendarPopup.find('[data-ui-field=datepicker]').datepicker({
        minDate: new Date(year, 0, 1),
        maxDate: new Date(year, 11, 31),
        onSelect: function(dateText, inst) {
            var day = _this.GetDayIndex(inst.selectedDay, inst.selectedMonth);
            var dv = parseInt(_this.CalendarData[day]);
            if (dv >= 3) dv = 0; else dv++;
            _this.CalendarData = _this.CalendarData.substr(0, day) + dv.toString() + _this.CalendarData.substr(day + 1);
            _this.CalendarPopup.find('[data-ui-field=datepicker]').datepicker('refresh');
        },
        beforeShowDay: function(d) {
            var day = _this.GetDayIndex(d.getDate(), d.getMonth());
            var dayClass = _this.CalendarData.substr(day, 1);
            if ((d.getDay() == 6 || d.getDay() == 0) && dayClass == '0')
                dayClass = 'ui-datepicker-daywe'; // weekend
            else
                dayClass = 'ui-datepicker-day' + dayClass; 
            return [true, dayClass, 'Day nr.' + (day + 1)];
        }
      });
      var userLang = HG.WebApp.Locales.GetUserLanguage();
      $.ajax({
        url: 'js/i18n/jquery.ui.datepicker-' + userLang + '.js',
        type: "GET",
        dataType: "text",
        success: function (data) {
           eval(data);
           _this.CalendarPopup.find('[data-ui-field=datepicker]').datepicker('option', $.datepicker.regional[userLang]);
        }
      });
      // END jQuery UI Calendar initialization
      this.Widget.data('ControlPopUp', this.ControlPopup);
      //
      // ui events handlers
      //
      // popup values on open
      this.ControlPopup.on('popupbeforeposition', function(evt, ui){
        var description = _this.Widget.find('[data-role=navbar]').eq(0).find('[class*=ui-btn-active]').html();
        $(this).find('[data-ui-field=timetable_edit_group]').html(description);
        var title = HG.WebApp.Locales.GetWidgetLocaleString(_this.Widget, 'timetable_edit_table', 'Edit Timetable');
        _this.ControlPopup.find('[data-ui-field=timetable_edit_slot]').html(title + ' #' + (_this.TimebarElement.table + 1));
      });
      this.CalendarPopup.on('popupbeforeposition', function(evt, ui){
        $.mobile.loading('show', { text: 'Loading Calendar', textVisible: true });
        $.ajax({
          url: '/' + HG.WebApp.Data.ServiceKey + '/' + _this.ApiDomain + '/Calendar.Get/',
          type: "GET",
          dataType: "text",
          success: function (data) {
             _this.CalendarData = eval(data)[0].ResponseValue;
             _this.CalendarPopup.find('[data-ui-field=datepicker]').datepicker('refresh');
            $.mobile.loading('hide');
          }
        });
      });
      this.Widget.find('[data-ui-field=btn-table-select]').click(function(el){
        var id = parseInt($(this).html() - 1);
        _this.Widget.find('[data-ui-field=btn-table-select]').removeClass('ui-btn-active');
        _this.Widget.find('[data-ui-field=btn-table-select]').eq(id).addClass('ui-btn-active');
        _this.SetTableId(id);
      });
      this.Widget.find('[data-ui-field=btn-table-help]').click(function(el){
        _this.HelpPopup.popup('open');
      });
      this.Widget.find('[data-ui-field=btn-table-calendar]').click(function(el){
        _this.CalendarPopup.popup('open');
      });
      this.CalendarPopup.find('[data-ui-field=btn_calendar_save]').click(function(el){
        $.mobile.loading('show', { text: 'Saving Calendar', textVisible: true });
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + _this.ApiDomain + '/Calendar.Set',
            type: 'POST',
            data: _this.CalendarData,
            dataType: 'text',
            success: function (data) {
                _this.CalendarPopup.popup('close');
                $.mobile.loading('hide');
            }
        });    
      });
      this.ControlPopup.find('[data-ui-field=select_action]').change(function() {
        var color = $(this).find('option:selected').attr('data-option-color');
        _this.ControlPopup.find('[data-ui-field=current_action_color]').css('background', color);
      });
      this.ControlPopup.find('[data-ui-field=btn_timeslot_set]').click(function(el){
        var slotFrom = _this.ControlPopup.find('[data-ui-field=select_timestart]').val();
        var slotTo = _this.ControlPopup.find('[data-ui-field=select_timeend]').val();
        var slotValue = _this.ControlPopup.find('[data-ui-field=select_action]').val();
        var slotRangeStart = _this.CurrentSlice.data("index");
        var slotRangeEnd = slotRangeStart + _this.CurrentSlice.data("length");
        var request = _this.Groups[_this.TimebarElement.group].Table + '.' + _this.TimebarElement.table + '/' + slotFrom + '/' + slotTo + '/' + slotValue;
        request += '/' + slotRangeStart + '/' + slotRangeEnd;
        $.mobile.loading('show', { text: 'Saving table', textVisible: true });
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + _this.ApiDomain + '/Timetable.Set',
            type: 'POST',
            data: request,
            dataType: 'text',
            success: function (data) {
                _this.SetTable(_this.TimebarElement, _this.TimebarElement.table);
                _this.ControlPopup.popup('close');
            }
        });
      });
      //
      // timebar initialization
      //
      this.timebar_level_s = this.Widget.find('[data-ui-field=timebar_level_s]');
      this.timebar_level_s.paper = Raphael(this.timebar_level_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_level_s.group = 0;
      this.timebar_level_d = this.Widget.find('[data-ui-field=timebar_level_d]');
      this.timebar_level_d.paper = Raphael(this.timebar_level_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_level_d.group = 0;
      this.timebar_therm_s = this.Widget.find('[data-ui-field=timebar_therm_s]');
      this.timebar_therm_s.paper = Raphael(this.timebar_therm_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_therm_s.group = 1;
      this.timebar_therm_d = this.Widget.find('[data-ui-field=timebar_therm_d]');
      this.timebar_therm_d.paper = Raphael(this.timebar_therm_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_therm_d.group = 1;
      this.timebar_onoff_s = this.Widget.find('[data-ui-field=timebar_onoff_s]');
      this.timebar_onoff_s.paper = Raphael(this.timebar_onoff_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_onoff_s.group = 2;
      this.timebar_onoff_d = this.Widget.find('[data-ui-field=timebar_onoff_d]');
      this.timebar_onoff_d.paper = Raphael(this.timebar_onoff_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_onoff_d.group = 2;
      this.SetTableId(0);
    }
    //
    if (lastupdatetime > 0)
    {
      this.UpdateTime = HG.WebApp.Utility.FormatDate(lastupdatetime) + ' ' + HG.WebApp.Utility.FormatDateTime(lastupdatetime);
    }
    this.Widget.find('[data-ui-field=status]').html('<span style="vertical-align:middle">' + statusInfo + '</span>');
  },
  
  GetDayIndex: function(day, month) {
    // ignore year in date, consider always as current year
    var year = (new Date).getFullYear();
    var d = new Date(year, month, day);
    var firstDay = new Date(year, 0, 1);
    var dayIndex = Math.ceil((d - firstDay) / 86400000);
    if (!this.IsLeapYear() && month > 1) dayIndex++;
    return dayIndex;
  },
  
  IsLeapYear: function() {
    // returns true if current year is a leap year
    var year = (new Date).getFullYear();
    return ((year % 4 == 0) && (year % 100 != 0)) || (year % 400 == 0);
  },

  SetTableId: function(id) {
      this.SetTable(this.timebar_level_s, id);
      this.SetTable(this.timebar_level_d, id+'.DST');
      this.SetTable(this.timebar_therm_s, id);
      this.SetTable(this.timebar_therm_d, id+'.DST');
      this.SetTable(this.timebar_onoff_s, id);
      this.SetTable(this.timebar_onoff_d, id+'.DST');
  },

  SetTable: function(el, id) {
    el.table = id;
    var _this = this;
    var table = '';
    $.mobile.loading('show', { text: 'Loading table', textVisible: true });
    $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + _this.ApiDomain + '/Timetable.Get/' + this.Groups[el.group].Table + '.' + el.table,
        type: 'GET',
        dataType: 'text',
        success: function (data) {
            table = eval(data)[0].ResponseValue;
            _this.DrawTimetable(el, true, table);
            $.mobile.loading('hide');
        }
    });    
  },
  
  SetTableGroup: function(id) {
    var actions = this.ControlPopup.find('[data-ui-field=select_action]');
    actions.empty();
    for (var i = 0; i < this.Groups[id].Actions.length; i++)
    {
        var title = HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, this.Groups[id].Actions[i].localeId, this.Groups[id].Actions[i].description);
        actions.append('<option value="' + this.Groups[id].Actions[i].id + '" data-option-color="' + this.Groups[id].Actions[i].color + '" style="padding-left:5px;border-left:solid 30px ' + this.Groups[id].Actions[i].color + '">' + title + '</option>');
    }
    actions.selectmenu('refresh', true);
  },
  
  GetTableAction: function(gid, id) {
    var actions = this.Groups[gid].Actions;
    var foundAction = null;
    for (var a = 0; a < actions.length; a++)
    {
        if (actions[a].id == id) {
            foundAction = actions[a];
            break;
        }
    }
    return foundAction;
  },
  
  GetSliceInfo: function(rect) {
    var id = rect.data('action');
    var start = rect.data('index');
    var end = start + rect.data('length');
    var action = this.GetTableAction(rect.data('group'), id);
    var info = '';
    if (action != null)
    {
        info = '<strong>' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, action.localeId, action.description) + '</strong>';
        info += ' ' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_hour_from', 'from');
        info += ' <strong>' + this.GetSliceTime(start) + '</strong>';
        info += ' ' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_hour_to', 'to');
        info += ' <strong>' + this.GetSliceTime(end) + '</strong>';
    }
    return info;
  },
  
  GetSliceTime: function(sliceIndex) {
      var sliceSize = (60 / this.TimebarResolution);
      var hour = Math.floor(sliceIndex / sliceSize);
      var minute = Math.round(((sliceIndex / sliceSize) - hour) * 60);
      if (hour.toString().length == 1) hour = '0' + hour;
      if (minute.toString().length == 1) minute = '0' + minute;
      return hour + ':' + minute;
  },
  
  EditTimetable: function(el, start, length, action) {
    this.SetTableGroup(el.group);
    this.TimebarElement = el;
    var selectStart = this.ControlPopup.find('[data-ui-field=select_timestart]');
    var selectEnd = this.ControlPopup.find('[data-ui-field=select_timeend]');
    selectStart.empty();
    selectEnd.empty();
    var tableLength = 24 * 60 / this.TimebarResolution;
    for (var i = 0; i <= tableLength; i++)
    {
      var displayTime = this.GetSliceTime(i);
      selectStart.append('<option value="' + i + '"' + (i == start ? ' selected' : '') + '>' + displayTime + '</option>');
      selectEnd.append('<option value="' + i + '"' + (i == (start + length) ? ' selected' : '') + '>' + displayTime + '</option>');
    }
    selectStart.selectmenu('refresh', true);
    selectEnd.selectmenu('refresh', true);
    this.ControlPopup.find('[data-ui-field=select_action]').val(this.GetTableAction(el.group, action).id).selectmenu('refresh').change();
    this.ControlPopup.popup('open');
  },
  
  DrawTimetable: function(el, drawHeader, timetable) {
    var _this = this;
    var paper = el.paper;
    console.log(el);
    console.log(el.paper);
    console.log(timetable);
    paper.clear();
    paper.rect(0, 0, this.TimebarWidth, this.TimebarHeight, 0).attr({fill: "#000", stroke: "none"});
    console.log('paper cleared');
    var tableLength = 24 * 60 / this.TimebarResolution;
    var sliceFactor = (60 / this.TimebarResolution);
    while (timetable.length < tableLength) timetable += ' ';
    var nb = 0, x = 0, y = 0, startIndex = 0;
    var stepSize = (_this.TimebarWidth / timetable.length);
    var startY = drawHeader ? 14 : 0;
    for (var i = 0, c = timetable.length; i < tableLength; i++)
    {
      nb++;
      if ((i == (tableLength - 1)) || (timetable[i + 1] != timetable[i])) 
      {
    console.log('paper drawing rect '+i);
        var rect = paper.rect(x, y + startY, (nb * stepSize) - 1, _this.TimebarHeight - (startY+1)).attr({ 'fill': '90-#455-' + _this.GetTableAction(el.group, timetable[i]).color, 'stroke': '#fff', 'stroke-width': '0.0' })
        .mouseover(function(e){
          this.attr({ 'stroke': '#5f5', 'stroke-width': '2', 'stroke-opacity' : 0.7 });
          _this.CurrentSlice = this;
        })
        .mouseout(function(e){
          this.attr({ 'stroke': '#fff', 'stroke-width': '0.0' });
        })
        .click(function(e){
          _this.EditTimetable(el, this.data('index'), this.data('length'), this.data('action'));
        });
        rect.data('group', el.group);
        rect.data('index', startIndex);
        rect.data('length', nb);
        rect.data('action', timetable[i]);
        //
        var centerX = (nb * stepSize) / 2;
        $(rect.node).qtip({ 
            content: _this.GetSliceInfo(rect),
            show: { delay: 350 },
            style: { classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap' },
            position: { my: 'bottom center', at: 'top right', adjust: { x: centerX, y: -10 } }
        });
        //
        x += nb * stepSize;
        nb = 0;
        startIndex = i + 1;
      }
      if (drawHeader && i % sliceFactor == 0)
      {
        paper.text(((i / sliceFactor) * (sliceFactor * stepSize)) + 10, 7, (i / sliceFactor).toString()).attr({ fill: '#fff' });
        paper.rect(((i / sliceFactor) * (sliceFactor * stepSize)) + (stepSize * sliceFactor), 0, 2, 16, 0).attr({ fill: '#99f' });
      }
    }    
    
  }

}]