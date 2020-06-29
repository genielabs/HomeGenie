import { Component, OnInit, Input } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { HguiService, CMD } from 'src/app/services/hgui/hgui.service';
import { HomegenieAdapter } from '../homegenie-adapter';
import { Adapter } from '../../adapter';

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
  
  constructor(private hgui: HguiService, private _formBuilder: FormBuilder) {}

  ngOnInit() {
    this.firstFormGroup = this._formBuilder.group({
      firstCtrl: ['', Validators.required]
    });
    this.secondFormGroup = this._formBuilder.group({
      secondCtrl: ['', Validators.required]
    });
    if (this.adapter) {
      this.getInterfaceList();
    }
  }

  getInterfaceList() {
    this.adapter.control(null, CMD.Drivers.List, {}).subscribe((res) => {
      console.log('Drivers', res, this.hgui);
      this.drivers = res;
    });
  }

}
