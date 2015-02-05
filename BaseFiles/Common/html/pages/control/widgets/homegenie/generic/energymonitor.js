[{
    Name: "Unknown Module",
    Author: "Generoso Martello",
    Version: "2015-01-25",

    GroupName: '',
    IconImage: 'pages/control/widgets/homegenie/generic/images/power.png',
    StatusText: '',
    Description: '',
    Initialized: false,
    Widget: null,
    LastDrawStats: new Date(0),

    RenderView: function (cuid, module) {
        var container = $(cuid);
        var widget = this.Widget = container.find('[data-ui-field=widget]');
        if (!this.Initialized)
        {
            this.Widget.find('[data-ui-field=energystats]').qtip({
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
            this.Widget.find('[data-ui-field=energystats]').bind("plothover", function (event, pos, item) {
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
        }
        // render widget
        this.DrawData();
        var elapsedMinutes = ((new Date() - this.LastDrawStats) / 1000 / 60);
        if (elapsedMinutes >= 5) {
            this.LastDrawStats = new Date();
            this.DrawStats('');
        }
    },
    
    DrawData: function() {
        var energyMonitor = HG.WebApp.Utility.GetModuleByDomainAddress('HomeAutomation.EnergyMonitor', '1');
        var wattLoad = parseFloat(HG.WebApp.Utility.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.WattLoad').Value.replace(',', '.'));
        var wattCounter = parseFloat(HG.WebApp.Utility.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.WattCounter').Value.replace(',', '.'));
        var lights = HG.WebApp.Utility.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.OperatingLights').Value;
        var switches = HG.WebApp.Utility.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.OperatingSwitches').Value;
        this.Widget.find('[data-ui-field=kwcounter]').html('Counter: ' + (wattCounter / 1000).toFixed(2) + ' kW');
        this.Widget.find('[data-ui-field=wattload]').html(wattLoad.toFixed(2));
        this.Widget.find('[data-ui-field=lightcount]').html(lights);
        this.Widget.find('[data-ui-field=switchcount]').html(switches);
    },
    
    DrawStats: function(mod) {
        var _this = this;
        var showsplines = false;
        var showlines = false;
        var showbars = true;
        $.ajax({
            url: '/' + HG.WebApp.Data.ServiceKey + '/' + HG.WebApp.Data.ServiceDomain + '/Statistics/Parameter.StatsHour/Meter.Watts/' + mod + '/0/' + new Date().getTime(),
            type: 'GET',
            success: function (data) {
                var stats = eval(data);
                try {
                    $.plot(_this.Widget.find('[data-ui-field=energystats]'), [
                            { label: 'Max', data: stats[1], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                            { label: 'Avg', data: stats[2], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                            { label: 'Min', data: stats[0], bars: { show: showbars, barWidth: (30 * 60 * 1000), align: 'center', steps: true } },
                            { label: 'Today Avg', data: stats[3], bars: { show: showbars, barWidth: (10 * 60 * 1000), align: 'center', steps: false } },
                            { label: 'Today Detail', data: stats[4], lines: { show: true, lineWidth: 2.0 }, bars: { show: false }, splines: { show: false }, points: { show: false } }
                        ],
                        {
                            yaxis: { show: false },
                            xaxis: { mode: "time", timeformat: "%H", minTickSize: [2, "hour"], tickSize: [2, "hour"] },
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
                            points: { show: false },
                            zoom: {
                                interactive: true
                            },
                            pan: {
                                interactive: true
                            }                            
                        });
                } catch (e) { }

            }
        });    
    }

}]