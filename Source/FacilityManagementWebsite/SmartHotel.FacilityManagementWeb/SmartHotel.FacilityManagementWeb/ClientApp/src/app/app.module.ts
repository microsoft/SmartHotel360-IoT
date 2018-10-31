import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { AdalService, AdalInterceptor } from 'adal-angular4';
import { AppRoutingModule } from './app-routing.module';
import { FloorComponent } from './floor/floor.component';
import { HotelComponent } from './hotel/hotel.component';
import { ErrorComponent } from './error/error.component';
import { LoginComponent } from './login/login.component';
import { AuthenticationGuard } from './common/authentication-guard';
import { FacilityService } from './services/facility.service';
import { Ng4LoadingSpinnerModule } from 'ng4-loading-spinner';
import { Ng5SliderModule } from 'ng5-slider';
import { HotelBrandComponent } from './hotel-brand/hotel-brand.component';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    HotelComponent,
    FloorComponent,
    ErrorComponent,
    LoginComponent,
    HotelBrandComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    Ng4LoadingSpinnerModule.forRoot(),
    Ng5SliderModule
  ],
  providers: [
    AdalService,
    AuthenticationGuard,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AdalInterceptor,
      multi: true
    },
    FacilityService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
