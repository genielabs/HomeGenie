import {Component, Inject, OnDestroy, OnInit} from '@angular/core';
import {Color, ColorPickerControl} from "@iplab/ngx-color-picker";
import {MAT_DIALOG_DATA} from "@angular/material/dialog";
import {Module} from "../../../../services/hgui/module";

@Component({
  selector: 'app-color-picker-dialog',
  templateUrl: './color-picker-dialog.component.html',
  styleUrls: ['./color-picker-dialog.component.scss']
})
export class ColorPickerDialogComponent implements OnInit, OnDestroy {

  color = '#550a55';
  compactControl = new ColorPickerControl();

  isLoading = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { module: Module, color: string }
  ) {
    this.color = data.color;
  }

  ngOnInit(): void {
    this.compactControl.hidePresets();
    this.compactControl.hideAlphaChannel();
  }
  ngOnDestroy(): void {
  }

  onColorChange(c: any) {
    if (this.isLoading) return;
    this.isLoading = true;
    const hsv = Color.from(c).getHsva();
    const h = hsv.hue / 360.0;
    const s = hsv.saturation / 100.0;
    const v = hsv.value / 100.0;
    const options = `${h},${s},${v}`;
    // TODO: make this generic
    this.data.module.control('Control.ColorHsb', options)
      .subscribe((res) => {
        setTimeout(() => {this.isLoading = false;}, 100);
      });
  }

  getColor(): Color {
    return Color.from(this.color);
  }
}
