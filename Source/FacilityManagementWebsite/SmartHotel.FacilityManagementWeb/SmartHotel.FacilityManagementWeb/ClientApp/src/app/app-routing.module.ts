import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AdalGuard } from 'adal-angular4';
import { HotelComponent } from './hotel/hotel.component';
import { FloorComponent } from './floor/floor.component';
import { ErrorComponent } from './error/error.component';
import { LoginComponent } from './login/login.component';
import { AuthenticationGuard } from './common/authentication-guard';

const routes: Routes = [
  { path: '', component: HomeComponent, canActivate: [AuthenticationGuard]},
  { path: 'hotel', component: HotelComponent, canActivate: [AuthenticationGuard] },
  { path: 'floor', component: FloorComponent, canActivate: [AuthenticationGuard]},
  { path: 'error', component: ErrorComponent },
  { path: 'login', component: LoginComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
