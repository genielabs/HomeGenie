import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ActivityStatusComponent } from './activity-status.component';

describe('ActivityStatusComponent', () => {
  let component: ActivityStatusComponent;
  let fixture: ComponentFixture<ActivityStatusComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ActivityStatusComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ActivityStatusComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
