import {Component, Input, OnInit} from '@angular/core';
import {Module} from "../../../../services/hgui/module";
import {Subscription} from "rxjs";
import {FLD} from "../../../../services/hgui/hgui.service";

@Component({
  selector: 'app-activity-status',
  templateUrl: './activity-status.component.html',
  styleUrls: ['./activity-status.component.scss']
})
export class ActivityStatusComponent implements OnInit {
  @Input()
  module: Module;
  @Input()
  statusText: string;
  errorText = '';
  isLedActive = false;

  private ledTimeout: any = null;
  private eventSubscription: Subscription;

  constructor() { }

  ngOnInit(): void {
    this.eventSubscription = this.module.event.subscribe((e) => {
      this.blinkLed();
    });
  }
  ngOnDestroy(): void {
    if (this.eventSubscription) {
      this.eventSubscription.unsubscribe();
      console.log('Unsubscribed module events.');
    }
  }

  public setError(message: string): void {
    this.errorText = message;
    this.blinkLed();
  }

  private blinkLed(): void {
    if (this.isLedActive && this.errorText.length > 0) return;
    clearTimeout(this.ledTimeout);
    this.isLedActive = true;
    this.ledTimeout = setTimeout(() => {
      this.isLedActive = false;
      this.errorText = '';
    }, this.errorText.length > 0 ? 1000 : 100);
  }
}
