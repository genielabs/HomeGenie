import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ProgramSelectComponent } from './program-select.component';

describe('ProgramSelectComponent', () => {
  let component: ProgramSelectComponent;
  let fixture: ComponentFixture<ProgramSelectComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ProgramSelectComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProgramSelectComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
