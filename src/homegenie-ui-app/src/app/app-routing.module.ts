import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SplashScreenComponent } from './splash-screen/splash-screen.component';
import { SetupPageComponent } from './setup-page/setup-page.component';


const routes: Routes = [
  { path: '', component: SplashScreenComponent },
  { path: 'setup', component: SetupPageComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
