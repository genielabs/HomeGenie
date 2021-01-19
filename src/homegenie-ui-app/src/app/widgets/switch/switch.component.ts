import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {Module, ModuleField} from '../../services/hgui/module';
import {CMD, FLD, HguiService} from '../../services/hgui/hgui.service';
import {Subscription} from 'rxjs';
import {MatDialog} from "@angular/material/dialog";
import {ColorPickerDialogComponent} from "../common/dialogs/color-picker-dialog/color-picker-dialog.component";
import {Color} from "@iplab/ngx-color-picker";
import {WidgetOptions} from "../widget-options";
import {WidgetBase} from "../widget-base";

@Component({
  selector: 'app-switch',
  templateUrl: './switch.component.html',
  styleUrls: ['./switch.component.scss']
})
export class SwitchComponent extends WidgetBase implements OnInit, OnDestroy {

  status = '';
  colorPresets: ModuleField[] = [];
  colorPresetsCount = 4;
  currentPreset = -1;

  isLoading = false;

  private ledTimeout: any = null;
  private eventSubscription: Subscription;

  get level(): number {
    let l = null;
    const level = this.module.field(FLD.Status.Level);
    if (level) {
      l = level.value.toString().replace(',', '.') * 100;
    }
    return l;
  }
  set level(n: number) {
    if (this.isLoading) return;
    this.isLoading = true;
    this.module.control(CMD.Control.Level, n).subscribe((res) => {
      this.isLoading = false;
    });
  }

  private _color = '#550a55';
  get color(): string {
    const hsvField = this.module.field(FLD.Status.ColorHsb);
    if (hsvField) {
      let hsv = hsvField.value.split(',').map(v => +v);
      const h = hsv[0] * 360;
      const s = hsv[1] * 100;
      const v = hsv[2] * 100;
      const c = new Color();
      c.setHsva(h, s, v, 1);
      this._color = c.toHexString();
    }
    return this._color;
  }
  set color(c: string) {
    if (this.isLoading) return;
    const color = Color.from(c);
    const hsv = color.getHsva();
    const h = hsv.hue / 360.0;
    const s = hsv.saturation / 100.0;
    const v = hsv.value / 100.0;
    const options = `${h},${s},${v}`;
    this.isLoading = true;
    this.module.control(CMD.Control.ColorHsb, options)
      .subscribe((res) => {
        this._color = c;
        this.isLoading = false;
      });
  }

  ngOnInit(): void {
    for (let i = 0; i < this.colorPresetsCount; i++) {
      const presetFieldName = `_hgui.Module.ColorPresets.${i}`;
      let presetField = this.module.field(presetFieldName);
      if (!presetField) {
        this.module.field(presetFieldName, '#f00');
        presetField = this.module.field(presetFieldName);
      }
      this.colorPresets.push(presetField);
    }
    if (this.module.getAdapter()) {
      this.eventSubscription = this.module.getAdapter().onModuleEvent.subscribe((e) => {
        if (e.module === this.module) {
          this.blinkLed();
        }
      });
    }
  }
  ngOnDestroy(): void {
    if (this.eventSubscription) {
      this.eventSubscription.unsubscribe();
      console.log('Unsubscribed module events.');
    }
  }

  onModuleOptionsClick(e): void {
    this.showOptions.emit(null);
  }

  onOnButtonClick(e): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.module.control(CMD.Control.On).subscribe((res) => this.isLoading = false);
  }
  onOffButtonClick(e): void {
    if (this.isLoading) return;
    this.isLoading = true;
    this.module.control(CMD.Control.Off).subscribe((res) => this.isLoading = false);
  }

  onToggleButtonClick(e: MouseEvent) {
    if (this.isLoading) return;
    this.isLoading = true;
    this.module.control(CMD.Control.Toggle).subscribe((res) => this.isLoading = false);
  }

  onColorClick() {
    this.currentPreset = -1;
    if (this.level === 0) {
      this.color = this._color;
      return;
    }
    const currentColor = this._color;
    const dialogRef = this.dialog.open(ColorPickerDialogComponent, {
      // height: '400px',
      width: '100%',
      minWidth: '300px',
      maxWidth: '300px',
      disableClose: false,
      data: { module: this.module, color: this._color }
    });
    dialogRef.afterClosed().subscribe((color: Color) => {
      if (color && currentColor === color.toHexString(true)) {
        // color was not changed, just return
        return;
      }
      if (!color) {
        // restore current color if user canceled the dialog
        color = Color.from(currentColor);
      }
      this.color = color.toHexString(true);
    });
  }

  onPresetColorClick(presetIndex: number) {
    const preset = this.colorPresets[presetIndex];
    const currentColor = this._color;
    if (this.level === 0) {
      this.currentPreset = presetIndex;
      this.color = preset.value;
      return;
    }
    if (this.currentPreset !== presetIndex) {
      // select and set current preset
      this.currentPreset = presetIndex;
      this.color = preset.value;
      return;
    }
    const dialogRef = this.dialog.open(ColorPickerDialogComponent, {
      // height: '400px',
      width: '100%',
      minWidth: '300px',
      maxWidth: '300px',
      disableClose: false,
      data: { module: this.module, color: preset.value }
    });
    dialogRef.afterClosed().subscribe((color: Color) => {
      if (preset.value === color) {
        // color was not changed, just return
        return;
      }
      if (!color) {
        // restore current color if user canceled the dialog
        this.color = currentColor;
      } else {
        // set the new color preset and save the config
        this.color = preset.value = color.toHexString(true);
        this.hgui.saveConfiguration();
      }
    });
  }

  onSliderInput(e: any) {
    this.level = e.value;
  }

  invertColor(color: string): string {
    const c = Color.from(color);
    const rgba = c.getRgba();
    return (rgba.red * 0.299 + rgba.green * 0.587 + rgba.blue * 0.114) > 186
      ? '#000000'
      : '#FFFFFF';
  }

  private blinkLed(): void {
    this.status = 'active';
    clearTimeout(this.ledTimeout);
    this.ledTimeout = setTimeout(() => {
      this.status = 'idle';
    }, 100);
  }
}
