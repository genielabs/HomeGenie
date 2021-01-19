import {Component, ElementRef, Input, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {ControlFieldBase} from "../control-field-base";
import {Observable} from "rxjs";

@Component({
  selector: 'app-text',
  templateUrl: './text.component.html',
  styleUrls: ['./text.component.scss']
})
export class TextComponent extends ControlFieldBase implements OnInit {
  @ViewChild('field', { read: ElementRef, static: true })
  textInputElement: ElementRef;
  @Input()
  type = "text";
  @Input()
  autocomplete: string;

  filteredOptions: string[] = [];

  ngOnInit() {
    super.ngOnInit();
    // changing border-top in scss is not working, so...
    const el = this.textInputElement.nativeElement.querySelector('.mat-form-field-infix');
    el.style.borderTop = 0;
  }

  onTextFieldChange(e): void {
    this.fieldChange.emit({ field: this.data.field, value: e.target.value });
    if (this.autocomplete) {
      this.getAutocompleteResults(e.target.value);
    }
  }

  onAutoCompleteSelect(e): void {
    this.fieldChange.emit({ field: this.data.field, value: e.option.value });
  }

  private getAutocompleteResults(filter: string): void {
    // populate auto-complete items list
    const autoCompleteCallback = this.data.type.options[0];
    (autoCompleteCallback(filter) as Observable<string[]>)
      .subscribe(options => this.filteredOptions = options);
  }
}
