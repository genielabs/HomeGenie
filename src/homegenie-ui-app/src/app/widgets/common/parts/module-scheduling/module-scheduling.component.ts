import {Component, Input, OnDestroy, OnInit} from '@angular/core';
import {CMD} from "../../../../services/hgui/hgui.service";
import {Schedule} from "../../../../services/hgui/automation";
import {DynamicOptionsBase} from "../dynamic-options-base.component";

@Component({
  selector: 'app-module-scheduling',
  templateUrl: './module-scheduling.component.html',
  styleUrls: ['./module-scheduling.component.scss']
})
export class ModuleSchedulingComponent extends DynamicOptionsBase implements OnInit, OnDestroy {
  schedulerItems = [] as Schedule[];
  activeItems = [] as { oldValue: boolean, newValue: boolean }[];
  occurrences = [] as any;

  schedulingRefreshTimeout = null;

  get isChanged(): boolean {
    return this.activeItems.filter((a) => a.oldValue !== a.newValue).length > 0;
  }
  applyChanges() {
    // TODO: ...
    console.log('// TODO: apply changes');
    /*
    console.log('ModuleSchedulingComponent::applyChanges', this.changes);
    if (this.changes.length > 0) {
      const changes: any = {};
      this.changes.forEach((c) => {
        changes[c.field.key] = c.value;
      });
      this.module.control(CMD.Options.Set, changes).subscribe((res) => {
        console.log('ModuleSchedulingComponent::applyChanges DONE');
      });
    }
    */
  }


  getOccurrences(s: Schedule): any {
    const occurrences = [];
    const matching = this.occurrences.find((o) => s.name === o.name);
    if (matching) {
      const today = new Date();
      today.setHours(0,0,0,0);
      //occurrences.push(...matching.occurs);
      matching.occurs.map((o) => {
        occurrences.push({
          x: (o.from - today.getTime()) / (24*60*60000),
          width: (o.to - o.from) / (24*60*60000)
        })
      });
    }
    return occurrences;
  }

  private refreshSchedulePreview(): void {

    // get today's complete scheduling
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    this.module.getAdapter().system(CMD.Automation.Scheduling.ListOccurrences, {
      hourSpan: 24, startTimestamp: today.getTime()
    }).subscribe((res) => {
//      console.log(res);

      const schedules = res;
      const d = new Date();
      d.setSeconds(0);
      const occurrences = [];
      let currentGroup = '';
      schedules.map((v) => {
        let n = v.Name;
        if (n.indexOf('.') > 0) {
          var scheduleGroup = n.substring(0, n.indexOf('.'));
          if (scheduleGroup != currentGroup) {
            occurrences.push({title: scheduleGroup.replace(/\./g, ' / '), separator: true});
            currentGroup = scheduleGroup;
          }
          n = n.substring(n.indexOf('.') + 1);
        }
        var entry = {name: v.Name, title: n, occurs: []} as any;
        /*
        this.schedulerItems.map((s){
          if (s.name == v.Name) {
            entry.index = sk;
            entry.description = sv.Description;
            entry.boundModules = sv.BoundModules;
            entry.hasScript = (typeof sv.Script != 'undefined' && sv.Script != null && sv.Script.trim() != '');
            entry.requiresBoundModules = sv.BoundModules && (sv.Script && sv.Script.replace(/\s+|\n|\r$/g, '').indexOf('$$.boundModules.') > 0);
            entry.prevOccurrence = 0;
            entry.nextOccurrence = 0;
          }
        });
        */
        let prev = 0, start = 0, end = 0;
        v.Occurrences.map((vv) => {
          if (prev == 0) prev = start = end = vv;
          if (vv - prev > 60000) {
            entry.occurs.push({from: start, to: end});
            prev = start = vv;
          } else {
            prev = vv;
          }
          end = vv;
          if (entry.prevOccurrence < vv && vv <= d.getTime())
            entry.prevOccurrence = vv;
          if (entry.nextOccurrence == 0 && vv > d.getTime())
            entry.nextOccurrence = vv;
        });
        entry.occurs.push({from: start, to: end});
        occurrences.push(entry);
      });

      this.occurrences = occurrences;


    });

  }

