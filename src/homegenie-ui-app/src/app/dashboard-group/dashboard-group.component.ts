import {Component, Input, OnDestroy, OnInit} from '@angular/core';
import {HguiService} from '../services/hgui/hgui.service';
import {ActivatedRoute} from '@angular/router';
import {Observable, Subscription} from 'rxjs';
import {map} from 'rxjs/operators';
import {Group} from '../services/hgui/group';

@Component({
  selector: 'app-dashboard-group',
  templateUrl: './dashboard-group.component.html',
  styleUrls: ['./dashboard-group.component.scss']
})
export class DashboardGroupComponent implements OnInit, OnDestroy {
  @Input()
  group: Group;

  private routeParamSubscription: Subscription;

  constructor(public hgui: HguiService, route: ActivatedRoute) {
    this.routeParamSubscription = route.queryParams.subscribe(params => {
      const name = params['group'];
      if (name) {
        this.group = hgui.getGroup(name);
      } else {
        this.group = hgui.groups[0];
      }
      hgui.setCurrentGroup(hgui.groups.indexOf(this.group));
    });
  }

  ngOnInit(): void {
  }
  ngOnDestroy(): void {
    if (this.routeParamSubscription) {
      this.routeParamSubscription.unsubscribe();
    }
  }
}
