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
                  item.series.label + " at " + new Date(item.datapoint[0] + offset).toLocaleTimeString() + " = " + item.datapoint[1].toFixed(4));

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
    this.Widget.find('[data-ui-field=kwcounter]').html((wattCounter / 1000).toFixed(4) + ' kW');
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
      url: '/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.HomeGenie/Config/Modules.StatisticsGet/HomeAutomation.EnergyMonitor/1/EnergyMonitor.KwCounter',
      type: 'GET',
      dataType: 'json',
      success: function (counterData) {

        var dataSerie = [];
        var yMin = 0;
        if (counterData.History.length > 0)
          yMin = counterData.History[counterData.History.length-1].Value;
        
        for (var x = 0; x < counterData.History.length; x+=Math.ceil(counterData.History.length/48)) {
          dataSerie.push([counterData.History[x].UnixTimestamp, counterData.History[x].Value]);
          if (yMin > counterData.History[x].Value)
            yMin = counterData.History[x].Value;
        }

        try {
          $.plot(_this.Widget.find('[data-ui-field=energystats]'), [
            { label: 'kW counter&nbsp;', data: dataSerie, lines: { show: true, lineWidth: 2.0 }, bars: { show: false }, splines: { show: false }, points: { show: true } }
          ],
                 {
            yaxis: { 
                show: true, min: yMin 
            },
            xaxis: { mode: "time", timeformat: "%h%p", minTickSize: [2, "hour"], tickSize: [2, "hour"] },
            legend: { position: "nw", noColumns: 6, backgroundColor: 'black' },
            lines: { 
              show: showlines, lineWidth: 1.0,
              fill: true,
              fillColor: { colors: [ { opacity: 0.0 }, { opacity: 1.0 } ] }
            },
            series: {
              splines: { show: showsplines }
            },
            grid: {
              color: 'white',
              backgroundColor: 'rgba(255,255,255, 0.2)',
              hoverable: true
            },
            colors: ["rgba(200, 255, 0, 1.0)"],
            points: { show: false },
            zoom: {
              interactive: false
            },
            pan: {
              interactive: false
            }
          });
        } catch (e) { }

      }
    });    
  }

}]