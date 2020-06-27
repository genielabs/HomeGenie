import { TestBed } from '@angular/core/testing';

import { HguiService } from './hgui.service';

describe('HguiService', () => {
  let service: HguiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HguiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
