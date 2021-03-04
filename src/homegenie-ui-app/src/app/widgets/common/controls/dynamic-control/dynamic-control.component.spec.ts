import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DynamicControlComponent } from './dynamic-control.component';

describe('DynamicControlComponent', () => {
  let component: DynamicControlComponent;
  let fixture: ComponentFixture<DynamicControlComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ DynamicControlComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DynamicControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
