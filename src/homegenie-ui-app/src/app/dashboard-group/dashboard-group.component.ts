import {Component, ElementRef, Input, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {HguiService} from '../services/hgui/hgui.service';
import {ActivatedRoute, Router} from '@angular/router';
import {Subscription} from 'rxjs';
import {Group} from '../services/hgui/group';
import {MatDialog} from '@angular/material/dialog';
import {WidgetOptionsDialogComponent} from '../widgets/common/dialogs/widget-options-dialog/widget-options-dialog.component';
import {DragDrop, DragRef} from "@angular/cdk/drag-drop";

@Component({
  selector: 'app-dashboard-group',
  templateUrl: './dashboard-group.component.html',
  styleUrls: ['./dashboard-group.component.scss']
})
export class DashboardGroupComponent implements OnInit, OnDestroy {
  @ViewChild('container', {static: false}) dashboardContainer: ElementRef<HTMLElement>;
  @Input()
  group: Group = null;

  private readonly routeParamSubscription: Subscription;
  private layoutArrangeRequest = null;
  private dragElements: DragRef[] = [];

  constructor(
    public dialog: MatDialog,
    public hgui: HguiService,
    private dragDrop: DragDrop,
    activatedRoute: ActivatedRoute,
    router: Router
  ) {
    this.routeParamSubscription = activatedRoute.params.subscribe(params => {
      const name = params['name'];
      if (name) {
        this.group = hgui.getGroup(name);
      }
      if (this.group == null) {
        this.group = hgui.groups[0];
        if (this.group) {
          router.navigate(['/groups', this.group.name]);
          return;
        }
      }
      setTimeout(() => {
        this.hgui.setCurrentGroup(this.hgui.groups.indexOf(this.group));
      }, 500);
    });
  }

  ngOnInit(): void {
    console.log(this.dashboardContainer);
    this.layoutArrange();
  }
  ngOnDestroy(): void {
    if (this.routeParamSubscription) {
      this.routeParamSubscription.unsubscribe();
    }
    this.disableDrag();
  }

  onResize(e) {
    cancelAnimationFrame(this.layoutArrangeRequest);
    this.layoutArrangeRequest = requestAnimationFrame(this.layoutArrange.bind(this));
  }

  enableDrag(): void {
    this.layoutArrange();
    const container = this.dashboardContainer.nativeElement;
    container.childNodes.forEach((item: HTMLElement, i) => {
      if (item['style']) {
        item.firstElementChild.classList.add('drag-no-input');
        const dr = this.dragDrop.createDrag(item);
        // TODO: add more event handlers to drag element (eg. `dr.started.subscribe` ...)
        dr.ended.subscribe((e) => {
          this.layoutArrange();
          e.source.reset();
        });
        this.dragElements.push(dr);
      }
    });
  }
  disableDrag(): void {
    this.dragElements.forEach((dr, i) => {
      dr.getRootElement().firstElementChild.classList.remove('drag-no-input');
      dr.dispose();
    });
    this.dragElements.length = 0;
    this.layoutArrange();
  }

  layoutArrange() {
    if (!this.dashboardContainer) return;
    const centerGap: number[] = [];
    const gap = 6;
    let cx = (gap / 2); let cy = (gap / 2);
    let row = 0;
    let rowHeight = 0;
    const eh = (e) => {
      console.log('AnimationEND', e);
      e.target.classList.remove('transition');
      e.target.removeEventListener('animationend', eh);
    };
    const container = this.dashboardContainer.nativeElement;
    // measure available space for centering rows
    container.childNodes.forEach((item: HTMLElement, i) => {
      if (item['style']) { // TODO: check also for a specific class/attr eg. "auto-arrange"
        if (rowHeight < item.offsetHeight) {
          rowHeight = item.offsetHeight;
        }
        centerGap[row] = container.offsetWidth - cx;
        if (cx > 0 && cx + item.offsetWidth + gap >= container.offsetWidth) {
          cx = (gap / 2);
          cy += rowHeight + (gap / 2);
          rowHeight = item.offsetHeight;
          row++;
        }
        item.classList.add('transition');
        cx += item.offsetWidth + (gap / 2);
      }
    });
    // arrange
    row = 0; cx = (gap / 2); cy = (gap / 2); rowHeight = 0;
    container.childNodes.forEach((item: HTMLElement, i) => {
      if (item['style']) {
        if (rowHeight < item.offsetHeight) {
          rowHeight = item.offsetHeight;
        }
        if (cx > 0 && cx + item.offsetWidth + gap >= container.offsetWidth) {
          cx = (gap / 2);
          cy += rowHeight + (gap / 2);
          rowHeight = item.offsetHeight;
          row++;
        }
        item.style.transform = `translate(${cx + (centerGap[row] / 2)}px,${cy}px)`;
        //item.style.left = cx + (centerGap[row] / 2) + 'px';
        //item.style.top = cy + 'px';
        cx += item.offsetWidth + (gap / 2);
      }
    });
    setTimeout(() => {
      container.childNodes.forEach((item: HTMLElement, i) => {
        if (item['style']) {
          item.classList.remove('transition');
        }
      });
    }, 500);
  }

  onShowOptions(data: any): void {
    // Show Module/Program Options
    this.dialog.open(WidgetOptionsDialogComponent, {
      panelClass: 'dialog-no-padding',
      width: '100%',
      minWidth: '320px',
      maxWidth: data.option === 'statistics' ? '960px' : '576px',
      disableClose: false,
      data
    });
  }
}
