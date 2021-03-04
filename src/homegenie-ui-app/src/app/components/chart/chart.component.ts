import {Component, ElementRef, HostListener, Input, OnInit, ViewChild} from '@angular/core';
import {BaseChartDirective, Color, Label} from "ng2-charts";
import {ChartDataSets, ChartOptions, ChartPoint} from "chart.js";
import {Module, ModuleField} from "../../services/hgui/module";
import {CMD, HguiService} from "../../services/hgui/hgui.service";
import {concat, Subject, Subscription} from "rxjs";
//import * as pluginAnnotations from 'chartjs-plugin-annotation';

export class GraphMode {
  public static COMBINE_FIELDS = 1;
  public static COMPARE_MODULES = 2;
}
export class TimeRange {
  public value: number;
  public label: string;
}

@Component({
  selector: 'app-chart',
  templateUrl: './chart.component.html',
  styleUrls: ['./chart.component.scss']
})
export class ChartComponent implements OnInit {
  @ViewChild('primary', { static: true }) colorPrimary: ElementRef;
  @ViewChild('accent', { static: true }) colorAccent: ElementRef;
  @ViewChild('warn', { static: true }) colorWarn: ElementRef;

  @Input()
  module: Module;

  GraphMode = GraphMode;

  timeRanges: TimeRange[] = [
    { value: 0.016, label: 'MODULE.stats.last_minute' },
    { value: 0.083, label: 'MODULE.stats.last_ten_minutes' },
    { value: 0.5, label: 'MODULE.stats.last_half_hour' },
    { value: 1, label: 'MODULE.stats.last_hour' },
    { value: 3, label: 'MODULE.stats.last_three_hours' },
    { value: 6, label: 'MODULE.stats.last_six_hours' },
    { value: 12, label: 'MODULE.stats.last_twelve_hours' },
    { value: 24, label: 'MODULE.stats.last_twentyfour_hours' }
  ]

  selectedFields: ModuleField[] = [];
  selectedModules: Module[] = [];
  selectedTimeRange: TimeRange = this.timeRanges[0];
  graphMode: number = GraphMode.COMBINE_FIELDS;

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
          unit: 'minute'
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
      backgroundColor: 'rgb(0,77,255)',
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
  lineChartLegend = true;
  lineChartType = 'line';
  lineChartPlugins = []; //[pluginAnnotations];

  isResizing = false;
  private resizeTimeout = null;
  private moduleEventsSubscription: Subscription;

  @ViewChild(BaseChartDirective, { static: false }) chart: BaseChartDirective;

  constructor(public hgui: HguiService) {}

  get hasStats(): boolean {
    return this.lineChartLabels.length > 0;
  }
  get statsFields(): ModuleField[] {
    return this.module.fields.filter((f) =>
      f.key.toLowerCase().startsWith('meter.')
      || f.key.toLowerCase().startsWith('sensor.')
      || f.key.toLowerCase().startsWith('statistics.')
      || f.key.toLowerCase().startsWith('status.')
      || f.key.toLowerCase().startsWith('energymonitor.')
    );
  }
  get comparableModules(): Module[] {
    // TODO: ... filter modules
    return this.selectedFields.length > 0
      ? this.hgui.modules.filter((m) => m.field(this.selectedFields[0].key) && m !== this.module)
      : [];
  }

  @HostListener('window:resize', ['$event'])
  onResize(event) {
    this.isResizing = true;
    clearTimeout(this.resizeTimeout);
    this.resizeTimeout = setTimeout(() => {
      this.isResizing = false;
    }, 10);
  }

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

    if (this.module) {
      let defaultField;
      if (this.statsFields.length > 0) {
        defaultField = this.statsFields[0];
        this.selectedFields.push(defaultField);
      }
      this.showStats();
    }

