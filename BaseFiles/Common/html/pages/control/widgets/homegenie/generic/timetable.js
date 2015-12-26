[{
  Name: "Timetable",
  Author: "Generoso Martello",
  Version: "2015-12-21",

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
  TimebarHeight: 50,
  TimebarResolution: 5,

  CalendarData: '',
  CurrentProgram: 0,
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
  _refreshTimeout: null,

  RenderView: function (cuid, module) {
    var container = $(cuid);
    var _this = this;      
    if (!this.Initialized)
    {
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
        // TODO: resize width to best fit content
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
      this.Widget.find('[data-ui-field=btn-table-help]').click(function(el){
        _this.HelpPopup.popup('open');
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

      // BEGIN Programs page initialization
      var programsPageContainer = container.parent().parent().parent(); // $(document.body); //
      this.ProgramsPage = container.find('[data-ui-field=timetable-programs-page]').remove();
      programsPageContainer.find('[data-ui-field=timetable-programs-page]').remove();
      programsPageContainer.append(this.ProgramsPage.get(0));
      programsPageContainer.trigger('create');
      // program change button
      this.ProgramsPage.find('[data-ui-field=btn-table-select]').click(function(el){
        var id = parseInt($(this).html() - 1);
        _this.CurrentProgram = id;
        _this.ProgramsPage.find('[data-ui-field=btn-table-select]').removeClass('ui-btn-active');
        _this.ProgramsPage.find('[data-ui-field=btn-table-select]').eq(id).addClass('ui-btn-active');
        _this.SetTableId(id);
      });
      // timebar initialization
      this.timebar_level_modules = this.ProgramsPage.find('[data-ui-field=timebar_level_modules]');
      this.timebar_level_s = this.ProgramsPage.find('[data-ui-field=timebar_level_s]');
      this.timebar_level_s.paper = Raphael(this.timebar_level_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_level_s.group = 0;
      this.timebar_level_d = this.ProgramsPage.find('[data-ui-field=timebar_level_d]');
      this.timebar_level_d.paper = Raphael(this.timebar_level_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_level_d.group = 0;
      this.timebar_therm_modules = this.ProgramsPage.find('[data-ui-field=timebar_therm_modules]');
      this.timebar_therm_s = this.ProgramsPage.find('[data-ui-field=timebar_therm_s]');
      this.timebar_therm_s.paper = Raphael(this.timebar_therm_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_therm_s.group = 1;
      this.timebar_therm_d = this.ProgramsPage.find('[data-ui-field=timebar_therm_d]');
      this.timebar_therm_d.paper = Raphael(this.timebar_therm_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_therm_d.group = 1;
      this.timebar_onoff_modules = this.ProgramsPage.find('[data-ui-field=timebar_onoff_modules]');
      this.timebar_onoff_s = this.ProgramsPage.find('[data-ui-field=timebar_onoff_s]');
      this.timebar_onoff_s.paper = Raphael(this.timebar_onoff_s.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_onoff_s.group = 2;
      this.timebar_onoff_d = this.ProgramsPage.find('[data-ui-field=timebar_onoff_d]');
      this.timebar_onoff_d.paper = Raphael(this.timebar_onoff_d.get(0), this.TimebarWidth, this.TimebarHeight);
      this.timebar_onoff_d.group = 2;
      // open/close timetable programs settings page
      this.Widget.find('[data-ui-field=btn-programs-open]').click(function(el){
        $('[data-ui-field=wallpaper]').addClass("blur-filter");
        $('[data-ui-field=homegenie_panel_button]').addClass('ui-disabled');
        $(document.body).css('overflow', 'hidden');
        _this.ProgramsPage.slideDown(500);
        setTimeout(function(){
          _this.ProgramsPage.scrollTop(0);
          _this.SetTableId(_this.CurrentProgram);
        }, 550);
      });
      this.ProgramsPage.find('[data-ui-field=btn-programs-close]').click(function(el){
        var btn = $(this);
        setTimeout(function(){
          $(btn).removeClass('ui-btn-active');
        }, 500);
        $(document.body).css('overflow', 'auto');
        $('[data-ui-field=homegenie_panel_button]').removeClass('ui-disabled');
        $('[data-ui-field=wallpaper]').removeClass("blur-filter");
        _this.ProgramsPage.slideUp(500);
        _this.RefreshSchedulingLog();
        HG.WebApp.Control.UpdateModules();
      });
      this.ProgramsPage.find('[data-ui-field=btn-table-calendar]').click(function(el){
        var btn = $(this);
        setTimeout(function(){
          $(btn).removeClass('ui-btn-active');
        }, 500);
        _this.CalendarPopup.popup('open');
      });
      // END

    }

    // Refresh current scheduling infos
    this.RefreshSchedulingLog();
  },

  RefreshSchedulingLog: function() {
    var _this = this;
    if (this._refreshTimeout != null)
      clearTimeout(this._refreshTimeout);
    this._refreshTimeout = setTimeout(function(){
      _this._refreshTimeout = null;
      var schedLog = _this.Widget.find('[data-ui-field=scheduling]');
      schedLog.empty();
      $.ajax({
        url: '/' + HG.WebApp.Data.ServiceKey + '/' + _this.ApiDomain + '/Timetable.GetScheduling/',
        type: 'GET',
        success: function (schedulingModules) {
          if (schedulingModules.length > 0) {
            $.each(schedulingModules, function(i, m){
              if (typeof m.Timetable == 'undefined') return true;
              var item = $('<div/>');
              var itemTitle = $('<div style="margin:0;font-size:11pt;font-weight:bold;margin-top:8px;max-width:270px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;" />');
              var itemImage = $('<img src="pages/control/widgets/homegenie/generic/images/unknown.png" width="32" height="32" align="absmiddle" style="margin-right:4px">');
              itemTitle.append(itemImage);
              itemTitle.append(HG.Ui.GetModuleDisplayName(m));
              var itemInfo = $('<div style="margin-left:10px">'+_this.GetTableInfo(m.Type, m.Timetable)+'</div>');
              item.append(itemTitle);
              item.append(itemInfo);
              schedLog.append(item);
              var modObj = HG.WebApp.Utility.GetModuleByDomainAddress(m.Domain, m.Address);
              HG.Ui.GetModuleIcon(modObj, function(icon, elid) {
                $(itemImage).attr('src', icon);
              }, itemImage);
            });
          } else {
            schedLog.append('<div align="center" style="line-height: 200px">No schedule programmed</div>');
          }
        }
      });
    }, 2000);
  },

  GetModules: function() {
    var _this = this;
    $.mobile.loading('show', { text: 'Updating data', textVisible: true });
    $.ajax({
      url: '/' + HG.WebApp.Data.ServiceKey + '/' + this.ApiDomain + '/Timetable.GetModules/',
      type: 'GET',
      success: function (mods) {
        //table = eval(data)[0].ResponseValue;
        //console.log(mods);
        _this.timebar_level_modules.empty();
        _this.timebar_therm_modules.empty();
        _this.timebar_onoff_modules.empty();
        $.each(mods, function(i, m){
          var item = $('<div style="display:table-row" />');
          item.append('<div style="width:270px;padding-left:4px;-display:table-cell;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;" align="left">'+HG.Ui.GetModuleDisplayName(m)+'</div>');
          var flags = $('<div style="width:250px;display:table-cell;overflow:hidden" align="right" class="ui-mini" />');
          item.append(flags);
          var flagEnabled = $('<a href="#" title="Enable Timetable scheduling for this module" class="ui-btn ui-btn-inline ui-btn-icon-left ui-icon-check timetable-typeflag'+(m.Enabled?' ui-btn-e ui-btn-active':'')+'">&nbsp;</a>');
          var flagCheckDST = $('<a href="#" title="Use alternate schedule on Daylight Saving Time" class="ui-btn ui-btn-inline ui-btn-icon-left ui-icon-clock timetable-typeflag'+(m.CheckDST?' ui-btn-e ui-btn-active':'')+'">&nbsp;</a>');
          var flagRepeat = $('<a href="#" title="Keep set module state for the whole schedule duration (5 minutes interval check)" class="ui-btn ui-btn-inline ui-btn-icon-left ui-icon-refresh timetable-typeflag'+(m.Repeat?' ui-btn-e ui-btn-active':'')+'">&nbsp;</a>');
          var flagWeekday = $('<a href="#" title="Use this schedule on Weekdays" class="ui-btn ui-btn-inline timetable-typeflag'+(m.Workday===_this.CurrentProgram+1?' ui-btn-active ui-datepicker-day1':'')+'">W</a>');
          var flagWeekend = $('<a href="#" title="Use this schedule on Weekend" class="ui-btn ui-btn-inline timetable-typeflag'+(m.Weekend===_this.CurrentProgram+1?' ui-btn-active ui-datepicker-daywe':'')+'">E</a>');
          var flagHoliday = $('<a href="#" title="Use this schedule on Holidays" class="ui-btn ui-btn-inline timetable-typeflag'+(m.Holiday===_this.CurrentProgram+1?' ui-btn-active ui-datepicker-day2':'')+'">H</a>');
          var flagSpecial = $('<a href="#" title="Use this schedule on Special days" class="ui-btn ui-btn-inline timetable-typeflag'+(m.Special===_this.CurrentProgram+1?' ui-btn-active ui-datepicker-day3':'')+'">S</a>');
          flags.append(flagEnabled);
          flags.append(flagCheckDST);
          flags.append(flagRepeat);
          flags.append(flagWeekday);
          flags.append(flagWeekend);
          flags.append(flagHoliday);
          flags.append(flagSpecial);
          flagEnabled.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Enable', $(this).hasClass('ui-btn-active')?'':'On', function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagCheckDST.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.CheckDST', $(this).hasClass('ui-btn-active')?'':'On', function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagRepeat.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Repeat', $(this).hasClass('ui-btn-active')?'':'On', function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagWeekday.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Workday', $(this).hasClass('ui-btn-active')?'':_this.CurrentProgram+1, function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagWeekend.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Weekend', $(this).hasClass('ui-btn-active')?'':_this.CurrentProgram+1, function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagHoliday.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Holiday', $(this).hasClass('ui-btn-active')?'':_this.CurrentProgram+1, function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          flagSpecial.on('click', function() {
            $.mobile.loading('show');
            HG.Configure.Modules.ParameterSet(m.Domain, m.Address, 'TimeTable.Special', $(this).hasClass('ui-btn-active')?'':_this.CurrentProgram+1, function(){
              _this.GetModules();
              _this.CheckNow();
            });
          });
          switch (m.Type) {
            case 'Level':
              _this.timebar_level_modules.append(item);
              break;
            case 'Therm':
              _this.timebar_therm_modules.append(item);
              break;
            case 'OnOff':
              _this.timebar_onoff_modules.append(item);
              break;
          }
        });
        $.mobile.loading('hide');
      }
    });      
  },

  CheckNow: function() {
    $.get('/' + HG.WebApp.Data.ServiceKey + '/' + this.ApiDomain + '/Timetable.CheckNow/', function (data) { });
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
    // refresh modules list
    this.GetModules();
  },

  SetTable: function(el, id) {
    el.table = id;
    el.paper.clear();
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
    // change popup title and description
    var description = '';
    switch(id) {
      case 0:
        description = HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_group_shutters', 'Shutters and Dimmers');
        break;
      case 1:
        description = HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_group_thermostats', 'Thermostats');
        break;
      case 2:
        description = HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_group_lights', 'Lights and Switches');
        break;
    }
    this.ControlPopup.find('[data-ui-field=timetable_edit_group]').html(description);
    var title = HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_program', 'Program');
    this.ControlPopup.find('[data-ui-field=timetable_edit_slot]').html(title + ' #' + (this.CurrentProgram + 1));
    // Select menu options
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

  GetSliceInfo: function(actionId, tableGroup, start, length, drawDecor) {
    var action = this.GetTableAction(tableGroup, actionId);
    var end = start + length;
    var info = '';
    if (action != null)
    {
      var startDate = this.GetSliceTime(start);
      var endDate = this.GetSliceTime(end);
      var dt = new Date();
      var inRange = (dt >= startDate && dt <= endDate);
      if (drawDecor) {
          if (inRange) 
            info += '<span style="border-left:3px solid lime;padding-left:4px;">';
          else
            info += '<span style="border-left:3px dotted gray;padding-left:4px;">';
      }
      info += '<strong>' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, action.localeId, action.description) + '</strong>';
      info += ' ' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_hour_from', 'from');
      info += ' <strong>' + HG.WebApp.Utility.FormatDateTime(startDate) + '</strong>';
      info += ' ' + HG.WebApp.Locales.GetWidgetLocaleString(this.Widget, 'timetable_hour_to', 'to');
      info += ' <strong>' + HG.WebApp.Utility.FormatDateTime(endDate) + '</strong>';
      if (drawDecor)
          info += '</span>';
    }
    return info;
  },

  GetSliceTime: function(sliceIndex) {
    var sliceSize = (60 / this.TimebarResolution);
    var hour = Math.floor(sliceIndex / sliceSize);
    var minute = Math.round(((sliceIndex / sliceSize) - hour) * 60);
    var d = new Date();
    d.setHours(hour);
    d.setMinutes(minute);
    return d;
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
      var displayTime = HG.WebApp.Utility.FormatDateTime(this.GetSliceTime(i));
      selectStart.append('<option value="' + i + '"' + (i == start ? ' selected' : '') + '>' + displayTime + '</option>');
      selectEnd.append('<option value="' + i + '"' + (i == (start + length) ? ' selected' : '') + '>' + displayTime + '</option>');
    }
    selectStart.selectmenu('refresh', true);
    selectEnd.selectmenu('refresh', true);
    this.ControlPopup.find('[data-ui-field=select_action]').val(this.GetTableAction(el.group, action).id).selectmenu('refresh').change();
    this.ControlPopup.popup('open');
  },

  GetTableInfo: function(tableType, timetable) {
    var tableInfo = '';
    var tableLength = 24 * 60 / this.TimebarResolution;
    var nb = 0, startIndex = 0;
    for (var i = 0, c = timetable.length; i < tableLength; i++)
    {
      nb++;
      if ((i == (tableLength - 1)) || (timetable[i + 1] != timetable[i])) 
      {
        var g = 0; if (tableType == 'Therm') g = 1; else if (tableType == 'OnOff') g = 2;
        var slice = { group: g, action: timetable[i], index: startIndex, length: nb };
        tableInfo += this.GetSliceInfo(slice.action, slice.group, slice.index, slice.length, true)+'<br/>';
        nb = 0;
        startIndex = i + 1;
      }
    }
    return tableInfo;
  },

  DrawTimetable: function(el, drawHeader, timetable) {
    var _this = this;
    var paper = el.paper;
    paper.clear();
    paper.rect(0, 0, this.TimebarWidth, this.TimebarHeight, 0).attr({fill: "#000", stroke: "none"});
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
          content: _this.GetSliceInfo(rect.data('action'), rect.data('group'), rect.data('index'), rect.data('length')),
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
        paper.text(((i / sliceFactor) * (sliceFactor * stepSize)) + 10, 8, (i / sliceFactor).toString()).attr({ fill: '#fff' });
        paper.rect(((i / sliceFactor) * (sliceFactor * stepSize)) + (stepSize * sliceFactor), 0, 2, 16, 0).attr({ fill: '#99f' });
      }
    }    

  }

}]