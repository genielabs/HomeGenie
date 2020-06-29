import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { HttpClientModule, HttpClient } from '@angular/common/http';
import { FlexLayoutModule } from '@angular/flex-layout';

import { MaterialModule } from './material.module';

import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { ModuleTranslateLoader, IModuleTranslationOptions } from '@larscom/ngx-translate-module-loader';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

import { HomegenieSetupComponent } from './adapters/homegenie/homegenie-setup/homegenie-setup.component';
import { SplashScreenComponent } from './splash-screen/splash-screen.component';

export function moduleHttpLoaderFactory(http: HttpClient) {
  const baseTranslateUrl = './assets/i18n';

  const options: IModuleTranslationOptions = {
    modules: [
      // final url: ./assets/i18n/en.json
      { baseTranslateUrl },
      // final url: ./assets/i18n/homegenie/en.json
      { moduleName: 'homegenie', baseTranslateUrl }
    ]
  };
  return new ModuleTranslateLoader(http, options);
}

@NgModule({
  declarations: [
    AppComponent,
    HomegenieSetupComponent,
    SplashScreenComponent
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
      defaultLanguage: 'it',
      loader: {
        provide: TranslateLoader,
        useFactory: moduleHttpLoaderFactory,
        deps: [HttpClient]
      }
    })
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }

