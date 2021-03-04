import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ZwaveSetupFormComponent } from './zwave-setup-form.component';

describe('ZwaveSetupFormComponent', () => {
  let component: ZwaveSetupFormComponent;
  let fixture: ComponentFixture<ZwaveSetupFormComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ZwaveSetupFormComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ZwaveSetupFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
