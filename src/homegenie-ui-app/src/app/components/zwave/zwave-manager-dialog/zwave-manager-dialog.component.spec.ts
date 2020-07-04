import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ZwaveManagerDialogComponent } from './zwave-synch-dialog.component';

describe('ZwaveSynchDialogComponent', () => {
  let component: ZwaveManagerDialogComponent;
  let fixture: ComponentFixture<ZwaveManagerDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ZwaveManagerDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ZwaveManagerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
