HG.WebApp.Statistics = HG.WebApp.Statistics || new function () { var $$ = this;

    $$._CurrentTab = 1;
    $$._InitDateFromTo = 4;
    $$._CurrentModule = '';
    $$._CurrentParameter = 'Meter.Watts';
    $$._CurrentGraphType = 'bars';
    $$._CurrentType = 'hours';
    $$._RefreshIntervalObject = null;
    $$._RefreshInterval = 5 * 60000; // stats refresh interval = 2 minutes
    $$._SelItemObject = false;
    $$._ItemObject = '';

    $$.InitializePage = function () {
        $('#page_analyze_source').on('change', function () {
            var selected = $(this).find('option:selected');
            var filter = selected.attr('data-context-domain') + ':' + selected.attr('data-context-address');
            if (filter == ':') filter = '';
            $$.RefreshParameters(filter);
        });
        $('#page_analyze_param').on('change', function () {
            $$.RefreshModules();
        });
        //$('#page_analyze_graphtype').on('change', function () {
        //    //...
        //});
        //
        $('#page_analyze_renderbutton').bind('click', function () {
            var selected = $('#page_analyze_source').find('option:selected');
            $$._CurrentModule = selected.attr('data-context-domain') + ':' + selected.attr('data-context-address');
            if ($$._CurrentModule == ':') $$._CurrentModule = '';
            $$._CurrentParameter = $('#page_analyze_param').val();
            //$$._CurrentGraphType = $('#page_analyze_graphtype').val();
            $$._CurrentType = $('#page_analyze_type').val();
            //
            if ($$._CurrentParameter.substring(0, 6) == 'Meter.' || $$._CurrentParameter.substring(0, 13) == 'PowerMonitor.') {
                $('#statistics_tab3_button').show();
            } else {
                $('#statistics_tab3_button').hide();
            }
            //
            $$.SetTab(1);

            $('#page_analyze_title').html($('#page_analyze_source').find('option:selected').text() + ' - ' + $('#page_analyze_param').find('option:selected').text());
            $('#page_analyze_title2').html($('#page_analyze_title').val());
            $('#analyze_stats_options').popup('close');
        });
        //
        $('#page_analyze_costperunit').on('change', function () {
            var cost = $('#page_analyze_costperunit').val() * $('#page_analyze_totalunits').val();
            $('#page_analyze_totalcost').val(cost.toFixed(2));
            HG.WebApp.Store.set('UI.Statistics.CostPerUnit', $('#page_analyze_costperunit').val());
        });
        //
        $('#analyze_stats_options').on('popupbeforeposition', function (event) {
            $$.RefreshModules();
            $$.RefreshParameters();
        });
        // datebox Hour field events
        $('#page_analyze_datefrom').val('');
        $('#page_analyze_datefrom').on('change', function () {
            if ($$._InitDateFromTo == 0)
                $$.Refresh();
            else
                $$._InitDateFromTo -= 1;
        });
        $('#page_analyze_dateto').val('');
        $('#page_analyze_dateto').on('change', function () {
            if ($$._InitDateFromTo == 0)
                $$.Refresh();
            else
                $$._InitDateFromTo -= 1;
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
                if ($$._CurrentType == 'days') {
                    if ($$._SelItemObject == false) {
                        $("#page_delete_stat").html("Delete Value : " + item.datapoint[1]);
                        $$._ItemObject = item.datapoint[0] + '/' + item.datapoint[1];
                        api.elements.tooltip.stop(1, 1);
                        api.show(item);
                    }
                } else {
                    api.elements.tooltip.stop(1, 1);
                    api.show(item);
                }
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
        $("#page_delete_stat").on('click', function () {
            if ($$._SelItemObject == true) {
                $.ajax({
                    url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatDelete/' + $$._ItemObject,
                    type: 'GET',
                    success: function (data) {
                        var stats = eval(data);
//                    $("#page_analyze_title").html(data) ;
                        $.mobile.loading('hide');
                    },
                    error: function (xhr, status, error) {
                        console.log('STATISTICS ERROR: ' + xhr.status + ':' + xhr.statusText);
                        $.mobile.loading('hide');
                    }
                });
                $$._ItemObject = '';
            }
            $$._SelItemObject = false;
            $("#page_delete_stat").hide();
        });
        $("#statshour").on('click', function () {
            if ($$._SelItemObject == false) {
                if ($$._ItemObject != '') {
                    $$._SelItemObject = true;
                    $("#page_delete_stat").show();
                }
            }
            else {
                $$._ItemObject = '';
                $$._SelItemObject = false;
                $("#page_delete_stat").hide();
            }
        });
    };

    $$.SetTab = function (tabindex) {
        $$._CurrentTab = tabindex;
        $('#statistics_tab1').hide();
        $('#statistics_tab2').hide();
        $('#statistics_tab1_button').removeClass('ui-btn-active');
        $('#statistics_tab2_button').removeClass('ui-btn-active');
        $('#statistics_tab' + tabindex).show();
        $('#statistics_tab' + tabindex + '_button').addClass('ui-btn-active');
        $$.Refresh();
    };

    $$.SetAutoRefresh = function (autorefresh) {
        if ($$._RefreshIntervalObject != null) clearInterval($$._RefreshIntervalObject);
        $$._RefreshIntervalObject = null;
        if (autorefresh) {
            $$._RefreshIntervalObject = setInterval('HG.WebApp.Statistics.Refresh();', $$._RefreshInterval);
        }
    };

    $$.InitConfiguration = function () {
        // cost per unit default value
        $('#page_analyze_costperunit').val(HG.WebApp.Store.get('UI.Statistics.CostPerUnit') ? HG.WebApp.Store.get('UI.Statistics.CostPerUnit') : 0.00022);
        // read stats settings
        HG.Statistics.ServiceCall('Configuration.Get', '', '', function (setting) {
            var sec = (setting.StatisticsUIRefreshSeconds * 1);
            $$._RefreshInterval = sec * 1000; //2 * 60000;
            $$.SetAutoRefresh(true);
            $$.SetTab(1);
        });
    };

    $$.RefreshParameters = function (filter) {
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
    };

    $$.RefreshModules = function () {

        var coption = $('#page_analyze_source').find('option:selected');
        var sdomain = coption.attr('data-context-domain');
        var saddress = coption.attr('data-context-address');
        //
        $('#page_analyze_source').empty();
        $('#page_analyze_source').append('<option data-context-domain="" data-context-address="" value="">Global averages</option>');
        var allSelected = (sdomain == "All");
        $('#page_analyze_source').append('<option data-context-domain="All" data-context-address="" value=""' + (allSelected ? ' selected' : '') + '>Compare all</option>');
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

    $$.Refresh = function () {
        $.mobile.loading('show');
        var showsplines = ($$._CurrentGraphType == 'splines' ? true : false);
        var showlines = ($$._CurrentGraphType == 'lines' ? true : false);
        var showbars = ($$._CurrentGraphType == 'bars' ? true : false);
        var showtype = ($$._CurrentType == 'hours' ? true : false);
        if ($$._CurrentTab == 1) {
            if ($$._SelItemObject == false)
                $("#page_delete_stat").hide();
            HG.Statistics.ServiceCall('Global.TimeRange', '', '', function (trange) {
                var start = new Date(parseFloat(trange.StartTime));
                var end = new Date(parseFloat(trange.EndTime));
                var today = new Date();
                var minDays = Math.ceil((today - start) / (1000 * 60 * 60 * 24));
                $('#page_analyze_datefrom').datebox('option', {
                    'minDays': minDays,
                    'maxDays': 0,
                    'useLang': HG.WebApp.Utility.GetDateBoxLocale()
                }).datebox('refresh');
                $('#page_analyze_dateto').datebox('option', {
                    'minDays': minDays,
                    'maxDays': 0,
                    'useLang': HG.WebApp.Utility.GetDateBoxLocale()
                }).datebox('refresh');
                if ($('#page_analyze_datefrom').val() == '') {
                    $('#page_analyze_datefrom').datebox('setTheDate', today);
                    $('#page_analyze_datefrom').trigger('datebox', {'method': 'doset'})
                }
                if ($('#page_analyze_dateto').val() == '') {
                    $('#page_analyze_dateto').datebox('setTheDate', today);
                    $('#page_analyze_dateto').trigger('datebox', {'method': 'doset'})
                }
                var dfrom = new Date($('#page_analyze_datefrom').datebox('getTheDate').getTime());
                var dto = new Date($('#page_analyze_dateto').datebox('getTheDate').getTime());
                dfrom.setHours(0, 0, 0, 0);
                dto.setHours(23, 59, 59, 0);
                if ($$._CurrentModule != "All:") {
                    
                    if (showtype == true) {
                        
                        $.ajax({
                            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatsHour/' + $$._CurrentParameter + '/' + $$._CurrentModule + '/' + dfrom.getTime() + '/' + dto.getTime(),
                            type: 'GET',
                            success: function (data) {
                                var stats = eval(data);
                                try {
                                    if ($$._CurrentParameter == 'Sensor.Temperature') {
                                        // convert to farehnehit if needed
                                        for (var sx = 0; sx < stats.length; sx++)
                                            for (var tx = 0; tx < stats[sx].length; tx++)
                                                stats[sx][tx][1] = HG.WebApp.Utility.GetLocaleTemperature(stats[sx][tx][1]);
                                    }
                                    var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
                                    $.plot($("#statshour"), [
                                            {
                                                label: 'Max',
                                                data: stats[1],
                                                bars: {
                                                    show: showbars,
                                                    barWidth: (30 * 60 * 1000),
                                                    align: 'center',
                                                    steps: true
                                                }
                                            },
                                            {
                                                label: 'Avg',
                                                data: stats[2],
                                                bars: {
                                                    show: showbars,
                                                    barWidth: (30 * 60 * 1000),
                                                    align: 'center',
                                                    steps: true
                                                }
                                            },
                                            {
                                                label: 'Min',
                                                data: stats[0],
                                                bars: {
                                                    show: showbars,
                                                    barWidth: (30 * 60 * 1000),
                                                    align: 'center',
                                                    steps: true
                                                }
                                            },
                                            {
                                                label: 'Today Avg',
                                                data: stats[3],
                                                bars: {
                                                    show: showbars,
                                                    barWidth: (10 * 60 * 1000),
                                                    align: 'center',
                                                    steps: false
                                                }
                                            },
                                            {
                                                label: 'Today Detail',
                                                data: stats[4],
                                                lines: {show: true, lineWidth: 2.0},
                                                bars: {show: false},
                                                splines: {show: false},
                                                points: {show: false}
                                            }
                                        ],
                                        {
                                            yaxis: {show: true},
                                            xaxis: {
                                                mode: "time",
                                                timeformat: (dateFormat == "MDY12" ? "%h:00%p" : "%h:00"),
                                                minTickSize: [1, "hour"],
                                                tickSize: [1, "hour"]
                                            },
                                            legend: {position: "nw", noColumns: 5, backgroundOpacity: 0.3},
                                            lines: {show: showlines, lineWidth: 1.0},
                                            series: {
                                                splines: {show: showsplines}
                                            },
                                            grid: {
                                                backgroundColor: {colors: ["#fff", "#ddd"]},
                                                hoverable: true
                                            },
                                            colors: ["rgba(200, 255, 0, 0.5)", "rgba(120, 160, 0, 0.5)", "rgba(40, 70, 0, 0.5)", "rgba(110, 80, 255, 0.5)", "rgba(200, 30, 0, 1.0)"], //"rgba(0, 30, 180, 1.0)"
                                            points: {show: true},
                                            zoom: {
                                                interactive: true
                                            },
                                            pan: {
                                                interactive: true
                                            }
                                        });
                                } catch (e) {
                                    console.log(e);
                                }
                                $.mobile.loading('hide');
                            },
                            error: function (xhr, status, error) {
                                console.log('STATISTICS ERROR: ' + xhr.status + ':' + xhr.statusText);
                                $.mobile.loading('hide');
                            }
                        });
                        
                    } else {
                        
                        $.ajax({
                            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatsDay/' + $$._CurrentParameter + '/' + $$._CurrentModule + '/' + dfrom.getTime() + '/' + dto.getTime(),
                            type: 'GET',
                            success: function (data) {
                                var stats = eval(data);
                                try {
                                    if ($$._CurrentParameter == 'Sensor.Temperature') {
                                        // convert to farehnehit if needed
                                        for (var sx = 0; sx < stats.length; sx++)
                                            for (var tx = 0; tx < stats[sx].length; tx++)
                                                stats[sx][tx][1] = HG.WebApp.Utility.GetLocaleTemperature(stats[sx][tx][1]);
                                    }
                                    $.plot($("#statshour"), [
                                            {
                                                label: $('#page_analyze_title').text(),
                                                data: stats[0],
                                                lines: {show: true, lineWidth: 1.0},
                                                bars: {show: false},
                                                splines: {show: false},
                                                points: {show: false}
                                            }
                                        ],
                                        {
                                            yaxis: {show: true},
                                            xaxis: {
                                                mode: "time",
                                                timeformat: "%d/%m",
                                                minTickSize: [1, "day"],
                                                tickSize: [1, "day"]
                                            },
                                            legend: {position: "nw", noColumns: 5, backgroundOpacity: 0.3},
                                            lines: {show: showlines, lineWidth: 1.0},
                                            series: {
                                                splines: {show: showsplines}
                                            },
                                            grid: {
                                                backgroundColor: {colors: ["#fff", "#ddd"]},
                                                hoverable: true
                                            },
                                            colors: ["rgba(200, 30, 0, 1.0)"],
                                            points: {show: true},
                                            zoom: {
                                                interactive: true
                                            },
                                            pan: {
                                                interactive: true
                                            }
                                        });
                                } catch (e) {
                                    console.log(e);
                                }
                                $.mobile.loading('hide');
                            },
                            error: function (xhr, status, error) {
                                console.log('STATISTICS ERROR: ' + xhr.status + ':' + xhr.statusText);
                                $.mobile.loading('hide');
                            }
                        });
                    }
                    
                } else {
                    
                    var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
                    $.ajax({
                        url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatsMultiple/' + $$._CurrentParameter + '/' + $$._CurrentModule + '/' + dfrom.getTime() + '/' + dto.getTime(),
                        type: 'GET',
                        success: function (data) {
                            var name = '';
                            var tformat = "";
                            var tickSize = "";
                            var graph_data = [];
                            var stats = eval(data);
                            if ($$._CurrentParameter == 'Sensor.Temperature') {
                                // convert to farehnehit if needed
                                for (var sx = 1; sx < stats.length; sx += 2)
                                    for (var tx = 0; tx < stats[sx].length; tx++)
                                        stats[sx][tx][1] = HG.WebApp.Utility.GetLocaleTemperature(stats[sx][tx][1]);
                            }
                            if (showtype == true) {
                                tformat = (dateFormat == "MDY12" ? "%h:00%p" : "%h:00");
                                tickSize = "hour";
                            } else {
                                tformat = "%d/%m";
                                tickSize = "day";
                            }
                            $.each(stats, function (index, val) {
                                if (index % 2)
                                    graph_data.push({
                                        label: name,
                                        data: val,
                                        lines: {show: true, lineWidth: 2.0},
                                        bars: {show: false},
                                        splines: {show: false},
                                        points: {show: false}
                                    });
                                else
                                    name = val;
                            });
                            $.plot($("#statshour"), graph_data,
                                {
                                    yaxis: {show: true},
                                    xaxis: {
                                        mode: "time",
                                        timeformat: tformat,
                                        minTickSize: [1, tickSize],
                                        tickSize: [1, tickSize]
                                    },
                                    legend: {backgroundOpacity: 0.3},
                                    grid: {
                                        backgroundColor: {colors: ["#fff", "#ddd"]},
                                        hoverable: true
                                    },
                                    points: {show: true},
                                    zoom: {
                                        interactive: true
                                    },
                                    pan: {
                                        interactive: true
                                    }
                                });
                            $.mobile.loading('hide');
                        },
                        error: function (xhr, status, error) {
                            console.log('STATISTICS ERROR: ' + xhr.status + ':' + xhr.statusText);
                            $.mobile.loading('hide');
                        }
                    });
                }
            });
            
        } else {
            
            HG.Statistics.ServiceCall('Global.CounterTotal', $$._CurrentParameter, '', function (total) {
                $('#page_analyze_totalunits').val((total * 1).toFixed(2));
                var cost = $('#page_analyze_costperunit').val() * $('#page_analyze_totalunits').val();
                $('#page_analyze_totalcost').val(cost.toFixed(2));
            });
            var dfrom = new Date($('#page_analyze_datefrom').datebox('getTheDate').getTime());
            var dto = new Date($('#page_analyze_dateto').datebox('getTheDate').getTime());
            dfrom.setHours(0, 0, 0, 0);
            dto.setHours(23, 59, 59, 0);
            $.ajax({
                url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.Counter/' + $$._CurrentParameter + '/' + $$._CurrentModule + '/' + dfrom.getTime() + '/' + dto.getTime(),
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
                        var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
                        $.plot($("#statscounter"), [{
                                label: $$._CurrentParameter,
                                data: stats[0]
                            }],
                            {
                                yaxis: {show: true},
                                xaxis: {
                                    mode: "time",
                                    timeformat: (dateFormat == "MDY12" ? "%h:00%p" : "%h:00"),
                                    minTickSize: [1, "hour"],
                                    tickSize: [1, "hour"]
                                },
                                legend: {position: "nw", backgroundOpacity: 0.3},
                                lines: {show: showlines, lineWidth: 1.5},
                                series: {
                                    splines: {show: showsplines}
                                },
                                bars: {show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true},
                                grid: {
                                    backgroundColor: {colors: ["#fff", "#ddd"]},
                                    hoverable: true
                                },
                                colors: ["rgba(120, 160, 0, 0.5)"],
                                points: {show: true},
                                zoom: {
                                    interactive: true
                                },
                                pan: {
                                    interactive: true
                                }
                            });
                    } catch (e) {
                        console.log(e);
                    }
                    //
                    $.mobile.loading('hide');
                },
                error: function (xhr, status, error) {
                    console.log('STATISTICS ERROR: ' + xhr.status + ':' + xhr.statusText);
                    $.mobile.loading('hide');
                }
            });
        }
    };

};