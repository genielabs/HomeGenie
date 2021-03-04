import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ThermostatComponent } from './thermostat.component';

describe('ThermostatComponent', () => {
  let component: ThermostatComponent;
  let fixture: ComponentFixture<ThermostatComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ThermostatComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ThermostatComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
