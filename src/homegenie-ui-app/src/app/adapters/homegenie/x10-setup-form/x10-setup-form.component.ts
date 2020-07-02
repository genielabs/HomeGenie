import {Component, Input, OnInit} from '@angular/core';
import {HomegenieAdapter} from '../homegenie-adapter';

@Component({
  selector: 'app-x10-setup-form',
  templateUrl: './x10-setup-form.component.html',
  styleUrls: ['./x10-setup-form.component.scss']
})
export class X10SetupFormComponent implements OnInit {
  @Input()
  adapter: HomegenieAdapter;

  constructor() {
  }

  ngOnInit(): void {
    // TODO: get X10 options
    console.log('X10 options', this.adapter);
  }
}
