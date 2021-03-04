import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { GroupListItemComponent } from './group-list-item.component';

describe('GroupListItemComponent', () => {
  let component: GroupListItemComponent;
  let fixture: ComponentFixture<GroupListItemComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ GroupListItemComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GroupListItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
