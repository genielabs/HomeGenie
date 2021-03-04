import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ColorPickerDialogComponent } from './color-picker-dialog.component';

describe('ColorPickerDialogComponent', () => {
  let component: ColorPickerDialogComponent;
  let fixture: ComponentFixture<ColorPickerDialogComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ColorPickerDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ColorPickerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
