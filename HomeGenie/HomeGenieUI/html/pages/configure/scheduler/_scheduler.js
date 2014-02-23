HG.WebApp.Scheduler = HG.WebApp.Scheduler || {};
HG.WebApp.Scheduler._ScheduleList = {};
HG.WebApp.Scheduler._CurrentScheduleName = "";
HG.WebApp.Scheduler._CurrentScheduleIndex = -1;

HG.WebApp.Scheduler.InitializePage = function () {

    $('#schedulerservice_item_edit').on('popupbeforeposition', function (event) {
        HG.WebApp.Scheduler.RefreshScheduleDetails();
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
        HG.WebApp.Scheduler._CurrentScheduleName = "";
        HG.WebApp.Scheduler._CurrentScheduleIndex = -1;
    });
    //
    $('#scheduleritem_add_button').bind('click', function (event) {
        HG.WebApp.Scheduler._CurrentScheduleName = "";
        HG.WebApp.Scheduler._CurrentScheduleIndex = -1;
    });

};

HG.WebApp.Scheduler.LoadScheduling = function (callback) {
    $.mobile.showPageLoadingMsg();
    HG.Automation.Scheduling.List(function (data) {
        HG.WebApp.Scheduler._ScheduleList = data;
        //
        $.mobile.hidePageLoadingMsg();
        //
        $('#configure_schedulerservice_list').empty();
        $('#configure_schedulerservice_list').append('<li data-theme="a" data-icon="false" data-role="list-divider">Scheduler Items</li>');
        //
        for (i = 0; i < HG.WebApp.Scheduler._ScheduleList.length; i++)
        {
            var schedule = HG.WebApp.Scheduler._ScheduleList[i];
            var item = '<li data-theme="' + uitheme + '" data-icon="' + (schedule.IsEnabled ? 'check' : 'alert') + '" data-schedule-name="' + schedule.Name + '"  data-schedule-index="' + i + '">';
            item += '<a href="#schedulerservice_item_edit" data-rel="popup" data-position-to="window" data-transition="pop">';
            //
//            var triggertime = '';
//            if (progrm.TriggerTime != null) {
//                var triggerts = moment(progrm.TriggerTime);
//                triggertime = triggerts.format('L LT');
//            }
            //
            item += '	<p class="ui-li-aside ui-li-desc"><strong>&nbsp;' + schedule.ProgramId + '</strong><br><font style="opacity:0.5">' + 'Last: ' + schedule.LastOccurrence + '<br>' + 'Next: ' + schedule.NextOccurrence +'</font></p>';
            item += '	<h3 class="ui-li-heading">' + schedule.Name + '</h3>';
            item += '	<p class="ui-li-desc">' + schedule.CronExpression + ' &nbsp;</p>';
            item += '</a>';
            item += '<a data-theme="' + uitheme + '" style="border:0;-moz-border-radius: 0px;-webkit-border-radius: 0px;border-radius: 0px" href="javascript:HG.WebApp.Scheduler.ToggleScheduleIsEnabled(\'' + i + '\')">' + (schedule.IsEnabled ? 'Tap to DISABLE item' : 'Tap to ENABLE item') + '</a>';
            //
            item += '</li>';
            $('#configure_schedulerservice_list').append(item);
        }
        $('#configure_schedulerservice_list').listview();
        $('#configure_schedulerservice_list').listview('refresh');
        //
        $("#configure_schedulerservice_list li").bind("click", function () {
            HG.WebApp.Scheduler._CurrentScheduleName = $(this).attr('data-schedule-name');
            HG.WebApp.Scheduler._CurrentScheduleIndex = $(this).attr('data-schedule-index')
        });
        //
        if (callback) callback();
    });
};

HG.WebApp.Scheduler.RefreshScheduleDetails = function () {

    var schedule = null;
    var name = '';
    var expr = '';
    var prid = '';
    if (HG.WebApp.Scheduler._CurrentScheduleIndex != -1) {
        schedule = HG.WebApp.Scheduler._ScheduleList[HG.WebApp.Scheduler._CurrentScheduleIndex];
        name = schedule.Name;
        expr = schedule.CronExpression;
        prid = schedule.ProgramId;
    }
    $('#schedulerservice_item_name').val(name);
    $('#schedulerservice_item_cronexp').val(expr);
    $('#schedulerservice_item_programid').val(prid);

};

HG.WebApp.Scheduler.ToggleScheduleIsEnabled = function (index) {
    var item = HG.WebApp.Scheduler._ScheduleList[index];
    $.mobile.showPageLoadingMsg();
    if (item.IsEnabled)
    {
        HG.Automation.Scheduling.Disable(item.Name, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
    }
    else
    {
        HG.Automation.Scheduling.Enable(item.Name, function () {
            HG.WebApp.Scheduler.LoadScheduling();
        });
    }
};
