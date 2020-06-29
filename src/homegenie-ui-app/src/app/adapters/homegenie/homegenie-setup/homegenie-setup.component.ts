import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { HguiService, CMD } from 'src/app/services/hgui/hgui.service';

@Component({
  selector: 'app-homegenie-setup',
  templateUrl: './homegenie-setup.component.html',
  styleUrls: ['./homegenie-setup.component.scss']
})
export class HomegenieSetupComponent implements OnInit {
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
    const adapters = this.hgui.getAdapters();
    const homegenieAdapter = adapters.find((adapter) => adapter.className === 'HomegenieAdapter');
    if (homegenieAdapter) {
      homegenieAdapter.control(null, CMD.Drivers.List, {}).subscribe((res) => {
        console.log('Drivers', res);
        this.drivers = res;
      });
    }
    this.hgui.onAdapterAdded.subscribe((adapter) => {
      if (adapter.className === 'HomegenieAdapter') {
        console.log(adapter.options.config.connection)
        adapter.control(null, CMD.Drivers.List, {}).subscribe((res) => {
          console.log('Drivers', res);
          this.drivers = res;
        });
      }
    });
  }

  enableDriverClicked(driver, e) {
    console.log(e.target, e);
  }

}
