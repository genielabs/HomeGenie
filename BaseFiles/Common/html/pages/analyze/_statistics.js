//
// namespace : HG.WebApp.Statistics
// info      : -
//
HG.WebApp.Statistics = HG.WebApp.Statistics || {};
HG.WebApp.Statistics._CurrentTab = 1;
HG.WebApp.Statistics._CurrentModule = '';
HG.WebApp.Statistics._CurrentParameter = 'Meter.Watts';
HG.WebApp.Statistics._CurrentGraphType = 'bars';
HG.WebApp.Statistics._RefreshIntervalObject = null;
HG.WebApp.Statistics._RefreshInterval = 2 * 60000; // stats refresh interval = 2 minutes
//
HG.WebApp.Statistics.InitializePage = function () {
    $('#page_analyze_source').on('change', function () {
        var selected = $(this).find('option:selected');
        var filter = selected.attr('data-context-domain') + ':' + selected.attr('data-context-address');
        if (filter == ':') filter = '';
        HG.WebApp.Statistics.RefreshParameters(filter);
    });
    $('#page_analyze_param').on('change', function () {
        HG.WebApp.Statistics.RefreshModules();
    });
    $('#page_analyze_graphtype').on('change', function () {
        //...
    });
    //
    $('#page_analyze_renderbutton').bind('click', function () {
        var selected = $('#page_analyze_source').find('option:selected');
        HG.WebApp.Statistics._CurrentModule = selected.attr('data-context-domain') + ':' + selected.attr('data-context-address');
        if (HG.WebApp.Statistics._CurrentModule == ':') HG.WebApp.Statistics._CurrentModule = '';
        HG.WebApp.Statistics._CurrentParameter = $('#page_analyze_param').val();
        HG.WebApp.Statistics._CurrentGraphType = $('#page_analyze_graphtype').val();
        //
        if (HG.WebApp.Statistics._CurrentParameter.substring(0, 6) == 'Meter.' || HG.WebApp.Statistics._CurrentParameter.substring(0, 13) == 'PowerMonitor.') {
            $('#statistics_tab2_button').show();
        }
        else {
            $('#statistics_tab2_button').hide();
        }
        HG.WebApp.Statistics.SetTab(1);
        //
        $('#page_analyze_title').html($('#page_analyze_source').find('option:selected').text() + ', ' + $('#page_analyze_param').find('option:selected').text());
        $('#analyze_stats_options').popup('close');
    });
    //
    $('#page_analyze_costperunit').on('change', function () {
        var cost = $('#page_analyze_costperunit').val() * $('#page_analyze_totalunits').val();
        $('#page_analyze_totalcost').val(cost.toFixed(2));
    });
    //
    $('#analyze_stats_options').on('popupbeforeposition', function (event) {
        HG.WebApp.Statistics.RefreshModules();
        HG.WebApp.Statistics.RefreshParameters();
    });
    // datebox field events
    $('#page_analyze_datefrom').val('');
    $('#page_analyze_datefrom').on('change', function(){
        HG.WebApp.Statistics.Refresh();
    });
    $('#page_analyze_dateto').val('');
    $('#page_analyze_dateto').on('change', function(){
        HG.WebApp.Statistics.Refresh();
    });
    // tooltips
    $("#statshour").qtip({
        id: 'flot',
        prerender: true,
        content: ' ',
        position: {
            target: 'mouse',
            viewport: $('#flot'),
            adjust: {
                x: 5
            }
        },
        show: false,
        hide: {
            event: false,
            fixed: true
        }
    });
    $("#statshour").bind("plothover", function (event, pos, item) {
        // Grab the API reference
        var graph = $(this),
            api = graph.qtip(),
            previousPoint;
        // If we weren't passed the item object, hide the tooltip and remove cached point data
        if (!item) {
            api.cache.point = false;
            return api.hide(item);
        }
        previousPoint = api.cache.point;
        if (previousPoint !== item.dataIndex) {
            var offset = new Date().getTimezoneOffset() * 60 * 1000;
            api.cache.point = item.dataIndex;

            api.set('content.text',
            item.series.label + " at " + new Date(item.datapoint[0] + offset).toLocaleTimeString() + " = " + item.datapoint[1].toFixed(2));

            api.elements.tooltip.stop(1, 1);
            api.show(item);
        }
    });
    $("#statscounter").qtip({
        id: 'flot',
        prerender: true,
        content: ' ',
        position: {
            target: 'mouse',
            viewport: $('#flot'),
            adjust: {
                x: 5
            }
        },
        show: false,
        hide: {
            event: false,
            fixed: true
        }
    });
    $("#statscounter").bind("plothover", function (event, pos, item) {
        // Grab the API reference
        var graph = $(this),
            api = graph.qtip(),
            previousPoint;
        // If we weren't passed the item object, hide the tooltip and remove cached point data
        if (!item) {
            api.cache.point = false;
            return api.hide(item);
        }
        previousPoint = api.cache.point;
        if (previousPoint !== item.dataIndex) {
            var offset = new Date().getTimezoneOffset() * 60 * 1000;
            api.cache.point = item.dataIndex;

            api.set('content.text',
            item.series.label + " at " + new Date(item.datapoint[0] + offset).toLocaleTimeString() + " = " + item.datapoint[1].toFixed(2));

            api.elements.tooltip.stop(1, 1);
            api.show(item);
        }
    });
};

