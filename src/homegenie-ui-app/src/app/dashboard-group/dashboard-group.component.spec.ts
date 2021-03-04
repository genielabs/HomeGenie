import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DashboardGroupComponent } from './dashboard-group.component';

describe('DashboardGroupComponent', () => {
  let component: DashboardGroupComponent;
  let fixture: ComponentFixture<DashboardGroupComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ DashboardGroupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
