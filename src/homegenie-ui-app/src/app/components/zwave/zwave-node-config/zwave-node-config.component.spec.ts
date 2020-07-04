import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ZwaveNodeConfigComponent } from './zwave-node-config.component';

describe('ZwaveNodeConfigComponent', () => {
  let component: ZwaveNodeConfigComponent;
  let fixture: ComponentFixture<ZwaveNodeConfigComponent>;

  beforeEach(async(() => {
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
