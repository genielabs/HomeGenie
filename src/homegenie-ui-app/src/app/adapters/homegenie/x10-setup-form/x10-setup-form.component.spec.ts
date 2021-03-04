import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { X10SetupFormComponent } from './x10-setup-form.component';

describe('X10SetupFormComponent', () => {
  let component: X10SetupFormComponent;
  let fixture: ComponentFixture<X10SetupFormComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ X10SetupFormComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(X10SetupFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
