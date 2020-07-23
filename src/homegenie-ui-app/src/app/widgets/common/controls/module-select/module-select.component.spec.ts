import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModuleSelectComponent } from './module-select.component';

describe('ModuleSelectComponent', () => {
  let component: ModuleSelectComponent;
  let fixture: ComponentFixture<ModuleSelectComponent>;

  beforeEach(async(() => {
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
