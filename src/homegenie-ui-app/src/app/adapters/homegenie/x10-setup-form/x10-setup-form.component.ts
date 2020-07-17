import {Component, Input, OnInit} from '@angular/core';
import {HomegenieAdapter, ResponseCode} from '../homegenie-adapter';
import {HomegenieApi} from '../homegenie-api';
import {HomegenieX10Api} from '../homegenie-x10-api';

@Component({
  selector: 'app-x10-setup-form',
  templateUrl: './x10-setup-form.component.html',
  styleUrls: ['./x10-setup-form.component.scss']
})
export class X10SetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  serialPorts: { value: string, content: string }[] = [];

  portName = '/dev/ttyUSB0';
  houseCodes = [ 'A' ];

  houseCodesList = ('ABCDEFGHIJKLMNOP').split('').map((hc) => ({
    code: hc
  }));

  constructor() {
  }

  ngOnInit(): void {
    this.adapter.apiCall(HomegenieApi.Config.Interfaces.Configure.Hardware.SerialPorts)
      .subscribe((res) => {
        this.serialPorts = [
          { value: 'USB', content: 'CM15 (USB)' },
          { value: 'CM19-USB', content: 'CM19 (USB)' }
        ];
        if (res.code === ResponseCode.Success) {
          this.serialPorts = this.serialPorts.concat(res.response.map((p) => {
            return { value: `${p}`, content: `CM11 - ${p}` };
          }));
          // get current port
          this.adapter.apiCall(HomegenieX10Api.Options.Get.Port).subscribe((res2) => {
            if (res2.code === ResponseCode.Success) {
              this.portName = res2.response.ResponseValue;
            } else {
              // TODO: ... log error
            }
          });
          // get current house codes
          this.adapter.apiCall(HomegenieX10Api.Options.Get.HouseCodes).subscribe((res2) => {
            if (res2.code === ResponseCode.Success) {
              this.houseCodes = res2.response.ResponseValue.split(',');
            } else {
              // TODO: ... log error
            }
          });
        } else {
          // TODO: ... log error
        }
      });
    // TODO: get X10 options
    console.log('X10 options', this.adapter);
  }

  onPortChange(e): void {
    const command = HomegenieX10Api.Options.Set.Port
      .replace('{{portName}}', encodeURIComponent(this.portName));
    this.adapter.apiCall(command)
      .subscribe((res) => {
        if (res.code === ResponseCode.Success) {
          console.log('X10 Set Port', this.portName, res);
        } else {
          // TODO: ... log error
        }
      });
  }

  onHouseCodesOpenedChange(opened): void {
    if (opened === false) {
      const command = HomegenieX10Api.Options.Set.HouseCodes
        .replace('{{houseCodes}}', encodeURIComponent(this.houseCodes.join(',')));
      this.adapter.apiCall(command)
        .subscribe((res) => {
          if (res.code === ResponseCode.Success) {
            console.log('X10 Set HouseCodes', this.houseCodes, res);
          } else {
            // TODO: ... log error
          }
        });
    }
  }
}
