import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ProgramOptionsComponent } from './program-options.component';

describe('ProgramOptionsComponent', () => {
  let component: ProgramOptionsComponent;
  let fixture: ComponentFixture<ProgramOptionsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ProgramOptionsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProgramOptionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
