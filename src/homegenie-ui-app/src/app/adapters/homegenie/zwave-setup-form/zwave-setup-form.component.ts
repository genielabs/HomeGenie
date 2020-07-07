import {Component, Input, OnInit} from '@angular/core';
import {HomegenieAdapter, ResponseCode} from '../homegenie-adapter';
import {MatDialog} from '@angular/material/dialog';
import {ZwaveManagerDialogComponent} from '../../../components/zwave/zwave-manager-dialog/zwave-manager-dialog.component';
import {HomegenieApi} from '../homegenie-api';
import {HomegenieZwaveApi} from '../homegenie-zwave-api';

@Component({
  selector: 'app-zwave-setup-form',
  templateUrl: './zwave-setup-form.component.html',
  styleUrls: ['./zwave-setup-form.component.scss']
})
export class ZwaveSetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  serialPorts: { value: string, content: string }[] = [];

  portName = '/dev/ttyUSB0';

  constructor(
    public dialog: MatDialog
  ) {
  }

  ngOnInit(): void {
    this.adapter.apiCall(HomegenieApi.Config.Interfaces.Configure.Hardware.SerialPorts)
      .subscribe((res) => {
        if (res.code === ResponseCode.Success) {
          this.serialPorts = res.response;
        } else {
          // TODO: ... log error
        }
      });
    this.adapter.apiCall(HomegenieZwaveApi.Options.Get.Port).subscribe((res) => {
      if (res.code === ResponseCode.Success) {
        this.portName = res.response.ResponseValue;
      } else {
        // TODO: ... log error
      }
    });
  }
  onPortChange(e): void {
    const command = HomegenieZwaveApi.Options.Set.Port
      .replace('{{portName}}', encodeURIComponent(this.portName));
    this.adapter.apiCall(command)
      .subscribe((res) => {
        if (res.code === ResponseCode.Success) {
          console.log('ZWave Set Port', this.portName, res);
        } else {
          // TODO: ... log error
        }
      });
  }
  onDeviceManagerButtonClick(e): void {
    const dialogRef = this.dialog.open(ZwaveManagerDialogComponent, {
      // height: '400px',
      // width: '600px',
      maxWidth: '800px',
      disableClose: true,
      data: this.adapter
    });
    dialogRef.afterClosed().subscribe(() => {
      this.adapter.hgui.saveConfiguration();
    });
  }
}