HG.WebApp.Statistics.SetTab = function (tabindex) {
    HG.WebApp.Statistics._CurrentTab = tabindex;
    $('#statistics_tab1').hide();
    $('#statistics_tab2').hide();
    $('#statistics_tab1_button').removeClass('ui-btn-active');
    $('#statistics_tab2_button').removeClass('ui-btn-active');
    $('#statistics_tab' + tabindex).show();
    $('#statistics_tab' + tabindex + '_button').addClass('ui-btn-active');
    HG.WebApp.Statistics.Refresh();
};

HG.WebApp.Statistics.SetAutoRefresh = function (autorefresh) {
    if (HG.WebApp.Statistics._RefreshIntervalObject != null) clearInterval(HG.WebApp.Statistics._RefreshIntervalObject);
    HG.WebApp.Statistics._RefreshIntervalObject = null;
    if (autorefresh) {
        HG.WebApp.Statistics._RefreshIntervalObject = setInterval('HG.WebApp.Statistics.Refresh();', HG.WebApp.Statistics._RefreshInterval);
    }
};

HG.WebApp.Statistics.InitConfiguration = function () {
    HG.Statistics.ServiceCall('Configuration.Get', '', '', function (configs) {
        var setting = eval(configs)[0];
        var sec = (setting.StatisticsUIRefreshSeconds * 1);
        HG.WebApp.Statistics._RefreshInterval = sec * 1000; //2 * 60000;
        HG.WebApp.Statistics.SetAutoRefresh(true);
        HG.WebApp.Statistics.SetTab(1);
    });
}

HG.WebApp.Statistics.RefreshParameters = function (filter) {
    var cval = $('#page_analyze_param').val();
    if (cval == '') cval = 'Meter.Watts'; // default param
    $('#page_analyze_param').empty();
    HG.Statistics.ServiceCall('Parameter.List', filter, '', function (stats) {
        for (var p = 0; p < stats.length; p++) {
            var displayname = stats[p];
            if (displayname.indexOf('.') > 0) displayname = displayname.substring(displayname.indexOf('.') + 1);
            $('#page_analyze_param').append('<option value="' + stats[p] + '"' + (stats[p] == cval ? ' selected' : '') + '>' + displayname + '</option>');
        }
        $('#page_analyze_param').selectmenu('refresh');
    });
}

HG.WebApp.Statistics.RefreshModules = function () {

    var coption = $('#page_analyze_source').find('option:selected');
    var sdomain = coption.attr('data-context-domain');
    var saddress = coption.attr('data-context-address');
    //
    $('#page_analyze_source').empty();
    $('#page_analyze_source').append('<option data-context-domain="" data-context-address="" value="">Global</option>');
    //
    var datasources = '';
    for (var i = 0; i < HG.WebApp.Data.Groups.length; i++) {
        var g = HG.WebApp.Data.Groups[i];
        var groupmodules = HG.Configure.Groups.GetGroupModules(g.Name).Modules;
        var datamodules = '';
        for (var c = 0; c < groupmodules.length; c++) {
            var mod = groupmodules[c];
            if (typeof mod.Properties != 'undefined')
                for (var p = 0; p < mod.Properties.length; p++) {
                    if (mod.Properties[p].Name == $('#page_analyze_param').val()) {
                        var name = mod.Domain + ':' + mod.Address;
                        if (mod.Name != '') name = mod.Name;
                        //
                        var selected = (mod.Domain == sdomain && mod.Address == saddress);
                        //
                        datamodules += '<option data-context-domain="' + mod.Domain + '" data-context-address="' + mod.Address + '"' + (selected ? ' selected' : '') + '>' + name + '</option>';
                        break;
                    }
                }
        }
        if (datamodules != '') {
            datasources += '<optgroup label="' + g.Name + '">';
            datasources += datamodules;
            datasources += '</optgroup>';
        }
    }
    $('#page_analyze_source').append(datasources);
    //
    $('#page_analyze_source').selectmenu('refresh');
};

