import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { EnergyMonitorComponent } from './energy-monitor.component';

describe('EnergyMonitorComponent', () => {
  let component: EnergyMonitorComponent;
  let fixture: ComponentFixture<EnergyMonitorComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ EnergyMonitorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EnergyMonitorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
