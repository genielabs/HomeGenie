import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SplashScreenComponent } from './splash-screen/splash-screen.component';
import { SetupPageComponent } from './setup-page/setup-page.component';
import {DashboardGroupComponent} from './dashboard-group/dashboard-group.component';


const routes: Routes = [
  { path: '', component: DashboardGroupComponent },
  { path: 'setup', component: SetupPageComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
