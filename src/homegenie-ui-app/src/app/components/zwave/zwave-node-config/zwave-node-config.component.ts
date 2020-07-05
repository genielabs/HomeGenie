import {Component, Input, OnInit} from '@angular/core';
import {Module, ModuleField} from '../../../services/hgui/module';
import {ZwaveApi, ZwaveConfigParam} from '../zwave-api';
import {HguiService} from '../../../services/hgui/hgui.service';
import {ErrorStateMatcher} from '@angular/material/core';
import {FormControl, FormGroupDirective, NgForm} from '@angular/forms';

export class CommandClass {
  id: any;
  description: string;
}

/** Error when invalid control is dirty, touched, or submitted. */
export class MyErrorStateMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    console.log("ERROR STATE", control);
    return true || !!(control && control.invalid && (control.dirty || control.touched || isSubmitted));
  }
}

@Component({
  selector: 'app-zwave-node-config',
  templateUrl: './zwave-node-config.component.html',
  styleUrls: ['./zwave-node-config.component.scss']
})
export class ZwaveNodeConfigComponent implements OnInit {
  @Input() module: Module;
  constructor(private hgui: HguiService) { }
  commandClasses: Array<CommandClass> = [];
  configurationParameters: Array<ZwaveConfigParam> = [];
  isNetworkBusy = true;
  errorMatcher = new MyErrorStateMatcher();

  ngOnInit(): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    adapter.zwaveAdapter.getCommandClasses(this.module).subscribe((classes) => {
      this.commandClasses = classes;
    });
    this.isNetworkBusy = true;
    adapter.zwaveAdapter.getConfigParams(this.module).subscribe((params) => {
      this.configurationParameters = params;
      params.map((cp) => {
        if (cp.field.value == null || cp.field.value.length === 0) {
          cp.status = 1; // 1 = loading
          adapter.zwaveAdapter.getConfigParam(this.module, cp.number)
            .subscribe((res) => {
              cp.field.value = res.response.ResponseValue;
              if (res.status === 200) {
                cp.status = 0; // 0 = ok
              } else {
                cp.status = 2; // 1 = error
              }
            });
        }
      });
      this.isNetworkBusy = false;
    });
  }
  onConfigParameterChange(e, p: ZwaveConfigParam): void {
    const adapter = this.hgui.getAdapter(this.module.adapterId);
    p.status = 1; // status = 1 -> loading
    if (e.target) e = e.target;
    adapter.zwaveAdapter.setConfigParam(this.module, p.number, +e.value)
      .subscribe((res) => {
        if (res.response.ResponseValue === 'ERR_TIMEOUT') {
          p.status = 2; // status = 2 -> error
        } else {
          p.field.value = res.response.ResponseValue;
          p.status = 0; // status = 0 -> ok
        }
      });
  }
}
