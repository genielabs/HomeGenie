import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-homegenie-setup',
  templateUrl: './homegenie-setup.component.html',
  styleUrls: ['./homegenie-setup.component.scss']
})
export class HomegenieSetupComponent implements OnInit {
  firstFormGroup: FormGroup;
  secondFormGroup: FormGroup;
  
  constructor(private _formBuilder: FormBuilder) {}

  ngOnInit() {
    this.firstFormGroup = this._formBuilder.group({
      firstCtrl: ['', Validators.required]
    });
    this.secondFormGroup = this._formBuilder.group({
      secondCtrl: ['', Validators.required]
    });
  }

}
