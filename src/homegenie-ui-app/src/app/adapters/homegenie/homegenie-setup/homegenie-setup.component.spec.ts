import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { HomegenieSetupComponent } from './homegenie-setup.component';

describe('HomegenieSetupComponent', () => {
  let component: HomegenieSetupComponent;
  let fixture: ComponentFixture<HomegenieSetupComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ HomegenieSetupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HomegenieSetupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
