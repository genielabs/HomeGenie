import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ZwaveNodeConfigComponent } from './zwave-node-config.component';

describe('ZwaveNodeConfigComponent', () => {
  let component: ZwaveNodeConfigComponent;
  let fixture: ComponentFixture<ZwaveNodeConfigComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ZwaveNodeConfigComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ZwaveNodeConfigComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
