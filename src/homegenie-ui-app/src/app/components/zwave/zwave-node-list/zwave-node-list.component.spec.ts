import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ZwaveNodeListComponent } from './zwave-node-list.component';

describe('ZwaveNodeListComponent', () => {
  let component: ZwaveNodeListComponent;
  let fixture: ComponentFixture<ZwaveNodeListComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ZwaveNodeListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ZwaveNodeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
