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

import {HomegenieSetupComponent} from './adapters/homegenie/homegenie-setup/homegenie-setup.component';
import {SplashScreenComponent} from './splash-screen/splash-screen.component';
import {ZwaveSetupFormComponent} from './adapters/homegenie/zwave-setup-form/zwave-setup-form.component';
import {X10SetupFormComponent} from './adapters/homegenie/x10-setup-form/x10-setup-form.component';
import {SetupPageComponent} from './setup-page/setup-page.component';
import {ZwaveSynchDialogComponent} from './adapters/homegenie/zwave-setup-form/zwave-synch-dialog/zwave-synch-dialog.component';
import {MAT_DIALOG_DEFAULT_OPTIONS} from '@angular/material/dialog';
import { ZwaveNodeConfigComponent } from './adapters/homegenie/zwave-setup-form/zwave-node-config/zwave-node-config.component';
import { ZwaveNodeListComponent } from './adapters/homegenie/zwave-setup-form/zwave-node-list/zwave-node-list.component';

export function moduleHttpLoaderFactory(http: HttpClient): ModuleTranslateLoader {
  const baseTranslateUrl = './assets/i18n';
  const options: IModuleTranslationOptions = {
    modules: [
      // final url: ./assets/i18n/en.json
      {baseTranslateUrl},
      // final url: ./assets/i18n/homegenie/en.json
      {moduleName: 'homegenie', baseTranslateUrl}
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
    ZwaveSynchDialogComponent,
    ZwaveNodeConfigComponent,
    ZwaveNodeListComponent
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
    })
  ],
  providers: [
    {provide: MAT_DIALOG_DEFAULT_OPTIONS, useValue: {hasBackdrop: true}}
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
}

