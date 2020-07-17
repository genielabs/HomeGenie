import {Component, Input, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {HguiService} from 'src/app/services/hgui/hgui.service';
import {HomegenieAdapter} from '../homegenie-adapter';
import {HomegenieApi} from '../homegenie-api';

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

  constructor(public hgui: HguiService, private formBuilder: FormBuilder) {
  }

  ngOnInit(): void {
    this.firstFormGroup = this.formBuilder.group({
      firstCtrl: ['', Validators.required]
    });
    this.secondFormGroup = this.formBuilder.group({
      secondCtrl: ['', Validators.required]
    });
    if (this.adapter) {
      this.getInterfaceList();
    }
  }

  getInterfaceList(): void {
    this.adapter.apiCall(HomegenieApi.Config.Interfaces.List)
      .subscribe((res) => {
        this.drivers = res.response;
      });
  }
}
