import { Component, OnInit } from '@angular/core';
import { HguiService } from 'src/app/services/hgui/hgui.service';

@Component({
  selector: 'app-zwave-setup-form',
  templateUrl: './zwave-setup-form.component.html',
  styleUrls: ['./zwave-setup-form.component.scss']
})
export class ZwaveSetupFormComponent implements OnInit {

  constructor(private hgui: HguiService) {}

  ngOnInit(): void {
    // TODO: get Z-Wave options
  }

}
