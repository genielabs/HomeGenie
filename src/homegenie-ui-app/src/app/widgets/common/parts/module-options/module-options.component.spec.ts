import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ModuleOptionsComponent } from './module-options.component';

describe('ModuleOptionsComponent', () => {
  let component: ModuleOptionsComponent;
  let fixture: ComponentFixture<ModuleOptionsComponent>;

  beforeEach(waitForAsync(() => {
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
