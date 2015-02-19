HG.WebApp.Scheduler = HG.WebApp.Scheduler || {};
HG.WebApp.Scheduler._ScheduleList = {};
HG.WebApp.Scheduler._CurrentEventName = "";
HG.WebApp.Scheduler._CurrentEventIndex = -1;

HG.WebApp.Scheduler.InitializePage = function () {

    $('#schedulerservice_item_edit').on('popupbeforeposition', function (event) {
        HG.WebApp.Scheduler.RefreshEventDetails();
    });
    //
    $('#scheduleritem_update_button').bind('click', function (event) {
        var name = $('#schedulerservice_item_name').val();
        var expr = $('#schedulerservice_item_cronexp').val();
        var prid = $('#schedulerservice_item_programid').val();
        HG.Automation.Scheduling.Update(name, expr, prid, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
    });
    //
    $('#scheduleritem_delete_button').bind('click', function (event) {
        var name = $('#schedulerservice_item_name').val();
        HG.Automation.Scheduling.Delete(name, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
        HG.WebApp.Scheduler._CurrentEventName = "";
        HG.WebApp.Scheduler._CurrentEventIndex = -1;
    });
    //
    $('#scheduleritem_add_button').bind('click', function (event) {
        HG.WebApp.Scheduler._CurrentEventName = "";
        HG.WebApp.Scheduler._CurrentEventIndex = -1;
    });

};

HG.WebApp.Scheduler.GetItemMarkup = function (schedule) {
    var displayName = schedule.Name;
    if (displayName.indexOf('.') > 0)
        displayName = displayName.substring(displayName.indexOf('.') + 1);
    var item = '<li data-icon="' + (schedule.IsEnabled ? 'check' : 'alert') + '" data-schedule-name="' + schedule.Name + '"  data-schedule-index="' + i + '">';
    item += '<a href="#schedulerservice_item_edit" data-rel="popup" data-position-to="window" data-transition="pop">';
    //
    //            var triggertime = '';
    //            if (progrm.TriggerTime != null) {
    //                var triggerts = moment(progrm.TriggerTime);
    //                triggertime = triggerts.format('L LT');
    //            }
    //
    item += '	<p class="ui-li-aside ui-li-desc"><strong>&nbsp;' + schedule.ProgramId + '</strong><br><font style="opacity:0.5">' + 'Last: ' + schedule.LastOccurrence + '<br>' + 'Next: ' + schedule.NextOccurrence + '</font></p>';
    item += '	<h3 class="ui-li-heading">' + displayName + '</h3>';
    item += '	<p class="ui-li-desc">' + schedule.CronExpression + ' &nbsp;</p>';
    item += '</a>';
    item += '<a href="javascript:HG.WebApp.Scheduler.ToggleScheduleIsEnabled(\'' + i + '\')">' + (schedule.IsEnabled ? 'Tap to DISABLE item' : 'Tap to ENABLE item') + '</a>';
    //
    item += '</li>';
    return item;
}

HG.WebApp.Scheduler.LoadScheduling = function (callback) {
    $.mobile.loading('show');
    HG.Automation.Scheduling.List(function (data) {
        HG.WebApp.Scheduler._ScheduleList = data;
        //
        $.mobile.loading('hide');
        //
        $('#configure_schedulerservice_list').empty();
        $('#configure_schedulerservice_list').append('<li data-icon="false" data-role="list-divider">' + HG.WebApp.Locales.GetLocaleString('configure_scheduler_events') + '</li>');
        //
        // element containing '.' in the name are grouped in own sections
        for (i = 0; i < HG.WebApp.Scheduler._ScheduleList.length; i++) {
            var schedule = HG.WebApp.Scheduler._ScheduleList[i];
            if (schedule.Name.indexOf('.') < 0) {
                var item = HG.WebApp.Scheduler.GetItemMarkup(schedule);
                $('#configure_schedulerservice_list').append(item);
            }
        }
        var currentGroup = '';
        for (i = 0; i < HG.WebApp.Scheduler._ScheduleList.length; i++) {
            var schedule = HG.WebApp.Scheduler._ScheduleList[i];
            if (schedule.Name.indexOf('.') > 0) {
                var scheduleGroup = schedule.Name.substring(0, schedule.Name.indexOf('.'));
                var item = HG.WebApp.Scheduler.GetItemMarkup(schedule);
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
        $("#configure_schedulerservice_list li").bind("click", function () {
            HG.WebApp.Scheduler._CurrentEventName = $(this).attr('data-schedule-name');
            HG.WebApp.Scheduler._CurrentEventIndex = $(this).attr('data-schedule-index')
        });
        //
        if (callback) callback();
    });
};

HG.WebApp.Scheduler.RefreshEventDetails = function () {

    var schedule = null;
    var name = '';
    var expr = '';
    var prid = '';
    if (HG.WebApp.Scheduler._CurrentEventIndex != -1) {
        schedule = HG.WebApp.Scheduler._ScheduleList[HG.WebApp.Scheduler._CurrentEventIndex];
        name = schedule.Name;
        expr = schedule.CronExpression;
        prid = schedule.ProgramId;
        $('#schedulerservice_item_name').addClass('ui-disabled');
    }
    else {
        $('#schedulerservice_item_name').removeClass('ui-disabled');
    }
    $('#schedulerservice_item_name').val(name);
    $('#schedulerservice_item_cronexp').val(expr);
    $('#schedulerservice_item_programid').val(prid);

};

HG.WebApp.Scheduler.ToggleScheduleIsEnabled = function (index) {
    var item = HG.WebApp.Scheduler._ScheduleList[index];
    $.mobile.loading('show');
    if (item.IsEnabled) {
        HG.Automation.Scheduling.Disable(item.Name, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
    }
    else {
        HG.Automation.Scheduling.Enable(item.Name, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
    }
};
