import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { WidgetOptionsDialogComponent } from './widget-options-dialog.component';

describe('WidgetOptionsDialogComponent', () => {
  let component: WidgetOptionsDialogComponent;
  let fixture: ComponentFixture<WidgetOptionsDialogComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ WidgetOptionsDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WidgetOptionsDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
