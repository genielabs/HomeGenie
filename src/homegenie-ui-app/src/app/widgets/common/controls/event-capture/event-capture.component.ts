import {Component, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subscription} from "rxjs";
import {ControlFieldBase} from "../control-field-base";

@Component({
  selector: 'app-event-capture',
  templateUrl: './event-capture.component.html',
  styleUrls: ['./event-capture.component.scss']
})
export class EventCaptureComponent extends ControlFieldBase implements OnInit, OnDestroy {
  @ViewChild('inputElement') inputElement: ElementRef;
  isCapturing = false;

  private moduleEventsSubscription: Subscription;

  ngOnInit() {
    super.ngOnInit();
    // subscribe to HGUI events stream
    this.moduleEventsSubscription = this.hgui.onModuleEvent.subscribe((e) => {
      if (!this.isCapturing) return;
      const field = e.event;
      if (field.key === 'Receiver.RawData') {
        this.isCapturing = false;
        this._snackBar.dismiss();
        this.inputElement.nativeElement.value = field.value;
        this.fieldChange.emit({ field: this.data.field, value: field.value });
      }
    });
  }

  ngOnDestroy(): void {
    // unsubscribe HGUI events stream
    this.moduleEventsSubscription && this.moduleEventsSubscription.unsubscribe();
    this._snackBar.dismiss();
  }

  onCaptureClick(): void {
    this.isCapturing = true;
    const snackBarRef = this._snackBar.open("Capturing `Receiver.RawData` events...", "Stop", {
      duration: 10000
    });
    snackBarRef.onAction().subscribe(() => {
      this.isCapturing = false;
    });
    snackBarRef.afterDismissed().subscribe(() => {
      this.isCapturing = false;
    });
  }

  onTextFieldChange(e): void {
    this.fieldChange.emit({ field: this.data.field, value: e.target.value });
  }
}
