import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModuleOptionsComponent } from './module-options.component';

describe('ModuleOptionsComponent', () => {
  let component: ModuleOptionsComponent;
  let fixture: ComponentFixture<ModuleOptionsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ModuleOptionsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModuleOptionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
