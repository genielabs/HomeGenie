import { Component, OnInit, Input } from '@angular/core';
import { HomegenieAdapter } from '../homegenie-adapter';
import { HguiService } from 'src/app/services/hgui/hgui.service';
import { FormBuilder } from '@angular/forms';

@Component({
  selector: 'app-x10-setup-form',
  templateUrl: './x10-setup-form.component.html',
  styleUrls: ['./x10-setup-form.component.scss']
})
export class X10SetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  constructor(private hgui: HguiService, private _formBuilder: FormBuilder) {}

  ngOnInit(): void {
    // TODO: get X10 options
    console.log('X10 options', this.adapter);
  }
}
