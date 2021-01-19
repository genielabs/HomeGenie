import {ChangeDetectorRef, Component, EventEmitter, Input, Output} from '@angular/core';
import {Module} from '../services/hgui/module';
import {WidgetOptions} from "./widget-options";
import {MatDialog} from "@angular/material/dialog";
import {HguiService} from "../services/hgui/hgui.service";
import {MediaMatcher} from "@angular/cdk/layout";

@Component({
  selector: '-widget-base',
  template: 'no-ui'
})
export class WidgetBase {
  @Input()
  module: Module;
  @Input()
  options: WidgetOptions;
  @Output()
  showOptions: EventEmitter<any> = new EventEmitter();

  private matcher: MediaQueryList;
  private mobileQueryListener: () => void;
  get isSmallScreen(): boolean {
    return this.matcher.matches;
  }

  get data(): any {
    return this.options ? this.options.data : {};
  }

  constructor(protected dialog: MatDialog, protected hgui: HguiService, changeDetectorRef: ChangeDetectorRef, public mediaMatcher: MediaMatcher) {
    this.matcher = this.mediaMatcher.matchMedia('(max-width: 500px)');
    this.mobileQueryListener = () => changeDetectorRef.detectChanges();
  }
}
