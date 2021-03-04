import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { WidgetOptionsMenuComponent } from './widget-options-menu.component';

describe('WidgetOptionsMenuComponent', () => {
  let component: WidgetOptionsMenuComponent;
  let fixture: ComponentFixture<WidgetOptionsMenuComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ WidgetOptionsMenuComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WidgetOptionsMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
