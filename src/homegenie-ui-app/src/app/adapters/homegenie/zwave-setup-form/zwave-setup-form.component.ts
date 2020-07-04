import {Component, Input, OnInit} from '@angular/core';
import {HomegenieAdapter} from '../homegenie-adapter';
import {MatDialog} from '@angular/material/dialog';
import {ZwaveManagerDialogComponent} from '../../../components/zwave/zwave-manager-dialog/zwave-manager-dialog.component';
import {HomegenieApi} from '../homegenie-api';

@Component({
  selector: 'app-zwave-setup-form',
  templateUrl: './zwave-setup-form.component.html',
  styleUrls: ['./zwave-setup-form.component.scss']
})
export class ZwaveSetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  serialPorts: { value: string, content: string }[] = [];

  constructor(
    public dialog: MatDialog
  ) {
  }

  ngOnInit(): void {
    // TODO: get Z-Wave options
    console.log('Z-Wave options', this.adapter);
    this.adapter.apiCall(HomegenieApi.Config.Interfaces.Configure.Hardware.SerialPorts)
      .subscribe((res) => {
        this.serialPorts = res.response;
      });
  }

  onSynchronizeButtonClick(e): void {
    this.dialog.open(ZwaveManagerDialogComponent, {
      // height: '400px',
      // width: '600px',
      disableClose: true,
      data: this.adapter
    });
  }
}
