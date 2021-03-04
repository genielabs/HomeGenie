import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ModuleSelectComponent } from './module-select.component';

describe('ModuleSelectComponent', () => {
  let component: ModuleSelectComponent;
  let fixture: ComponentFixture<ModuleSelectComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ModuleSelectComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModuleSelectComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
