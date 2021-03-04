import {BrowserModule} from '@angular/platform-browser';
import {NgModule} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';

import {BrowserAnimationsModule} from '@angular/platform-browser/animations';

import {HttpClient, HttpClientModule, HttpClientJsonpModule} from '@angular/common/http';

import { LayoutModule } from '@angular/cdk/layout';
import {FlexLayoutModule} from '@angular/flex-layout';

import {MaterialModule} from './material.module';

import {TranslateLoader, TranslateModule} from '@ngx-translate/core';
import {IModuleTranslationOptions, ModuleTranslateLoader} from '@larscom/ngx-translate-module-loader';

import {UnitsConvererModule} from 'ngx-units-converter'

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';

import { MomentModule } from 'ngx-moment';

import { AngularSvgIconModule } from 'angular-svg-icon';

import { CodeEditorModule } from '@ngstack/code-editor';

import { ChartsModule } from 'ng2-charts';

import { ReteModule } from 'rete-angular-render-plugin';

import {HomegenieSetupComponent} from './adapters/homegenie/homegenie-setup/homegenie-setup.component';
import {SplashScreenComponent} from './splash-screen/splash-screen.component';
import {ZwaveSetupFormComponent} from './adapters/homegenie/zwave-setup-form/zwave-setup-form.component';
import {X10SetupFormComponent} from './adapters/homegenie/x10-setup-form/x10-setup-form.component';
import {SetupPageComponent} from './setup-page/setup-page.component';
import {ZwaveManagerDialogComponent} from './components/zwave/zwave-manager-dialog/zwave-manager-dialog.component';
import {MAT_DIALOG_DEFAULT_OPTIONS} from '@angular/material/dialog';
import {ZwaveNodeConfigComponent} from './components/zwave/zwave-node-config/zwave-node-config.component';
import {ZwaveNodeListComponent} from './components/zwave/zwave-node-list/zwave-node-list.component';
import { SwitchComponent } from './widgets/switch/switch.component';
import { SensorComponent } from './widgets/sensor/sensor.component';
import { SensorValueFormatterPipe } from './pipes/SensorValueFormatterPipe';
import { DashboardGroupComponent } from './dashboard-group/dashboard-group.component';
import { GroupListItemComponent } from './group-list-item/group-list-item.component';
import { ProgramEditorComponent } from './automation/program-editor/program-editor.component';
import { WidgetOptionsDialogComponent } from './widgets/common/dialogs/widget-options-dialog/widget-options-dialog.component';
import { ControlFieldBase } from './widgets/common/controls/control-field-base';
import { CheckboxComponent } from './widgets/common/controls/checkbox/checkbox.component';
import { ModuleSelectComponent } from './widgets/common/controls/module-select/module-select.component';
import { ScenarioSelectComponent } from './widgets/common/controls/scenario-select/scenario-select.component';
import { SliderComponent } from './widgets/common/controls/slider/slider.component';
import { TextComponent } from './widgets/common/controls/text/text.component';
import { EventCaptureComponent } from './widgets/common/controls/event-capture/event-capture.component';
import { ChartComponent } from './components/chart/chart.component';
import { DynamicWidgetComponent } from './widgets/dynamic-widget/dynamic-widget.component';
import { DynamicOptionsBase } from './widgets/common/parts/dynamic-options-base.component';
import { DynamicControlComponent } from './widgets/common/controls/dynamic-control/dynamic-control.component';

import { NgxSvgModule } from 'ngx-svg';

import { MyReteEditorModule } from "./automation/visual-editor/rete.module";

import { NgxColorsModule } from 'ngx-colors';
import { ColorPickerDialogComponent } from './widgets/common/dialogs/color-picker-dialog/color-picker-dialog.component';
import { ColorPickerModule } from '@iplab/ngx-color-picker';

import { EnergyMonitorComponent } from './widgets/energy-monitor/energy-monitor.component';
import { ThermostatComponent } from './widgets/thermostat/thermostat.component';
import { AlarmSystemComponent } from './widgets/alarm-system/alarm-system.component';
import { WeatherForecastComponent } from './widgets/weather-forecast/weather-forecast.component';
import { ActivityStatusComponent } from './widgets/common/parts/activity-status/activity-status.component';
import { ModuleOptionsComponent } from './widgets/common/parts/module-options/module-options.component';
import { ProgramOptionsComponent } from './widgets/common/parts/program-options/program-options.component';
import { WidgetOptionsMenuComponent } from './widgets/common/parts/widget-options-menu/widget-options-menu.component';
import { ModuleSchedulingComponent } from './widgets/common/parts/module-scheduling/module-scheduling.component';
import { SchedulingBarComponent } from './widgets/common/parts/scheduling-bar/scheduling-bar.component';

export function moduleHttpLoaderFactory(http: HttpClient): ModuleTranslateLoader {
  const baseTranslateUrl = './assets/i18n';
  const options: IModuleTranslationOptions = {
    modules: [
      {baseTranslateUrl},
      {moduleName: 'homegenie', baseTranslateUrl},
      {moduleName: 'zwave', baseTranslateUrl},
      {moduleName: 'module', baseTranslateUrl}
    ]
  };
  return new ModuleTranslateLoader(http, options);
}

@NgModule({
  declarations: [
    AppComponent,
    HomegenieSetupComponent,
    SplashScreenComponent,
    ZwaveSetupFormComponent,
    X10SetupFormComponent,
    SetupPageComponent,
    ZwaveManagerDialogComponent,
    ZwaveNodeConfigComponent,
    ZwaveNodeListComponent,
    SwitchComponent,
    SensorComponent,
    SensorValueFormatterPipe,
    DashboardGroupComponent,
    GroupListItemComponent,
    ProgramEditorComponent,
    WidgetOptionsDialogComponent,
    ControlFieldBase,
    CheckboxComponent,
    ModuleSelectComponent,
    ScenarioSelectComponent,
    SliderComponent,
    TextComponent,
    EventCaptureComponent,
    ChartComponent,
    DynamicWidgetComponent,
    DynamicOptionsBase,
    DynamicControlComponent,
    ColorPickerDialogComponent,
    EnergyMonitorComponent,
    ThermostatComponent,
    AlarmSystemComponent,
    WeatherForecastComponent,
    ActivityStatusComponent,
    ModuleOptionsComponent,
    ProgramOptionsComponent,
    WidgetOptionsMenuComponent,
    ModuleSchedulingComponent,
    SchedulingBarComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    FlexLayoutModule,
    HttpClientModule,
    HttpClientJsonpModule,
    MaterialModule,
    TranslateModule.forRoot({
      defaultLanguage: 'en',
      loader: {
        provide: TranslateLoader,
        useFactory: moduleHttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
    NgxSvgModule,
    MomentModule.forRoot(),
    UnitsConvererModule,
    AngularSvgIconModule.forRoot(),

    CodeEditorModule.forRoot(),

    ChartsModule,

    ReteModule,
    MyReteEditorModule,
    NgxColorsModule,

    ColorPickerModule,

    LayoutModule

  ],
  providers: [
    {provide: MAT_DIALOG_DEFAULT_OPTIONS, useValue: {hasBackdrop: true}}
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
}
