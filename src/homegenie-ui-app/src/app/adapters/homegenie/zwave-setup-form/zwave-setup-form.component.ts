import { Component, OnInit, Input } from '@angular/core';
import { HguiService } from 'src/app/services/hgui/hgui.service';
import { FormBuilder } from '@angular/forms';
import { HomegenieAdapter } from '../homegenie-adapter';

@Component({
  selector: 'app-zwave-setup-form',
  templateUrl: './zwave-setup-form.component.html',
  styleUrls: ['./zwave-setup-form.component.scss']
})
export class ZwaveSetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  serialPorts: { value: string, content: string }[] = [];

  constructor(private hgui: HguiService, private _formBuilder: FormBuilder) {}

  ngOnInit(): void {
    // TODO: get Z-Wave options
    console.log('Z-Wave options', this.adapter);
    this.adapter.apiCall('HomeAutomation.HomeGenie/Config/Interfaces.Configure/Hardware.SerialPorts')
      .subscribe((res) => {
        //this.serialPorts = res.response.map((portName: string) => ({ value: portName, content: portName }));
        this.serialPorts = res.response;
        console.log('!!!!!!!!!!!', this.serialPorts);
      });
  }
}
