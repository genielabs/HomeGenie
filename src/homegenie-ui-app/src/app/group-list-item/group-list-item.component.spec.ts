import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupListItemComponent } from './group-list-item.component';

describe('GroupListItemComponent', () => {
  let component: GroupListItemComponent;
  let fixture: ComponentFixture<GroupListItemComponent>;

  beforeEach(async(() => {
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
