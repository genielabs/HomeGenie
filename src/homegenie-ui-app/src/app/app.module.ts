import {BrowserModule} from '@angular/platform-browser';
import {NgModule} from '@angular/core';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';

import {BrowserAnimationsModule} from '@angular/platform-browser/animations';

import {HttpClient, HttpClientModule} from '@angular/common/http';
import {FlexLayoutModule} from '@angular/flex-layout';

import {MaterialModule} from './material.module';

import {TranslateLoader, TranslateModule} from '@ngx-translate/core';
import {IModuleTranslationOptions, ModuleTranslateLoader} from '@larscom/ngx-translate-module-loader';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';

import { AngularSvgIconModule } from 'angular-svg-icon';

import { CodeEditorModule } from '@ngstack/code-editor';

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
import {SensorValueFormatterPipe} from './pipes/SensorValueFormatterPipe';
import { DashboardGroupComponent } from './dashboard-group/dashboard-group.component';
import { GroupListItemComponent } from './group-list-item/group-list-item.component';
import { ProgramEditorComponent } from './automation/program-editor/program-editor.component';
import { WidgetOptionsDialogComponent } from './widgets/common/widget-options-dialog/widget-options-dialog.component';
import { CheckboxComponent } from './widgets/common/controls/checkbox/checkbox.component';
import { ModuleSelectComponent } from './widgets/common/controls/module-select/module-select.component';
import { ProgramSelectComponent } from './widgets/common/controls/program-select/program-select.component';
import { SliderComponent } from './widgets/common/controls/slider/slider.component';
import { TextComponent } from './widgets/common/controls/text/text.component';
import { EventCaptureComponent } from './widgets/common/controls/event-capture/event-capture.component';

export function moduleHttpLoaderFactory(http: HttpClient): ModuleTranslateLoader {
  const baseTranslateUrl = './assets/i18n';
  const options: IModuleTranslationOptions = {
    modules: [
      {baseTranslateUrl},
      {moduleName: 'homegenie', baseTranslateUrl},
      {moduleName: 'zwave', baseTranslateUrl}
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
    CheckboxComponent,
    ModuleSelectComponent,
    ProgramSelectComponent,
    SliderComponent,
    TextComponent,
    EventCaptureComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    FlexLayoutModule,
    HttpClientModule,
    MaterialModule,
    TranslateModule.forRoot({
      defaultLanguage: 'en',
      loader: {
        provide: TranslateLoader,
        useFactory: moduleHttpLoaderFactory,
        deps: [HttpClient]
      }
    }),
    AngularSvgIconModule.forRoot(),
    CodeEditorModule.forRoot()
  ],
  providers: [
    {provide: MAT_DIALOG_DEFAULT_OPTIONS, useValue: {hasBackdrop: true}}
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
}
