// this field is used to provide infos about the widget
$$.widget = {
    name   : 'Energy Monitor Widget',
    version: '1.1',
    author : 'Generoso Martello',
    release: '2016-01-10',
    icon   : 'pages/control/widgets/homegenie/generic/images/power.png'
}

// called after the widget is loaded
$$.onStart = function() {
  $$.field('btn_reset').on('click', function() {
    $$.util.ConfirmPopup('Energy Counter Reset', 'Do you want to reset the energy counter?', function (confirmed) {
      if (confirmed)
        $$.module.command('Counter.Reset');
    });
  });
  $$.field('btn_watt_load').on('click', function(){
    statsField = {
      name: 'WattLoad',
      desc: 'Watt Load'
    }
    lastDrawStats = new Date();
    drawStats('');
  });
  $$.field('kwcounter').on('click', function(){
    statsField = {
      name: 'KwCounter',
      desc: 'kW Load'
    }
    lastDrawStats = new Date();
    drawStats('');
  });
  $$.field('energystats').qtip({
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
  $$.field('energystats').on('plothover', function (event, pos, item) {
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

// called each time the UI needs to be fully updated
$$.onRefresh = function() {
    drawData();
    var elapsedMinutes = ((new Date() - lastDrawStats) / 1000 / 60);
    if (elapsedMinutes >= 5) {
      lastDrawStats = new Date();
      drawStats('');
    }
}

// called each time a parameter of the bound module is updated
$$.onUpdate = function(parameter, value) {
}

// called when the widget is requested to stop/dispose
$$.onStop = function() {
}

// --- Private members / functions

var lastDrawStats = new Date(0);
var statsField = {
  name: 'WattLoad',
  desc: 'Watt Load'
};

function drawData() {
  var energyMonitor = $$.util.GetModuleByDomainAddress('HomeAutomation.EnergyMonitor', '1');
  var wattLoad = parseFloat($$.util.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.WattLoad').Value.replace(',', '.'));
  var lights = $$.util.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.OperatingLights').Value;
  var switches = $$.util.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.OperatingSwitches').Value;
  var wattCounter = parseFloat($$.util.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.WattCounter').Value.replace(',', '.'));
  var wattCounterToday = parseFloat($$.util.GetModulePropertyByName(energyMonitor, 'EnergyMonitor.WattCounter.Today').Value.replace(',', '.'));
  $$.field('wattload').html(wattLoad.toFixed(2));
  $$.field('lightcount').html(lights);
  $$.field('switchcount').html(switches);
  $$.field('kwcounter').html((wattCounter / 1000).toFixed(4) + ' kW');
  $$.field('kwcounter-today').html((wattCounterToday / 1000).toFixed(4) + ' kW');
}

function drawStats(mod) {
  var _this = this;
  var showsplines = false;
  var showlines = false;
  var showbars = true;

  $.ajax({
    url: '/' + HG.WebApp.Data.ServiceKey + '/HomeAutomation.HomeGenie/Config/Modules.StatisticsGet/HomeAutomation.EnergyMonitor/1/EnergyMonitor.'+statsField.name,
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
        var dateFormat = HG.WebApp.Store.get('UI.DateFormat');
        $.plot($$.field('energystats'), [
          { label: statsField.desc+'&nbsp;', data: dataSerie, lines: { show: true, lineWidth: 2.0 }, bars: { show: false }, splines: { show: false }, points: { show: true } }
        ],
               {
          yaxis: { 
            show: true, min: yMin 
          },
          xaxis: { mode: "time", useLocalTime: true, timeformat: (dateFormat == "MDY12" ? "%h%p" : "%h"), minTickSize: [2, "hour"], tickSize: [2, "hour"] },
          legend: { position: "nw", noColumns: 6, backgroundColor: 'rgba(0,0,0,0.4)' },
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
