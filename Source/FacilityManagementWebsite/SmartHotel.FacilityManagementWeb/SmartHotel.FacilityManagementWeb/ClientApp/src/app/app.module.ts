import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { TenantComponent } from './tenant/tenant.component';
import { AdalService, AdalInterceptor } from 'adal-angular4';
import { AppRoutingModule } from './app-routing.module';
import { FloorComponent } from './floor/floor.component';
import { HotelComponent } from './hotel/hotel.component';
import { ErrorComponent } from './error/error.component';
import { LoginComponent } from './login/login.component';
import { AuthenticationGuard } from './common/authentication-guard';
import { NgxSpinnerModule } from 'ngx-spinner';
import { Ng5SliderModule } from 'ng5-slider';
import { HotelBrandComponent } from './hotel-brand/hotel-brand.component';
import { BreadcrumbComponent } from './breadcrumb/breadcrumb.component';
import { LoadingComponent } from './loading/loading.component';
import { SlidePanelComponent } from './slide-panel/slide-panel.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    TenantComponent,
    HotelComponent,
    FloorComponent,
    ErrorComponent,
    LoginComponent,
    HotelBrandComponent,
    BreadcrumbComponent,
    LoadingComponent,
    SlidePanelComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    NgxSpinnerModule,
    Ng5SliderModule,
    BrowserAnimationsModule
  ],
  providers: [
    AdalService,
    AuthenticationGuard,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AdalInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
