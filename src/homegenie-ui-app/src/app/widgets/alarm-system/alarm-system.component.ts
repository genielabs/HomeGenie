import { Component, OnInit } from '@angular/core';
import {WidgetBase} from "../widget-base";

@Component({
  selector: 'app-alarm-system',
  templateUrl: './alarm-system.component.html',
  styleUrls: ['./alarm-system.component.scss']
})
export class AlarmSystemComponent extends WidgetBase implements OnInit {

  ngOnInit(): void {
  }

}
