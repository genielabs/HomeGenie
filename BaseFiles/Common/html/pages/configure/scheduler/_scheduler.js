HG.WebApp.Scheduler = HG.WebApp.Scheduler || new function () { var $$ = this;

    $$.PageId = 'page_configure_schedulerservice';
    $$._ScheduleList = {};
    $$._CurrentEventName = "";
    $$._CurrentEventIndex = -1;
    $$._CurrentDate = new Date();
    $$._UpdateTimeout = null;

    $$.InitializePage = function () {
        var page = $('#' + $$.PageId);
        page.on('pagebeforeshow', function (e) {
            $$._CurrentDate = new Date(); // set today as initial date
            $$.editorContainer.hide();
            $$.calendarContainer.hide();
            setTimeout(function(){
                $$.LoadScheduling($$.showCalendar);
            }, 500);
        });

        $$.datePicker = page.find('[data-ui-field="calendar-date"]').datebox({
            mode: "calbox",
            useLang: HG.WebApp.Utility.GetDateBoxLocale(),
            calShowWeek: true,
            //useInline: true,
            //hideContainer: true,
            hideInput: true,
            //useFocus: false,
            useButton: false,
            useClearButton: false,
            useSetButton: true,
            useHeader: true,
            overrideCalHeaderFormat: '%B %Y',
            overrideDateFormat: '%Y-%m-%d',
            defaultValue: '12/02/73'
        }).on('change', function(){
            $$._CurrentDate = $$.datePicker.datebox('getTheDate');
            $$.RefreshOccursTable();
        });

        $$.displayDate = page.find('[data-ui-field="display-date"]');
        page.find('[data-ui-field="calendar-title"]').on('click', function(){
            $$.datePicker.datebox('open');
        });

        $$.editorContainer = page.find('[data-ui-field="editor-container"]');
        $$.calendarContainer = page.find('[data-ui-field="calendar-container"]');
        $$.editorButton = page.find('[data-ui-field="edit-button"]').on('click', function(){
            $('#schedulerservice_actionmenu').popup('close');
            $$.showEditor();
        });
        $$.calendarButton = page.find('[data-ui-field="calendar-button"]').on('click', function(){
            $('#schedulerservice_actionmenu').popup('close');
            $$.showCalendar();
        });
        page.find('[data-ui-field="add-button"]').on('click', function(){
            $('#schedulerservice_actionmenu').popup('close');
            $$._CurrentEventName = "";
            $$._CurrentEventIndex = -1;
            setTimeout($$.EditCurrentItem, 500);
        });
    };

    $$.showCalendar = function() {
        $$.calendarButton.hide();
        $$.editorButton.show();
        $$.editorContainer.fadeOut(200,function(){
            $$.calendarContainer.fadeIn(200);
        });
        setTimeout($$.RefreshOccursTable, 500);
    };

    $$.showEditor = function() {
        $$.editorButton.hide();
        $$.calendarButton.show();
        $$.calendarContainer.fadeOut(200,function(){
            $$.editorContainer.fadeIn(200);
        });
        setTimeout($$.LoadScheduling, 500);
    };

    $$.GetItemMarkup = function (schedule) {
        var displayName = schedule.Name;
        if (displayName.indexOf('.') > 0)
            displayName = displayName.substring(displayName.indexOf('.') + 1);
        var item = '<li  data-icon="false" data-schedule-name="' + schedule.Name + '" data-schedule-index="' + i + '">';
        item += '<a href="#" data-ui-ref="edit-btn">';
        //item += '   <p class="ui-li-aside ui-li-desc"><strong>&nbsp;' + schedule.ProgramId + '</strong><br><font style="opacity:0.5">' + 'Last: ' + schedule.LastOccurrence + '<br>' + 'Next: ' + schedule.NextOccurrence + '</font></p>';
        item += '   <h3 class="ui-li-heading">' + displayName + '</h3>';
        item += '   <p class="ui-li-desc">' + (schedule.Description != null ? schedule.Description : '') + ' <span style="opacity:0.5">(' + schedule.CronExpression + ')</span></p>';
        item += '</a>';
        item += '<div class="ui-grid-a" style="position:absolute;right:0;top:0;height:100%;">';
        item += '<div class="ui-block-a"><a data-ui-field="btn_delete" title="Delete" class="ui-btn ui-icon-delete ui-btn-icon-notext ui-list-btn-option"></a></div>';
        item += '<div class="ui-block-b"><a data-ui-field="btn_toggle" title="Disable" class="ui-btn ui-icon-' + (schedule.IsEnabled ? 'check' : 'alert') + ' ui-btn-icon-notext ui-list-btn-option">' + (schedule.IsEnabled ? 'Tap to DISABLE item' : 'Tap to ENABLE item') + '</a></div>';
        item += '</div>';
        item += '</li>';
        return item;
    }

    $$.RefreshOccursTable = function() {
        var page = $('#' + $$.PageId);
        var occursList = page.find('[data-ui-field="occurs-table"]');
        var dd = moment($$._CurrentDate).format('LL');
        $$.displayDate.html(dd);
        $.mobile.loading('show');
        var startDate = new Date($$._CurrentDate.getTime());
        startDate.setHours(0,0,0,0);
        HG.Control.Modules.ApiCall('HomeAutomation.HomeGenie', 'Automation', 'Scheduling.ListOccurrences', (24*1).toString()+'/'+startDate.getTime(), function(schedules){
            occursList.empty();
            var d = new Date(); d.setSeconds(0);
            var occurrences = [];
            $.each(schedules, function(k,v){
              var entry = { name: v.Name, occurs: [] };
              $.each($$._ScheduleList,function(sk,sv){
                if (sv.Name == v.Name) {
                    entry.index = sk;
                    entry.description = sv.Description;
                    entry.boundModules = sv.BoundModules.length;
                    entry.hasScript = (typeof sv.Script != 'undefined' && sv.Script != null && sv.Script.trim() != '');
                    entry.prevOccurrence = 0;
                    entry.nextOccurrence = 0;
                }
              });
              var prev = 0, start = 0;
              $.each(v.Occurrences, function(kk,vv){
                if (prev == 0) prev = start = end = vv;
                if (vv - prev > 60000) {
                  entry.occurs.push({ from: start, to: end });
                  prev = start = vv;
                } else {
                    prev = vv;
                }
                end = vv;
                if (entry.prevOccurrence < vv && vv <= d.getTime())
                    entry.prevOccurrence = vv;
                if (entry.nextOccurrence == 0 && vv > d.getTime())
                    entry.nextOccurrence = vv;
              });
              entry.occurs.push({ from: start, to: end });
              occurrences.push(entry);
            });
            var w = $(window).width()-32-50, h = 10;
            $.each(occurrences, function(k,v){
                var timeBarDiv = $('<div/>');
                timeBarDiv.css('cursor', 'pointer');
                timeBarDiv.on('click',function(){
                    $$._CurrentEventName = v.name;
                    $$._CurrentEventIndex = v.index;
                    $$.EditCurrentItem();
                });
                var indicators = '';
                if (v.boundModules > 0)
                    indicators += '<i title="bound modules" class="fa fa-check-square"></i> '+v.boundModules;
                if (v.hasScript > 0)
                    indicators += '&nbsp;&nbsp;&nbsp;<i title="runs script" class="fa fa-code"></i>';
                var timeBar = Raphael(timeBarDiv[0], w, h*2.5);
                timeBarDiv.append('<i class="fa fa-clock-o" aria-hidden="true" style="font-size:20pt;margin-left:8px;opacity:0.65;vertical-align:top;"></i>');
                timeBar.rect(0, 0, w, h*2.5).attr({
                    fill: "rgb(90, 90, 90)", 
                    stroke: "rgb(0,0,0)",
                    "stroke-width": 1
                });
                occursList.append('<div class="ui-grid-a"><div class="ui-block-a"><h2 style="text-align:left;margin:0;margin-top:0.5em">'+v.name+'</h2></div><div class="ui-block-b" align="right" style="padding-right:48px;padding-top:20px">'+indicators+'</div></div>');
                occursList.append(timeBarDiv);

                $.each(v.occurs, function(kk,vv){
                    var df = sd = new Date(vv.from);
                    sd.setHours(0,0,0,0);
                    vv.from -= sd.getTime();
                    vv.to -= sd.getTime();
                    var sx1 = Math.round(vv.from/(1440*60000)*w), sx2 = Math.round(vv.to/(1440*60000)*w)-sx1;
                    timeBar.rect(sx1, 0, sx2+4, h+1).attr({
                        fill: "rgba(255, 255, 70, 75)", 
                        stroke: "rgba(255,255,255, 70)",
                        "stroke-width": 1
                    });
                });    


                for (var t = 0; t < 24; t++) {
                    timeBar.text(t*(w/24)+(w/48), 18, t.toString()).attr({fill:'white'});
                    timeBar.rect(t*(w/24), (h*1.25-1), (w/24), (h*1.25)+1).attr({
                        stroke: "rgba(255, 255, 255, 0.2)", 
                        "stroke-width": 1
                    });
                }

                // build basic tooltip data
                var desc = '';
                if (typeof v.description != 'undefined' && v.description != null && v.description.trim() != '')
                    desc += v.description;
                desc += '<br/>';

                d = new Date(); d.setSeconds(0);
                var isToday = d.toDateString() == $$._CurrentDate.toDateString();
                if (isToday) {
                    d = d.getTime();
                    var d2 = new Date(); d2.setHours(0,0,0,0);
                    d -= d2.getTime();
                    var bw = Math.round(d/(1440*60000)*w);
                    timeBar.rect(bw+2, 1, w-bw-2, (h*2.5)-2).attr({
                        fill: "rgba(0, 0, 0, 0.35)", 
                        stroke: "rgba(0,0,0, 70)",
                        "stroke-width": 0.5
                    });
                    timeBar.rect(bw-1, 0, 2, h*2.5).attr({
                        fill: "rgba(80, 255, 80, 0.8)", 
                        stroke: "rgba(0,0,0, 70)",
                        "stroke-width": 0.5
                    });
                    // tooltip: previous/next occurrence text
                    if (v.prevOccurrence > 0 || v.nextOccurrence > 0) {
                        if (v.prevOccurrence > 0) {
                            desc += '<br/><strong>Today last</strong><br/>&nbsp;&nbsp;&nbsp;';
                            desc += moment(v.prevOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.prevOccurrence).from(new Date())+')';
                        }
                        if (v.nextOccurrence > 0) {
                            desc += '<br/><strong>Today next</strong><br/>&nbsp;&nbsp;&nbsp;';
                            desc += moment(v.nextOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.nextOccurrence).from(new Date())+')';
                        }
                    }
                } else {
                    var d2 = new Date(); d2.setHours(0,0,0,0);
                    if ($$._CurrentDate.getTime() > d2.getTime()) {
                        timeBar.rect(0, 1, w, (h*2.5)-2).attr({
                            fill: "rgba(0, 0, 0, 0.35)", 
                            stroke: "rgba(0,0,0, 70)",
                            "stroke-width": 0.5
                        });
                    }
                }

                // attach tooltip
                timeBarDiv.qtip({
                  content: {
                    text: desc,
                    title: {
                        text: '<strong>'+v.name+'</strong>',            
                        //button: 'close'
                    }
                  },
                  show: { delay: 350, solo: true, effect: function(offset) {
                    $(this).slideDown(100);
                  } },
                  hide: { inactive: 10000 },
                  style: { 
                    classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap',
                    width: 400, 
                    name: 'dark',
                    padding: 0
                  },
                  position: { my: 'bottom center', at: 'top center' }
                });

                timeBar.rect(0, 0, w, h*2.5).attr({
                    stroke: "rgba(255, 255, 255, 0.75)", 
                    "stroke-width": 1
                });
            });
            $.mobile.loading('hide');
            // auto-refresh every minute
            if ($$._UpdateTimeout != null)
                clearTimeout($$._UpdateTimeout);
            if ($.mobile.activePage.attr("id") == $$.PageId)
                $$._UpdateTimeout = setTimeout($$.RefreshOccursTable, 60000);
        });

    }

    $$.LoadScheduling = function (callback) {
        $.mobile.loading('show');
        HG.Automation.Scheduling.List(function (data) {
            $$._ScheduleList = data;
            $.mobile.loading('hide');
            //
            $('#configure_schedulerservice_list').empty();
            $('#configure_schedulerservice_list').append('<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_scheduler_events') + '</li>');
            //
            // element containing '.' in the name are grouped in own sections
            for (i = 0; i < $$._ScheduleList.length; i++) {
                var schedule = $$._ScheduleList[i];
                if (schedule.Name.indexOf('.') < 0) {
                    var item = $$.GetItemMarkup(schedule);
                    $('#configure_schedulerservice_list').append(item);
                }
            }
            var currentGroup = '';
            for (i = 0; i < $$._ScheduleList.length; i++) {
                var schedule = $$._ScheduleList[i];
                if (schedule.Name.indexOf('.') > 0) {
                    var scheduleGroup = schedule.Name.substring(0, schedule.Name.indexOf('.'));
                    var item = $$.GetItemMarkup(schedule);
                    if (scheduleGroup != currentGroup) {
                        $('#configure_schedulerservice_list').append('<li data-role="list-divider">' + scheduleGroup + '</li>');
                        currentGroup = scheduleGroup;
                    }
                    $('#configure_schedulerservice_list').append(item);
                }
            }
            $('#configure_schedulerservice_list').listview();
            // set on click handler for list items
            $('#configure_schedulerservice_list').find("li").each(function (index) {
                var item = $(this);
                item.find('[data-ui-field=btn_delete]').on('click', function () {
                    var that = $(this);
                    HG.WebApp.Utility.ConfirmPopup('Delete item', 'This action cannot be undone!', function(proceed){
                        if (proceed) {
                            var name = that.parent().parent().parent().attr('data-schedule-name');
                            HG.Automation.Scheduling.Delete(name, function () {
                                $$.LoadScheduling();
                            });
                            $$._CurrentEventName = "";
                            $$._CurrentEventIndex = -1;
                        }
                    });
                });
                item.find('[data-ui-field=btn_toggle]').on('click', function () {
                    $$.ToggleScheduleIsEnabled($(this).parent().parent().parent().attr('data-schedule-index'));
                });
            });
            $('#configure_schedulerservice_list').listview('refresh');
            //
            $('#configure_schedulerservice_list li a[data-ui-ref="edit-btn"]').bind('click', function() {
                $$._CurrentEventName = $(this).parent().attr('data-schedule-name');
                $$._CurrentEventIndex = $(this).parent().attr('data-schedule-index');
                $$.EditCurrentItem();
            });
            //
            if (callback) callback();
        });
    };

    $$.EditCurrentItem = function() {
        HG.Ui.Popup.CronWizard.open($$._CurrentEventName);
        HG.Ui.Popup.CronWizard.onChange = function(item) {
            HG.Automation.Scheduling.UpdateItem(item.Name, item, function () {
                if ($$._CurrentEventName == '')
                    $$.showEditor();
                else
                    $$.LoadScheduling($$.RefreshOccursTable);
            });
        };
    };

    $$.ToggleScheduleIsEnabled = function (index) {
        var item = $$._ScheduleList[index];
        $.mobile.loading('show');
        if (item.IsEnabled) {
            HG.Automation.Scheduling.Disable(item.Name, function () {
                $$.LoadScheduling();
            });
        }
        else {
            HG.Automation.Scheduling.Enable(item.Name, function () {
                $$.LoadScheduling();
            });
        }
    };

};