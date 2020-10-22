import {Component, OnDestroy, OnInit} from '@angular/core';
import {Subscription} from "rxjs";
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-event-capture',
  templateUrl: './event-capture.component.html',
  styleUrls: ['./event-capture.component.scss']
})
export class EventCaptureComponent extends ControlFieldBase implements OnInit, OnDestroy {
  private moduleEventsSubscription: Subscription;

  ngOnInit() {
    super.ngOnInit();
    this.moduleEventsSubscription = this.hgui.onModuleEvent.subscribe((e) => {
      console.log(e);
    });
  }

  ngOnDestroy(): void {
    this.moduleEventsSubscription && this.moduleEventsSubscription.unsubscribe();
  }
}
