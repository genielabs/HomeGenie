import {
  Component, ElementRef,
  HostListener,
  OnDestroy,
  OnInit, ViewChild
} from '@angular/core';

import {Color, Label} from "ng2-charts";

import {CMD} from "../../services/hgui/hgui.service";
import {ModuleField} from "../../services/hgui/module";
import {WidgetBase} from "../widget-base";
import {ChartDataSets, ChartOptions} from "chart.js";

export class EnergyMonitorData {
  wattLoad: ModuleField;
  operatingLights: ModuleField;
  operatingAppliances: ModuleField;
  todayCounter: ModuleField;
  totalCounter: ModuleField;
}

@Component({
  selector: 'app-energy-monitor',
  templateUrl: './energy-monitor.component.html',
  styleUrls: ['./energy-monitor.component.scss']
})
export class EnergyMonitorComponent extends WidgetBase implements OnInit, OnDestroy {
  @ViewChild('primary', { static: true }) colorPrimary: ElementRef;
  @ViewChild('accent', { static: true }) colorAccent: ElementRef;
  @ViewChild('warn', { static: true }) colorWarn: ElementRef;

  isLoading = false;

  lineChartData: ChartDataSets[] = [];
  lineChartLabels: Label[] = [];
  lineChartOptions: (ChartOptions & { annotation: any }) = {
    animation: {
      duration: 10
    },
    responsive: true,
    legend: {
      position: "bottom"
    },
    scales: {
      xAxes: [{
        type: 'time',
        ticks: {
          autoSkip: true
        },
        time: {
          unit: 'hour'
        }
      }]
      /*
      // We use this empty structure as a placeholder for dynamic theming.
      xAxes: [{}],
      yAxes: [
        {
          id: 'y-axis-0',
          position: 'left',
        },
        {
          id: 'y-axis-1',
          position: 'right',
          gridLines: {
            color: 'rgba(255,0,0,0.3)',
          },
          ticks: {
            fontColor: 'red',
          }
        }
      ]
       */
    },
    annotation: {
      /*
      annotations: [
        {
          type: 'line',
          mode: 'vertical',
          scaleID: 'x-axis-0',
          value: 'March',
          borderColor: 'orange',
          borderWidth: 2,
          label: {
            enabled: true,
            fontColor: 'orange',
            content: 'LineAnno'
          }
        },
      ],
      */
    },
  };
  lineChartColors: Color[] = [
    { // blue
      backgroundColor: 'rgba(0,77,255,.5)',
      borderColor: 'rgb(29,86,212)',
      pointBackgroundColor: 'rgba(0,77,255,.5)',
      pointBorderColor: '#ffffff22',
      pointHoverBackgroundColor: '#fff',
      pointHoverBorderColor: 'rgba(77,83,96,1)'
    },
    { // red
      backgroundColor: 'rgba(255,0,0,0.3)',
      borderColor: 'rgb(167,39,13)',
      pointBackgroundColor: 'rgba(167,39,13, .5)',
      pointBorderColor: '#ffffff22',
      pointHoverBackgroundColor: '#fff',
      pointHoverBorderColor: 'rgba(148,159,177,0.8)'
    },
    { // green
      backgroundColor: 'rgb(34,78,14)',
      borderColor: 'rgb(15,78,36)',
      pointBackgroundColor: 'rgba(34,78,14,.5)',
      pointBorderColor: '#ffffff22',
      pointHoverBackgroundColor: '#fff',
      pointHoverBorderColor: 'rgba(148,159,177,0.8)'
    }
  ];
  lineChartLegend = false;
  lineChartType = 'line';
  lineChartPlugins = []; //[pluginAnnotations];

  isResizing = false;
  private resizeTimeout = null;

  get data(): EnergyMonitorData {
    return super.data;
  }


  @HostListener('window:resize', ['$event'])
  onResize(event) {
    this.isResizing = true;
    clearTimeout(this.resizeTimeout);
    this.resizeTimeout = setTimeout(() => {
      this.isResizing = false;
    }, 10);
  }


  // chart events
  chartClicked({ event, active }: { event: MouseEvent, active: {}[] }): void {
    console.log(event, active);
  }

  chartHovered({ event, active }: { event: MouseEvent, active: {}[] }): void {
    console.log(event, active);
  }


  get actualLoad(): ModuleField {
    return this.data.wattLoad;
  }
  get lightsCount(): ModuleField {
    return this.data.operatingLights;
  }
  get appliancesCount(): ModuleField {
    return this.data.operatingAppliances;
  }
  get totalCounter(): ModuleField {
    return this.data.totalCounter;
  }
  get todayCounter(): ModuleField {
    return this.data.todayCounter;
  }

  private statsUpdateInterval = setInterval(this.updateStats.bind(this), 30000);

  ngOnInit(): void {

    // get colors from current material theme
    // TODO: possibly move this to a service or utility class
    const primaryColor = getComputedStyle(this.colorPrimary.nativeElement).color;
    const accentColor = getComputedStyle(this.colorAccent.nativeElement).color;
    const warnColor = getComputedStyle(this.colorWarn.nativeElement).color;
    // set chart colors using material theme palette
    this.lineChartColors[0].backgroundColor =
      this.lineChartColors[0].pointBackgroundColor = primaryColor.replace(')', ', 0.1)');
    this.lineChartColors[0].borderColor =
      this.lineChartColors[0].pointBorderColor = primaryColor;
    this.lineChartColors[1].backgroundColor =
      this.lineChartColors[1].pointBackgroundColor = accentColor.replace(')', ', 0.1)');
    this.lineChartColors[1].borderColor =
      this.lineChartColors[1].pointBorderColor = accentColor;
    this.lineChartColors[2].backgroundColor =
      this.lineChartColors[2].pointBackgroundColor = warnColor.replace(')', ', 0.1)');
    this.lineChartColors[2].borderColor =
      this.lineChartColors[2].pointBorderColor = warnColor;

    this.updateStats();
  }
  ngOnDestroy() {
    if (this.statsUpdateInterval) {
      clearInterval(this.statsUpdateInterval);
    }
  }

  private updateStats(): void {
    if (!this.module) return;
    this.module.control(CMD.Statistics.Field.Get, 'EnergyMonitor.WattLoad')
      .subscribe((res) => {
        if (res.response.History) {
          this.lineChartData = [];
          const timeRangeStart = 24 * 60 * 60 * 1000;
          const now = new Date().getTime();
          const data = res.response.History.filter((stat) => now - stat.UnixTimestamp <= timeRangeStart);
          //data.reverse();
          const fieldStats: ChartDataSets = {};
          //fieldStats.lineTension = 0;
          //fieldStats.label = labels[this.lineChartData.length];
          fieldStats.data = data.map((stat) => ({
            // TODO: the HG 'UnixTimestamp' field should be mapped to HGUI module's 'timestamp' field
            x: stat.UnixTimestamp,
            y: stat.Value
          }));
          fieldStats.borderWidth = 1;
          fieldStats.pointRadius = 0;
          fieldStats.fill = true;
          //fieldStats.type = "bars";
          fieldStats.lineTension = 0;
          fieldStats.spanGaps = true;
          this.lineChartData.push(fieldStats);
        }
      });
  }
}
