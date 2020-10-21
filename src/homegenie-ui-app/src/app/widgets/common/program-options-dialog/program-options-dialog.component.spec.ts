import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ProgramOptionsDialogComponent } from './program-options-dialog.component';

describe('ProgramOptionsDialogComponent', () => {
  let component: ProgramOptionsDialogComponent;
  let fixture: ComponentFixture<ProgramOptionsDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ProgramOptionsDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProgramOptionsDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
