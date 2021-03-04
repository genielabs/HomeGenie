import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SchedulingBarComponent } from './scheduling-bar.component';

describe('SchedulingBarComponent', () => {
  let component: SchedulingBarComponent;
  let fixture: ComponentFixture<SchedulingBarComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SchedulingBarComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SchedulingBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
