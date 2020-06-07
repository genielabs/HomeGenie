HG.WebApp.Scheduler = HG.WebApp.Scheduler || new function () { var $$ = this;

    $$.PageId = 'page_configure_schedulerservice';
    $$._ScheduleList = {};
    $$._CurrentEventName = "";
    $$._CurrentEventIndex = -1;
    $$._CurrentDate = new Date();
    $$._UpdateTimeout = null;
    $$._RefreshTimeout = null;

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
            $$.showEditor();
        });
        $$.calendarButton = page.find('[data-ui-field="calendar-button"]').on('click', function(){
            $$.showCalendar();
        });
        page.find('[data-ui-field="add-button"]').on('click', function(){
            var _btn = $(this);
            $$._CurrentEventName = "";
            $$._CurrentEventIndex = -1;
            $$.EditCurrentItem();
            setTimeout(function() {
                _btn.removeClass('ui-btn-active');
            }, 200);
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

    $$.GetItemMarkup = function (schedule, i) {
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

    $$.RefreshOccursTableOnce = function() {
        if ($$._RefreshTimeout != null) clearTimeout($$._RefreshTimeout);
        $$._RefreshTimeout = setTimeout($$.RefreshOccursTable, 500);
    }
    $$.RefreshOccursTable = function() {
        var page = $('#' + $$.PageId);
        var occursList = page.find('[data-ui-field="occurs-table"]');
        var w = occursList.width(); var h = 10;
        if (w === 0) return;
        w -= 8; // margin
        var dd = moment($$._CurrentDate).format('LL');
        $$.displayDate.html(dd);
        $.mobile.loading('show');
        var startDate = new Date($$._CurrentDate.getTime());
        startDate.setHours(0,0,0,0);
        HG.Control.Modules.ApiCall('HomeAutomation.HomeGenie', 'Automation', 'Scheduling.ListOccurrences', (24*1).toString()+'/'+startDate.getTime(), function(schedules){
            occursList.empty();
            var d = new Date(); d.setSeconds(0);
            var occurrences = [];
            var currentGroup = '';
            $.each(schedules, function(k,v){
              var n = v.Name;
              if (n.indexOf('.') > 0) {
                var scheduleGroup = n.substring(0, n.indexOf('.'));
                if (scheduleGroup != currentGroup) {
                    occurrences.push({ title: scheduleGroup.replace(/\./g, ' / '), separator: true });
                    currentGroup = scheduleGroup;
                }
                n = n.substring(n.indexOf('.')+1);
              }
              var entry = { name: v.Name, title: n, occurs: [] };
              $.each($$._ScheduleList, function(sk,sv){
                if (sv.Name == v.Name) {
                    entry.index = sk;
                    entry.description = sv.Description;
                    entry.boundModules = sv.BoundModules;
                    entry.hasScript = (typeof sv.Script != 'undefined' && sv.Script != null && sv.Script.trim() != '');
                    entry.requiresBoundModules = sv.BoundModules && (sv.Script && sv.Script.indexOf('$$.boundModules.') > 0);
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

            $.each(occurrences, function(k,v){
                if (v.separator) {
                    occursList.append('<div style="text-align: left;margin-top:1.5em;width:auto;font-size:20pt;font-weight:bold">'+v.title+'</div>');
                    return true;
                }

                var scheduleTitle = $('<div/>');
                scheduleTitle.css('float', 'left');
                scheduleTitle.css('cursor', 'pointer');
                scheduleTitle.css('margin-top', '1.0em');
                scheduleTitle.click(function() {
                    $$._CurrentEventIndex = v.index;
                    $$._CurrentEventName = v.name;
                    $$.EditCurrentItem();
                });
                scheduleTitle.append('<h3 style="text-align:left;margin:0;margin-top:0.5em;line-height:16pt;font-size:16pt;vertical-align:middle"><i class="fa fa-clock-o"></i>&nbsp;&nbsp;<span>'+v.title+'</span></h3>');
                occursList.append(scheduleTitle).append('<br clear="all">');
                if (typeof v.description != 'undefined' && v.description != null && v.description.trim() !== '') {
                    var scheduleDescription = $('<div style="text-align:left;font-size:14pt;margin-bottom: 6px;margin-top:8px;opacity:0.75">' + v.description + '</div>');
                    occursList.append(scheduleDescription);
                }

                if (v.prevOccurrence > 0 || v.nextOccurrence > 0) {
                    var lastNextInfo = $('<div/>');
                    occursList.append(lastNextInfo);
                    lastNextInfo.css('margin-top', '8px');
                    lastNextInfo.css('font-size', '10pt');
                    lastNextInfo.css('width', '100%');
                    lastNextInfo.css('height', '12px');
                    if (v.prevOccurrence > 0) {
                        lastNextInfo.append('<span style="display: block; float: left;margin-left:4px"><strong style="opacity: 0.65;margin-right: 10px">LAST</strong> ' + moment(v.prevOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.prevOccurrence).from(new Date())+')</span>');
                    }
                    if (v.nextOccurrence > 0) {
                        lastNextInfo.append('<span style="display: block; float: right;margin-right:4px"> <strong style="opacity: 0.65;margin-right: 10px">NEXT</strong> ' + moment(v.nextOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.nextOccurrence).from(new Date())+')</span>');
                    }
                }

                var timeBarDiv = $('<div/>');
                timeBarDiv.addClass('hg-scheduler-table-row');
                var timeBar = Raphael(timeBarDiv[0], w, h*2.5);
                timeBar.rect(0, 0, w, h*2.5).attr({
                    fill: "rgb(90, 90, 90)",
                    stroke: "rgb(0,0,0)",
                    "stroke-width": 1
                });
                occursList.append(timeBarDiv);

                $.each(v.occurs, function(kk,vv){
                    var df = sd = new Date(vv.from);
                    sd.setHours(0,0,0,0);
                    vv.from -= sd.getTime();
                    vv.to -= sd.getTime();
                    var sx1 = Math.round(vv.from/(1440*60000)*w), sx2 = Math.round(vv.to/(1440*60000)*w)-sx1;
                    timeBar.rect(sx1-1, 0, sx2+1, h+1).attr({
                        fill: "rgba(255, 255, 70, 85)",
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

                if (v.requiresBoundModules) {
                    var scheduleModulesDiv = $('<div/>');
                    scheduleModulesDiv.css('margin-top', '10px');
                    scheduleModulesDiv.css('text-align', 'left');
                    scheduleModulesDiv.css('clear', 'both');
                    var modulesButton = $('<a class="ui-btn ui-corner-all ui-btn-icon-left ui-icon-fa-cube ui-btn-inline ui-mini" style="margin:0;margin-right: 0.5em;padding-top:4px;padding-bottom:4px">' + HG.WebApp.Locales.GetLocaleString('cronwizard_section_modules', 'Modules') + '</a>');
                    modulesButton.click(function() {
                        $$._CurrentEventIndex = v.index;
                        $$._CurrentEventName = v.name;
                        $$.EditCurrentItem(true);
                    });
                    scheduleModulesDiv.append(modulesButton);
                    if (v.boundModules.length > 0) {
                        var html = '<div style="margin:8px;margin-left: 24px;">';
                        for (i = 0; i < v.boundModules.length; i++) {
                            html += ' &nbsp;&nbsp;<span style="opacity: 0.65">&bull;</span>&nbsp;&nbsp;';
                            var moduleRef = v.boundModules[i];
                            var module = HG.WebApp.Utility.GetModuleByDomainAddress(moduleRef.Domain, moduleRef.Address);
                            if (module) {
                                html += HG.Ui.GetModuleDisplayName(module, true);
                            } else {
                                html += moduleRef.Domain  + ':' + moduleRef.Address;
                            }
                        }
                        html += '</div>';
                        scheduleModulesDiv.append(html);
                    }
                    occursList.append(scheduleModulesDiv);
                }

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

                timeBarDiv.on('mousemove',function(e,d){
                    if ($(e.target).is('rect')) {

                        var md = new Date($$._CurrentDate.getTime());
                        md.setHours(0,0,0,0);
                        md = new Date(md.getTime()+(e.offsetX / w * 1440 * 60000)-60000);
                        $$._CurrentDate = md;

                    }
                });

                // build basic tooltip data
                var desc = '<p align="center"><strong>';
                desc += moment($$._CurrentDate).format('LL');
                desc += '</strong></p>';
                // attach tooltip
                timeBarDiv.qtip({
                  content: {
                    text: desc
                  },
                  show: { delay: 350, solo: true, effect: function(offset) {
                    $(this).slideDown(100);
                  } },
                  hide: { inactive: 10000 },
                  style: {
                    classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap',
                    width: 200,
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

            // auto-refresh every minute and on window resize
            $(window).off('resize', $$.RefreshOccursTable);
            if ($$._UpdateTimeout != null) clearTimeout($$._UpdateTimeout);
            if ($.mobile.activePage.attr("id") == $$.PageId) {
                var time = Date.now() / 1000;
                var minutes = Math.floor(time / 60);
                var secondsToNextMinute = 60 - (time - minutes * 60);
                $$._UpdateTimeout = setTimeout($$.RefreshOccursTable, secondsToNextMinute * 1000);
                $(window).on('resize', $$.RefreshOccursTableOnce);
            }
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
            // element containing '.' in the name are grouped into own sections
            for (var i = 0; i < $$._ScheduleList.length; i++) {
                var schedule = $$._ScheduleList[i];
                if (schedule.Name.indexOf('.') < 0) {
                    var item = $$.GetItemMarkup(schedule, i);
                    $('#configure_schedulerservice_list').append(item);
                }
            }
            var currentGroup = '';
            for (var i = 0; i < $$._ScheduleList.length; i++) {
                var schedule = $$._ScheduleList[i];
                if (schedule.Name.indexOf('.') > 0) {
                    var scheduleGroup = schedule.Name.substring(0, schedule.Name.indexOf('.'));
                    var item = $$.GetItemMarkup(schedule, i);
                    if (scheduleGroup != currentGroup) {
                        $('#configure_schedulerservice_list').append('<li data-role="list-divider">' + scheduleGroup.replace(/\./g, ' / ') + '</li>');
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

    $$.EditCurrentItem = function(compactMode) {
        HG.Ui.Popup.CronWizard.open($$._CurrentEventName, compactMode);
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