  checkBoxChange(e, i) {
    console.log(e, i);
    this.activeItems[i].newValue = e.checked;
  }

  startCheckSchedules(): void {
    clearTimeout(this.schedulingRefreshTimeout);
    this.refreshSchedulePreview();
    this.schedulingRefreshTimeout = setTimeout(this.refreshSchedulePreview.bind(this), 60000);
  }
  stopCheckSchedules(): void {
    clearTimeout(this.schedulingRefreshTimeout);
  }

  ngOnDestroy(): void {
    this.stopCheckSchedules();
  }

  ngOnInit(): void {
    const adapter = this.module.getAdapter();
    // get scheduling list
    adapter.system(CMD.Automation.Scheduling.List, {
      type: this.module.type // filter by module type
    }).subscribe((res: Schedule[]) => {
        this.schedulerItems = res;
        // build active schedules list
        this.schedulerItems.forEach((s, i) => {
          const isIncluded = s.boundModules.indexOf(this.module) >= 0;
          this.activeItems[i] = { oldValue: isIncluded, newValue: isIncluded };
        })
      });
    this.startCheckSchedules();
    return;




    // TODO: reuse the code below for the scheduler configuration page

    // get today's complete scheduling
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    adapter.system(CMD.Automation.Scheduling.ListOccurrences, {
      hourSpan: 24, startTimestamp: today.getTime()
    }).subscribe((res) => {
//      console.log(res);

      const schedules = res;
      const d = new Date();
      d.setSeconds(0);
      const occurrences = [];
      let currentGroup = '';
      schedules.map((v) => {
        let n = v.Name;
        if (n.indexOf('.') > 0) {
          var scheduleGroup = n.substring(0, n.indexOf('.'));
          if (scheduleGroup != currentGroup) {
            occurrences.push({title: scheduleGroup.replace(/\./g, ' / '), separator: true});
            currentGroup = scheduleGroup;
          }
          n = n.substring(n.indexOf('.') + 1);
        }
        var entry = {name: v.Name, title: n, occurs: []} as any;
        /*
        this.schedulerItems.map((s){
          if (s.name == v.Name) {
            entry.index = sk;
            entry.description = sv.Description;
            entry.boundModules = sv.BoundModules;
            entry.hasScript = (typeof sv.Script != 'undefined' && sv.Script != null && sv.Script.trim() != '');
            entry.requiresBoundModules = sv.BoundModules && (sv.Script && sv.Script.replace(/\s+|\n|\r$/g, '').indexOf('$$.boundModules.') > 0);
            entry.prevOccurrence = 0;
            entry.nextOccurrence = 0;
          }
        });
        */
        let prev = 0, start = 0, end = 0;
        v.Occurrences.map((vv) => {
          if (prev == 0) prev = start = end = vv;
          if (vv - prev > 60000) {
            entry.occurs.push({from: start, to: end});
            prev = start = vv;
          } else {
            prev = vv;
          }
          end = vv;
          if (entry.prevOccurrence < vv && vv <= d.getTime())
            entry.prevOccurrence = vv;
          if (entry.nextOccurrence == 0 && vv > d.getTime())
            entry.nextOccurrence = vv;
        });
        entry.occurs.push({from: start, to: end});
        occurrences.push(entry);
      });

      this.occurrences = occurrences;


      return;


      occurrences.map((o) => {
        if (o.separator) {
          //occursList.append('<div style="text-align: left;margin-top:1.5em;width:auto;font-size:20pt;font-weight:bold">'+v.title+'</div>');
          // TODO: draw group separator `o.title`
          return true;
        }
        /*
                var scheduleTitle = $('<div/>');
                scheduleTitle.css('float', 'left');
                scheduleTitle.css('cursor', 'pointer');
                scheduleTitle.css('margin-top', '1.0em');
                scheduleTitle.click(function() {
                  $$._CurrentEventIndex = o.index;
                  $$._CurrentEventName = o.name;
                  $$.EditCurrentItem();
                });
                scheduleTitle.append('<h3 style="text-align:left;margin:0;margin-top:0.5em;line-height:16pt;font-size:16pt;vertical-align:middle"><i class="fa fa-clock-o"></i>&nbsp;&nbsp;<span>'+o.title+'</span></h3>');
                occursList.append(scheduleTitle).append('<br clear="all">');
                if (typeof o.description != 'undefined' && o.description != null && o.description.trim() !== '') {
                  var scheduleDescription = $('<div style="text-align:left;font-size:14pt;margin-bottom: 6px;margin-top:8px;opacity:0.75">' + o.description + '</div>');
                  occursList.append(scheduleDescription);
                }
        */
        if (o.prevOccurrence > 0 || o.nextOccurrence > 0) {
          //var lastNextInfo = $('<div/>');
//          occursList.append(lastNextInfo);
          //lastNextInfo.css('margin-top', '8px');
          //lastNextInfo.css('font-size', '10pt');
          //lastNextInfo.css('width', '100%');
          //lastNextInfo.css('height', '12px');
          if (o.prevOccurrence > 0) {
            // TODO: show last info
            //lastNextInfo.append('<span style="display: block; float: left;margin-left:4px"><strong style="opacity: 0.65;margin-right: 10px">LAST</strong> ' + moment(v.prevOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.prevOccurrence).from(new Date())+')</span>');
          }
          if (o.nextOccurrence > 0) {
            // TODO: show next info
            //lastNextInfo.append('<span style="display: block; float: right;margin-right:4px"> <strong style="opacity: 0.65;margin-right: 10px">NEXT</strong> ' + moment(v.nextOccurrence).format('LT')+'&nbsp;&nbsp;('+moment(v.nextOccurrence).from(new Date())+')</span>');
          }
        }
        /*
                var timeBarDiv = $('<div/>');
                timeBarDiv.addClass('hg-scheduler-table-row');
                var timeBar = Raphael(timeBarDiv[0], w, h*2.5);
                timeBar.rect(0, 0, w, h*2.5).attr({
                  fill: "rgb(90, 90, 90)",
                  stroke: "rgb(0,0,0)",
                  "stroke-width": 1
                });
                occursList.append(timeBarDiv);
        */
        var w = 300;
        var h = 10;
        if (w === 0) return;
        w -= 8; // margin
/*
        o.occurs.map((vv) => {
          const sd = new Date(vv.from);
          const df = sd;
          sd.setHours(0, 0, 0, 0);
          vv.from -= sd.getTime();
          vv.to -= sd.getTime();
          var sx1 = Math.round(vv.from / (1440 * 60000) * w), sx2 = Math.round(vv.to / (1440 * 60000) * w) - sx1;
          // TODO:

          timeBar.rect(sx1-1, 0, sx2+1, h+1).attr({
            fill: "rgba(255, 255, 70, 85)",
            stroke: "rgba(255,255,255, 70)",
            "stroke-width": 1
          });

        });

 */
        /*
        o.occurs.map(range => {
          const d1 = new Date(today);
          d1.setMilliseconds(range.from);
          const d2 = new Date(today);
          d2.setMilliseconds(range.to);
          console.log(
            d1.toLocaleString(),
            d2.toLocaleString()
          );
        });
         */
        /*
                for (var t = 0; t < 24; t++) {
                  timeBar.text(t*(w/24)+(w/48), 18, t.toString()).attr({fill:'white'});
                  timeBar.rect(t*(w/24), (h*1.25-1), (w/24), (h*1.25)+1).attr({
                    stroke: "rgba(255, 255, 255, 0.2)",
                    "stroke-width": 1
                  });
                }

                if (v.requiresBoundModules) {
                  var scheduleModulesDiv = $('<div/>');
                  scheduleModulesDiv.css('margin-top', '10px');
                  scheduleModulesDiv.css('text-align', 'left');
                  scheduleModulesDiv.css('clear', 'both');
                  var modulesButton = $('<a class="ui-btn ui-corner-all ui-btn-icon-left ui-icon-fa-cube ui-btn-inline ui-mini" style="margin:0;margin-right: 0.5em;padding-top:4px;padding-bottom:4px">' + HG.WebApp.Locales.GetLocaleString('cronwizard_section_modules', 'Modules') + '</a>');
                  modulesButton.click(function() {
                    $$._CurrentEventIndex = v.index;
                    $$._CurrentEventName = v.name;
                    $$.EditCurrentItem(true);
                  });
                  scheduleModulesDiv.append(modulesButton);
                  if (v.boundModules.length > 0) {
                    var html = '<div style="margin:8px;margin-left: 24px;">';
                    for (i = 0; i < v.boundModules.length; i++) {
                      html += ' &nbsp;&nbsp;<span style="opacity: 0.65">&bull;</span>&nbsp;&nbsp;';
                      var moduleRef = v.boundModules[i];
                      var module = HG.WebApp.Utility.GetModuleByDomainAddress(moduleRef.Domain, moduleRef.Address);
                      if (module) {
                        html += HG.Ui.GetModuleDisplayName(module, true);
                      } else {
                        html += moduleRef.Domain  + ':' + moduleRef.Address;
                      }
                    }
                    html += '</div>';
                    scheduleModulesDiv.append(html);
                  }
                  occursList.append(scheduleModulesDiv);
                }

                d = new Date(); d.setSeconds(0);
                var isToday = d.toDateString() == $$._CurrentDate.toDateString();
                if (isToday) {
                  d = d.getTime();
                  var d2 = new Date(); d2.setHours(0,0,0,0);
                  d -= d2.getTime();
                  var bw = Math.round(d/(1440*60000)*w);
                  timeBar.rect(bw+2, 1, w-bw-2, (h*2.5)-2).attr({
                    fill: "rgba(0, 0, 0, 0.35)",
                    stroke: "rgba(0,0,0, 70)",
                    "stroke-width": 0.5
                  });
                  timeBar.rect(bw-1, 0, 2, h*2.5).attr({
                    fill: "rgba(80, 255, 80, 0.8)",
                    stroke: "rgba(0,0,0, 70)",
                    "stroke-width": 0.5
                  });
                } else {
                  var d2 = new Date(); d2.setHours(0,0,0,0);
                  if ($$._CurrentDate.getTime() > d2.getTime()) {
                    timeBar.rect(0, 1, w, (h*2.5)-2).attr({
                      fill: "rgba(0, 0, 0, 0.35)",
                      stroke: "rgba(0,0,0, 70)",
                      "stroke-width": 0.5
                    });
                  }
                }

                timeBarDiv.on('mousemove',function(e,d){
                  if ($(e.target).is('rect')) {

                    var md = new Date($$._CurrentDate.getTime());
                    md.setHours(0,0,0,0);
                    md = new Date(md.getTime()+(e.offsetX / w * 1440 * 60000)-60000);
                    $$._CurrentDate = md;

                  }
                });

                // build basic tooltip data
                var desc = '<p align="center"><strong>';
                desc += moment($$._CurrentDate).format('LL');
                desc += '</strong></p>';
                // attach tooltip
                timeBarDiv.qtip({
                  content: {
                    text: desc
                  },
                  show: { delay: 350, solo: true, effect: function(offset) {
                      $(this).slideDown(100);
                    } },
                  hide: { inactive: 10000 },
                  style: {
                    classes: 'qtip-red qtip-shadow qtip-rounded qtip-bootstrap',
                    width: 200,
                    name: 'dark',
                    padding: 0
                  },
                  position: { my: 'bottom center', at: 'top center' }
                });

                timeBar.rect(0, 0, w, h*2.5).attr({
                  stroke: "rgba(255, 255, 255, 0.75)",
                  "stroke-width": 1
                });
        */


      });
    });
  }

  matchesType(filter: string[]) {
    return filter.indexOf(this.module.type) >= 0;
  }
}
