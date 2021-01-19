import {Component, Input, OnDestroy, OnInit} from '@angular/core';
import {CMD, HguiService} from '../services/hgui/hgui.service';
import {ActivatedRoute, Router} from '@angular/router';
import {Subscription} from 'rxjs';
import {Group} from '../services/hgui/group';
import {MatDialog} from '@angular/material/dialog';
import {WidgetOptionsDialogComponent} from '../widgets/common/dialogs/widget-options-dialog/widget-options-dialog.component';
import {Module, ModuleType} from '../services/hgui/module';
import {ProgramOptionsDialogComponent} from "../widgets/common/dialogs/program-options-dialog/program-options-dialog.component";

@Component({
  selector: 'app-dashboard-group',
  templateUrl: './dashboard-group.component.html',
  styleUrls: ['./dashboard-group.component.scss']
})
export class DashboardGroupComponent implements OnInit, OnDestroy {
  @Input()
  group: Group = null;

  private readonly routeParamSubscription: Subscription;

  constructor(public dialog: MatDialog, public hgui: HguiService, activatedRoute: ActivatedRoute, router: Router) {
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
  }
  ngOnDestroy(): void {
    if (this.routeParamSubscription) {
      this.routeParamSubscription.unsubscribe();
    }
  }

  onShowOptions(module: Module): void {
    // Program Options
    if (module.type === ModuleType.Program) {
      const dialogRef = this.dialog.open(ProgramOptionsDialogComponent, {
        // height: '400px',
        width: '100%',
        minWidth: '320px',
        maxWidth: '420px',
        disableClose: false,
        data: module
      });
      dialogRef.afterClosed().subscribe((changeList) => {
        if (changeList) {
          const changes: any = {};
          changeList.forEach((c) => {
            changes[c.field.key] = c.value;
          });
          // TODO:
          module.control(CMD.Options.Set, changes).subscribe((res) => {
            // TODO: ... logging
          });
        }
      });
      return;
    }
    // Module Options
    const dialogRef = this.dialog.open(WidgetOptionsDialogComponent, {
      // height: '400px',
      width: '100%',
      minWidth: '320px',
      maxWidth: '800px',
      disableClose: false,
      data: module
    });
    dialogRef.afterClosed().subscribe((changeList) => {
      if (changeList) {
        const changes: any = {};
        changeList.forEach((c) => {
          changes[c.field.key] = c.value;
        });
        module.control(CMD.Options.Set, changes).subscribe((res) => {
          // TODO: ... logging
        });
      }
    });
  }
}
