import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { AlarmSystemComponent } from './alarm-system.component';

describe('AlarmSystemComponent', () => {
  let component: AlarmSystemComponent;
  let fixture: ComponentFixture<AlarmSystemComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ AlarmSystemComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AlarmSystemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
