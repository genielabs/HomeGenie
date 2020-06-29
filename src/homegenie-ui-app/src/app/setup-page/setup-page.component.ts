import { Component, OnInit } from '@angular/core';
import { HguiService } from '../services/hgui/hgui.service';

@Component({
  selector: 'app-setup-page',
  templateUrl: './setup-page.component.html',
  styleUrls: ['./setup-page.component.scss']
})
export class SetupPageComponent implements OnInit {

  constructor(public hgui: HguiService) { }

  ngOnInit(): void {
  }

}
