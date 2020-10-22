import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScenarioSelectComponent } from './scenario-select.component';

describe('ProgramSelectComponent', () => {
  let component: ScenarioSelectComponent;
  let fixture: ComponentFixture<ScenarioSelectComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ScenarioSelectComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScenarioSelectComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
