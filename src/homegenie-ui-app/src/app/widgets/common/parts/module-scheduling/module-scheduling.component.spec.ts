import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ModuleSchedulingComponent } from './module-scheduling.component';

describe('ModuleSchedulingComponent', () => {
  let component: ModuleSchedulingComponent;
  let fixture: ComponentFixture<ModuleSchedulingComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ModuleSchedulingComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModuleSchedulingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