    // real-time chart update
    this.moduleEventsSubscription = this.hgui.onModuleEvent.subscribe((e) => {
      if (e.module === this.module || this.selectedModules.indexOf(e.module) >= 0) {
        const field = this.selectedFields.find((f) => f.key === e.event.key);
        if (field) {
          let statIndex = 0;
          if (this.graphMode === GraphMode.COMBINE_FIELDS) {
            statIndex = this.selectedFields.indexOf(field);
          } else {
            if (e.module !== this.module) {
              statIndex = this.selectedModules.indexOf(e.module) + 1;
            }
          }
          const ds = this.lineChartData[statIndex];
          if (ds && this.chart) {
            const now = new Date().getTime();
            const timeRangeStart = this.selectedTimeRange.value * 60 * 60 * 1000;
            // update chart
            const ds = this.chart.datasets[statIndex];
            // prepend new data
            const data: ChartPoint[] = [{
              x: e.event.timestamp,
              y: e.event.value
            }];
            // delete old data out of the selected time range
            ds.data.forEach((p, i) => {
              if (now - p.x <= timeRangeStart) {
                data.push(p);
              }
            });
            ds.data = data;
            if (ds.data.length > 0) {
              // update chart axis
              const timeAxis = this.chart.chart.config.options.scales.xAxes[0].ticks;
              timeAxis.min = (ds.data[ds.data.length - 1] as ChartPoint).x as string;
              timeAxis.max = e.event.timestamp;
            }
          }
        }
      }
    });

  }
  ngOnDestroy(): void {
    this.moduleEventsSubscription && this.moduleEventsSubscription.unsubscribe();
  }

  onFieldChange(e): void {
    this.selectedFields = this.graphMode === GraphMode.COMBINE_FIELDS ? e.value : [ e.value ];
    this.showStats();
  }
  onCompareModulesChange(e): void {
    this.selectedModules = e.value;
    this.showStats();
  }
  onGraphModeChanged(e): void {
    this.graphMode = e.value;
    if (this.graphMode === GraphMode.COMPARE_MODULES && this.selectedFields.length > 1) {
      this.selectedFields = [ this.selectedFields[0] ];
    }
    this.showStats();
  }
  onTimeRangeChange(e): void {
    this.selectedTimeRange = e.value;
    this.showStats();
  }

  // chart events
  chartClicked({ event, active }: { event: MouseEvent, active: {}[] }): void {
    console.log(event, active);
  }

  chartHovered({ event, active }: { event: MouseEvent, active: {}[] }): void {
    console.log(event, active);
  }

  private showStats(): void {
    // get charts data
    const reqs: Subject<any>[] = [];
    const labels: string[] = [];
    this.selectedFields.forEach((f) => {
      reqs.push(this.module.control(CMD.Statistics.Field.Get, f.key));
      labels.push(this.graphMode === GraphMode.COMPARE_MODULES ? this.module.name : f.key);
    });
    if (this.graphMode === GraphMode.COMPARE_MODULES) {
      this.selectedModules.forEach((m) => {
        this.selectedFields.forEach((f) => {
          reqs.push(m.control(CMD.Statistics.Field.Get, f.key));
          labels.push(m.name);
        });
      });
    }
    this.lineChartData = [];
    concat(...reqs).subscribe((res: any) => {
      if (res.response.History) {
        const timeRangeStart = this.selectedTimeRange.value * 60 * 60 * 1000;
        const now = new Date().getTime();
        const data = res.response.History.filter((stat) => now - stat.UnixTimestamp <= timeRangeStart);
        //data.reverse();
        const fieldStats: ChartDataSets = {};
        //fieldStats.lineTension = 0;
        fieldStats.label = labels[this.lineChartData.length];
        fieldStats.data = data.map((stat) => ({
          // TODO: the HG 'UnixTimestamp' field should be mapped to HGUI module's 'timestamp' field
          x: stat.UnixTimestamp,
          y: stat.Value
        }));
        fieldStats.borderWidth = 2;
        fieldStats.pointRadius = 1;
        fieldStats.fill = false;
        this.lineChartData.push(fieldStats);
      }
    }, undefined, () => {
      if (this.lineChartData.length > 0) {
        this.lineChartLabels = this.getChartLabels();
        this.lineChartData = this.lineChartData.slice();
        this.lineChartColors = this.lineChartColors.slice();
        // update chart axis
        if (this.chart) {
          this.lineChartData.map((ds) => {
            if (ds.data.length > 0) {
              const timeAxis = this.chart.chart.config.options.scales.xAxes[0].ticks;
              const x = (ds.data[ds.data.length - 1] as ChartPoint).x;
              timeAxis.min = x as string;
            }
          });
        }
      }
    });
  }

  private getChartLabels(): Label[] {
    const labels: Label[] = [];
    this.lineChartData.forEach((cd) => {
      (cd.data as ChartPoint[]).forEach((d) => {
        labels.push(d.x as Label);
      });
    });
    labels.sort((a, b) => { return a < b ? -1 : a === b ? 0 : 1; });
    return labels;
  }
}
