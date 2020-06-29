import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomegenieSetupComponent } from './adapters/homegenie/homegenie-setup/homegenie-setup.component';
import { SplashScreenComponent } from './splash-screen/splash-screen.component';


const routes: Routes = [
  { path: '', component: SplashScreenComponent },
  { path: 'setup', component: HomegenieSetupComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
