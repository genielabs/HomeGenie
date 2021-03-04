import {AfterViewInit, Component, Inject, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Module} from "../../../../services/hgui/module";
import {MAT_DIALOG_DATA} from "@angular/material/dialog";
import {CMD, HguiService} from "../../../../services/hgui/hgui.service";
import {DynamicOptionsBase} from "../../parts/dynamic-options-base.component";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-widget-options-dialog',
  templateUrl: './widget-options-dialog.component.html',
  styleUrls: ['./widget-options-dialog.component.scss']
})
export class WidgetOptionsDialogComponent implements OnDestroy {
  @ViewChild('optionsHandler', {static: false})
  optionsHandler: DynamicOptionsBase;

  showPage = '';
  module: Module;

  selectedDate = new Date();

  constructor(
    private hgui: HguiService,
    public translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) data: {module: Module, option: 'statistics' | 'settings' | 'schedule'}
  ) {
    this.module = data.module;
    this.showPage = data.option;
  }

  ngOnDestroy(): void {
    if (this.optionsHandler && this.optionsHandler.isChanged) {
      this.optionsHandler.applyChanges();
    }
  }

}
