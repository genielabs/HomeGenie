import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModuleSchedulingComponent } from './module-scheduling.component';

describe('ModuleSchedulingComponent', () => {
  let component: ModuleSchedulingComponent;
  let fixture: ComponentFixture<ModuleSchedulingComponent>;

  beforeEach(async(() => {
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
