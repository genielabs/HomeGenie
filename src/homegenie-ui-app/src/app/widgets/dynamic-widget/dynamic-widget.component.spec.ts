import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DynamicWidgetComponent } from './dynamic-widget.component';

describe('DynamicWidgetComponent', () => {
  let component: DynamicWidgetComponent;
  let fixture: ComponentFixture<DynamicWidgetComponent>;

  beforeEach(async(() => {
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
