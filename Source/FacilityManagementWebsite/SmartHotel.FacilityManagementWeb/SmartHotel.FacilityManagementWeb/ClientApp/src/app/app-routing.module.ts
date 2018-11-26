import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { TenantComponent } from './tenant/tenant.component';
import { HotelComponent } from './hotel/hotel.component';
import { FloorComponent } from './floor/floor.component';
import { ErrorComponent } from './error/error.component';
import { LoginComponent } from './login/login.component';
import { AuthenticationGuard } from './common/authentication-guard';
import { HotelBrandComponent } from './hotel-brand/hotel-brand.component';
import { LoadingComponent } from './loading/loading.component';

const routes: Routes = [
  { path: '', component: LoadingComponent, canActivate: [AuthenticationGuard] },
  { path: 'tenant', component: TenantComponent, canActivate: [AuthenticationGuard] },
  { path: 'hotelbrand', component: HotelBrandComponent, canActivate: [AuthenticationGuard] },
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
