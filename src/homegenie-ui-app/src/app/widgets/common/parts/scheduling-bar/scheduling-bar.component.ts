import {Component, ElementRef, Input, OnInit, ViewChild} from '@angular/core';

@Component({
  selector: 'app-scheduling-bar',
  templateUrl: './scheduling-bar.component.html',
  styleUrls: ['./scheduling-bar.component.scss']
})
export class SchedulingBarComponent {
  @ViewChild('schedulingBarContainer', {static: true}) schedulingBar: ElementRef;

  @Input()
  index: number = 0;
  @Input()
  occurrences: any[] = [];

  hours = ['00','01','02','03','04','05','06','07','08','09','10','11','12','13','14','15','16','17','18','19','20','21','22','23'];

  get availableWidth(): number {
    return this.schedulingBar && this.schedulingBar.nativeElement.clientWidth > 0
      ? this.schedulingBar.nativeElement.clientWidth : 320;
  }

}
