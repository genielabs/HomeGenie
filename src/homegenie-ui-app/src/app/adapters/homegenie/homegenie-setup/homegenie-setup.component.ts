import {Component, Input, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {CMD, HguiService} from 'src/app/services/hgui/hgui.service';
import {Group, HomegenieAdapter, Program} from '../homegenie-adapter';
import {HomegenieApi, ModuleParameter} from '../homegenie-api';
import {ProgramOptionsDialogComponent} from "../../../widgets/common/program-options-dialog/program-options-dialog.component";
import {MatDialog} from "@angular/material/dialog";

class ProgramsGroup {
  public name: string;
  public programs: Program[] = [];
}

@Component({
  selector: 'app-homegenie-setup',
  templateUrl: './homegenie-setup.component.html',
  styleUrls: ['./homegenie-setup.component.scss']
})
export class HomegenieSetupComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;
  firstFormGroup: FormGroup;
  secondFormGroup: FormGroup;

  drivers: any[] = [];
  programsGroups: ProgramsGroup[] = [];

  constructor(public hgui: HguiService, private formBuilder: FormBuilder, public dialog: MatDialog) {
  }

  ngOnInit(): void {
    this.firstFormGroup = this.formBuilder.group({
      firstCtrl: ['', Validators.required]
    });
    this.secondFormGroup = this.formBuilder.group({
      secondCtrl: ['', Validators.required]
    });
    if (this.adapter) {
      this.getInterfaceList();
      this.getProgramList();
    }
  }

  onDriverEnabledChange(e: any, driver: any): void {
    this.adapter.apiCall(e.checked
      ? HomegenieApi.Config.Interfaces.Enable(driver.Domain)
      : HomegenieApi.Config.Interfaces.Disable(driver.Domain)
    ).subscribe((res) => {
      console.log(res);
    });
  }

  getInterfaceList(): void {
    this.adapter.apiCall(HomegenieApi.Config.Interfaces.List)
      .subscribe((res) => {
        this.drivers = res.response;
      });
  }

  getProgramList(): void {
    // build groups and programs list
    this.adapter.apiCall(`${HomegenieApi.Config.Groups.List}/Automation`)
      .subscribe((res) => {
        this.programsGroups = res.response.map((g) => {
          return { name: g.Name, programs: [] } as ProgramsGroup;
        });
        // get programs list
        this.adapter.apiCall(HomegenieApi.Automation.Programs.List)
          .subscribe((res) => {
            // keep only programs that are running and that have configuration options
            const programs = res.response.filter((p) => {
              let hasOptions = false;
              const module = this.adapter.getModule(`${p.Domain}/${p.Address}`);
              if (module) {
                hasOptions = module.Properties
                  .filter((p: ModuleParameter) => p.Name.startsWith('ConfigureOptions.'))
                  .length > 0;
              }
              return p.IsRunning && hasOptions ? p : undefined;
            });
            // populate groups with respective programs
            this.programsGroups.map((pg) => {
              pg.programs = programs.filter((p) => p.Group === pg.name);
            });
            // filter out groups without programs
            this.programsGroups = this.programsGroups.filter((pg) => pg.programs.length > 0);
          });
      });
  }

  onProgramSelected(program: any) {
    const module = this.hgui.getModule(`${program.Domain}/${program.Address}`, this.adapter.id);
    if (!module) {
      console.log('WARNING', 'No module associated with this program.');
      return;
    }
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
  }
}
