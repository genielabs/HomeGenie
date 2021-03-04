import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { EventCaptureComponent } from './event-capture.component';

describe('EventCaptureComponent', () => {
  let component: EventCaptureComponent;
  let fixture: ComponentFixture<EventCaptureComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ EventCaptureComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EventCaptureComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
