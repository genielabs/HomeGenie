HG.WebApp.Scheduler = HG.WebApp.Scheduler || new function () { var $$ = this;

    $$._ScheduleList = {};
    $$._CurrentEventName = "";
    $$._CurrentEventIndex = -1;

    $$.InitializePage = function () {

        $('#scheduleritem_add_button').on('click', function (event) {
            $$._CurrentEventName = "";
            $$._CurrentEventIndex = -1;
            $$.EditCurrentItem();
            setTimeout(function(){
                $('#scheduleritem_add_button').removeClass('ui-btn-active');
            }, 200);
        });

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

    $$.LoadScheduling = function (callback) {
        $.mobile.loading('show');
        HG.Automation.Scheduling.List(function (data) {
            $$._ScheduleList = data;
            //
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
            HG.Automation.Scheduling.Update(item.Name, item.CronExpression, item.Data, item.Description, item.Script, function () {
                $$.LoadScheduling();
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