HG.WebApp.Statistics.Refresh = function () {
    $.mobile.loading('show');
    var showsplines = (HG.WebApp.Statistics._CurrentGraphType == 'splines' ? true : false);
    var showlines = (HG.WebApp.Statistics._CurrentGraphType == 'lines' ? true : false);
    var showbars = (HG.WebApp.Statistics._CurrentGraphType == 'bars' ? true : false);
    if (HG.WebApp.Statistics._CurrentTab == 1)
    {
        HG.Statistics.ServiceCall('Global.TimeRange', '', '', function (res) {
            var trange = eval(res)[0];
            var start = new Date(trange.StartTime * 1);
            var end = new Date(trange.EndTime * 1);
            var today = new Date();
            var minDays = Math.floor((today - start) / (1000 * 60 * 60 * 24));
            $('#page_analyze_datefrom').datebox('option', { 'minDays': minDays, 'maxDays': 0, 'useLang': HG.WebApp.Locales.GetUserLanguage() });
            $('#page_analyze_dateto').datebox('option', { 'minDays': minDays, 'maxDays': 0, 'useLang': HG.WebApp.Locales.GetUserLanguage() });
            if ($('#page_analyze_datefrom').val() == '') {
                $('#page_analyze_datefrom').datebox('setTheDate', today);
                $('#page_analyze_datefrom').trigger('datebox', {'method':'doset'})
            }
            if ($('#page_analyze_dateto').val() == '') {
                $('#page_analyze_dateto').datebox('setTheDate', today);
                $('#page_analyze_dateto').trigger('datebox', {'method':'doset'})
            }
            $.ajax({
                url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatsHour/' + HG.WebApp.Statistics._CurrentParameter + '/' + HG.WebApp.Statistics._CurrentModule + '/' + $('#page_analyze_datefrom').datebox('getTheDate').getTime() + '/' + $('#page_analyze_dateto').datebox('getTheDate').getTime(),
                type: 'GET',
                success: function (data) {
                    var stats = eval(data);
                    try {
                        $.plot($("#statshour"), [
                                { label: 'Max', data: stats[1], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                                { label: 'Avg', data: stats[2], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                                { label: 'Min', data: stats[0], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                                { label: 'Today Avg', data: stats[3], bars: { show: showbars, barWidth: (10 * 60 * 1000), align: 'center', steps: false } },
                                { label: 'Today Detail', data: stats[4], lines: { show: true, lineWidth: 2.0 }, bars: { show: false }, splines: { show: false }, points: { show: false } }
                            ],
                            {
                                yaxis: { show: true },
                                xaxis: { mode: "time", timeformat: "%H", minTickSize: [1, "hour"], tickSize: [1, "hour"] },
                                legend: { position: "nw", noColumns: 5, backgroundOpacity: 0.3 },
                                lines: { show: showlines, lineWidth: 1.0 },
                                series: {
                                    splines: { show: showsplines }
                                },
                                grid: {
                                    backgroundColor: { colors: ["#fff", "#ddd"] },
                                    hoverable: true
                                },
                                colors: ["rgba(200, 255, 0, 0.5)", "rgba(120, 160, 0, 0.5)", "rgba(40, 70, 0, 0.5)", "rgba(110, 80, 255, 0.5)", "rgba(200, 30, 0, 1.0)"], //"rgba(0, 30, 180, 1.0)"
                                points: { show: true },
                                zoom: {
                                    interactive: true
                                },
                                pan: {
                                    interactive: true
                                }  
                            });
                    } catch (e) { }
                    $.mobile.loading('hide');
                }
            });
        });
    }
    else    
    {
        HG.Statistics.ServiceCall('Global.CounterTotal', HG.WebApp.Statistics._CurrentParameter, '', function (total) {
            $('#page_analyze_totalunits').val((total * 1).toFixed(2));
            var cost = $('#page_analyze_costperunit').val() * $('#page_analyze_totalunits').val();
            $('#page_analyze_totalcost').val(cost.toFixed(2));
        });
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.Counter/' + HG.WebApp.Statistics._CurrentParameter + '/' + HG.WebApp.Statistics._CurrentModule + '/' + $('#page_analyze_datefrom').datebox('getTheDate').getTime() + '/' + $('#page_analyze_dateto').datebox('getTheDate').getTime(),
            type: 'GET',
            success: function (data) {
                var stats = eval(data);
                //
                var total = 0.0;
                for (var s = 0; s < stats[0].length; s++) {
                    total += stats[0][s][1];
                }
                $('#page_analyze_cost_counter').html('Counter ' + (Math.round(total * 100) / 100));
                //
                try {
                    $.plot($("#statscounter"), [{
                            label: HG.WebApp.Statistics._CurrentParameter,
                            data: stats[0]
                        }],
                        {
                            yaxis: { show: true },
                            xaxis: { mode: "time", timeformat: "%H", minTickSize: [1, "hour"], tickSize: [1, "hour"] },
                            legend: { position: "nw", backgroundOpacity: 0.3 },
                            lines: { show: showlines, lineWidth: 1.5 },
                            series: {
                                splines: { show: showsplines }
                            },
                            bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true },
                            grid: {
                                backgroundColor: { colors: ["#fff", "#ddd"] },
                                hoverable: true
                            },
                            colors: ["rgba(120, 160, 0, 0.5)"],
                            points: { show: true },
                            zoom: {
                                interactive: true
                            },
                            pan: {
                                interactive: true
                            }    
                        });
                } catch (e) { }
                //
                $.mobile.loading('hide');
            }
        });
    }
};
