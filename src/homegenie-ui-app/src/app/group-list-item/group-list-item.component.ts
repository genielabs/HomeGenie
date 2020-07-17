import {Component, Input, OnInit} from '@angular/core';
import {Group} from '../services/hgui/group';
import {HguiService} from '../services/hgui/hgui.service';

@Component({
  selector: 'app-group-list-item',
  templateUrl: './group-list-item.component.html',
  styleUrls: ['./group-list-item.component.scss']
})
export class GroupListItemComponent implements OnInit {
  @Input()
  group: Group;

  constructor(public hgui: HguiService) { }

  ngOnInit(): void {
  }

}
