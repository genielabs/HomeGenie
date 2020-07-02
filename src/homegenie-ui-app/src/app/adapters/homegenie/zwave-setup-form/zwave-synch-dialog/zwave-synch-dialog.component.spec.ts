import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ZwaveSynchDialogComponent } from './zwave-synch-dialog.component';

describe('ZwaveSynchDialogComponent', () => {
  let component: ZwaveSynchDialogComponent;
  let fixture: ComponentFixture<ZwaveSynchDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ZwaveSynchDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ZwaveSynchDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
