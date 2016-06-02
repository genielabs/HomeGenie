HG.WebApp.Scheduler = HG.WebApp.Scheduler || new function () { var $$ = this;

    $$._ScheduleList = {};
    $$._CurrentEventName = "";
    $$._CurrentEventIndex = -1;

    $$.InitializePage = function () {

        $('#schedulerservice_item_edit').on('popupbeforeposition', function (event) {
            $$.RefreshEventDetails();
        });
        //
        $('#schedulerservice_item_cronwizard').on('click', function (event) {
            $('#schedulerservice_item_edit').one('popupafterclose', function () {
                HG.Ui.Popup.CronWizard.element.one('popupafterclose', function () {
                    $('#schedulerservice_item_edit').popup('open');
                });
                var config = null;
                if ($('#schedulerservice_item_data').val() != '')
                    config = $.parseJSON($('#schedulerservice_item_data').val());
                else
                    config = {

                    };                    
                HG.Ui.Popup.CronWizard.open(config);
                HG.Ui.Popup.CronWizard.onChange = function (expr,config) {
                    setTimeout(function () {
                        $('#schedulerservice_item_cronexp').val(expr);
                        $('#schedulerservice_item_data').val(JSON.stringify(config));
                        $('#schedulerservice_item_desc').val(config.description);
                    }, 500);
                };
            });
            $('#schedulerservice_item_edit').popup('close');
        });
        $('#schedulerservice_item_cronexp').on('change', function() {
            $('#schedulerservice_item_data').val('');
            $('#schedulerservice_item_desc').val('');
        });
        $('#scheduleritem_update_button').on('click', function (event) {
            var name = $('#schedulerservice_item_name').val();
            var expr = $('#schedulerservice_item_cronexp').val();
            var data = $('#schedulerservice_item_data').val();
            var desc = $('#schedulerservice_item_desc').val();
            var prid = $('#schedulerservice_item_programid').val();
            HG.Automation.Scheduling.Update(name, expr, data, desc, prid, function () {
                $$.LoadScheduling();
            });
        });
        //
        $('#scheduleritem_delete_button').on('click', function (event) {
            var name = $('#schedulerservice_item_name').val();
            HG.Automation.Scheduling.Delete(name, function () {
                $$.LoadScheduling();
            });
            $$._CurrentEventName = "";
            $$._CurrentEventIndex = -1;
        });
        //
        $('#scheduleritem_add_button').on('click', function (event) {
            $('#schedulerservice_item_name').val('');
            $$._CurrentEventName = "";
            $$._CurrentEventIndex = -1;
            $$.EditCurrentItem();
        });

    };

    $$.GetItemMarkup = function (schedule) {
        var displayName = schedule.Name;
        if (displayName.indexOf('.') > 0)
            displayName = displayName.substring(displayName.indexOf('.') + 1);
        var item = '<li data-icon="' + (schedule.IsEnabled ? 'check' : 'alert') + '" data-schedule-name="' + schedule.Name + '"  data-schedule-index="' + i + '">';
        item += '<a href="#">';
        item += '   <p class="ui-li-aside ui-li-desc"><strong>&nbsp;' + schedule.ProgramId + '</strong><br><font style="opacity:0.5">' + 'Last: ' + schedule.LastOccurrence + '<br>' + 'Next: ' + schedule.NextOccurrence + '</font></p>';
        item += '   <h3 class="ui-li-heading">' + displayName + '</h3>';
        item += '   <p class="ui-li-desc" style="padding-right:160px">' + (schedule.Description != null ? schedule.Description : '') + ' <span style="opacity:0.5">(' + schedule.CronExpression + ')</span></p>';
        item += '</a>';
        item += '<a href="javascript:HG.WebApp.Scheduler.ToggleScheduleIsEnabled(\'' + i + '\')">' + (schedule.IsEnabled ? 'Tap to DISABLE item' : 'Tap to ENABLE item') + '</a>';
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
            $('#configure_schedulerservice_list').listview('refresh');
            //
            $("#configure_schedulerservice_list li").bind("click", function() {
                $$._CurrentEventName = $(this).attr('data-schedule-name');
                $$._CurrentEventIndex = $(this).attr('data-schedule-index')
                $$.EditCurrentItem();
            });
            //
            if (callback) callback();
        });
    };

    $$.EditCurrentItem = function() {
        HG.Ui.Popup.CronWizard.open($$._CurrentEventName);
        HG.Ui.Popup.CronWizard.onChange = function(item) {
            HG.Automation.Scheduling.Update(item.Name, item.CronExpression, item.Data, item.Description, item.ProgramId, function () {
                $$.LoadScheduling();
            });
        };
    };

    $$.RefreshEventDetails = function () {

        var schedule = null;
        var name = '', expr = '', data = '', desc = '', prid = '';
        if ($$._CurrentEventIndex != -1) {
            schedule = $$._ScheduleList[$$._CurrentEventIndex];
            name = schedule.Name;
            expr = schedule.CronExpression;
            data = schedule.Data;
            desc = schedule.Description;
            prid = schedule.ProgramId;
            $('#schedulerservice_item_name').addClass('ui-disabled');
            $('#schedulerservice_item_name').val(name);
        }
        else {
            $('#schedulerservice_item_name').removeClass('ui-disabled');
        }
        $('#schedulerservice_item_cronexp').val(expr);
        $('#schedulerservice_item_data').val(data);
        $('#schedulerservice_item_desc').val(desc);
        $('#schedulerservice_item_programid').val(prid);

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