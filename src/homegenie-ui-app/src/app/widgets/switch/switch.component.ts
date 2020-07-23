import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Module} from '../../services/hgui/module';
import {CMD, FLD, HguiService} from '../../services/hgui/hgui.service';
import {Adapter} from '../../adapters/adapter';
import {Subscription} from 'rxjs';

@Component({
  selector: 'app-switch',
  templateUrl: './switch.component.html',
  styleUrls: ['./switch.component.scss']
})
export class SwitchComponent implements OnInit, OnDestroy {
  @Input()
  module: Module;
  @Output()
  showOptions: EventEmitter<any> = new EventEmitter();

  status = '';

  private ledTimeout: any = null;
  private eventSubscription: Subscription;

  constructor() { }

  ngOnInit(): void {
    if (this.module.getAdapter()) {
      this.eventSubscription = this.module.getAdapter().onModuleEvent.subscribe((e) => {
        if (e.module === this.module) {
          console.log(e.event);
          this.blinkLed();
        }
      });
    }
  }
  ngOnDestroy(): void {
    if (this.eventSubscription) {
      this.eventSubscription.unsubscribe();
      console.log('Unsubscribed module events.');
    }
  }

  get level(): number {
    let l = null;
    const level = this.module.field(FLD.Status.Level);
    if (level) {
      l = level.value * 100;
    }
    return l;
  }
  set level(n: number) {
    this.module.control(CMD.Control.Level, n).subscribe((res) => {
      console.log('onOffButtonClick', res);
    });
  }

  onModuleOptionsClick(e): void {
    this.showOptions.emit(null);
  }

  onOnButtonClick(e): void {
    this.module.control(CMD.Control.On).subscribe();
  }
  onOffButtonClick(e): void {
    this.module.control(CMD.Control.Off).subscribe();
  }

  private blinkLed(): void {
    this.status = 'active';
    clearTimeout(this.ledTimeout);
    this.ledTimeout = setTimeout(() => {
      this.status = 'idle';
    }, 100);
  }
}
