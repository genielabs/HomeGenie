import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DynamicWidgetComponent } from './dynamic-widget.component';

describe('DynamicWidgetComponent', () => {
  let component: DynamicWidgetComponent;
  let fixture: ComponentFixture<DynamicWidgetComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ DynamicWidgetComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DynamicWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
