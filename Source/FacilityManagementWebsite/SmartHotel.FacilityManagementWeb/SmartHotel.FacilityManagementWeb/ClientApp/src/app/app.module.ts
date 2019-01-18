import { BrowserModule } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import * as AuthenticationContext from 'adal-angular/lib/adal';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { TenantComponent } from './tenant/tenant.component';
import { AdalService } from 'adal-angular4';
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
import { EnvironmentService } from './services/environment.service';
import { SecurePipe } from './pipes/secure.pipe';
import { MapComponent } from './map/map.component';
import { AuthenticationInterceptor } from './common/authentication-interceptor';
import { TsiChartComponent } from './tsi-chart/tsi-chart.component';

const initializeApp = (environmentService: EnvironmentService) => {
  return () => {
    const loadEnvironmentPromise = environmentService.loadEnvironment();
    return loadEnvironmentPromise;
  };
};

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
    SlidePanelComponent,
    SecurePipe,
    MapComponent,
    TsiChartComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    ReactiveFormsModule,
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
      useClass: AuthenticationInterceptor,
      multi: true
    },
    EnvironmentService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      multi: true,
      deps: [EnvironmentService]
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

// tslint:disable:quotemark
// tslint:disable:no-var-keyword
// tslint:disable:prefer-const
/**
 * HACK: This is used to replace the functionality of the _addHintParameters function on AuthenticationContext in adal-angular package
 *  (https://github.com/AzureAD/azure-activedirectory-library-for-js)
 *  This issue has been logged (https://github.com/AzureAD/azure-activedirectory-library-for-js/issues/580)
 *  Workaround (https://github.com/AzureAD/azure-activedirectory-library-for-js/issues/580#issuecomment-414708770) is being used.
 * Adds login_hint to authorization URL which is used to pre-fill the username field of sign in page for the user if known ahead of time.
 * domain_hint can be one of users/organisations which when added skips the email based discovery process of the user.
 * @ignore
 */
AuthenticationContext.prototype._addHintParameters = function (urlNavigate) {
  // If you don't use prompt=none, then if the session does not exist, there will be a failure.
  // If sid is sent alongside domain or login hints, there will be a failure since request is ambiguous.
  // If sid is sent with a prompt value other than none or attempt_none, there will be a failure since the request is ambiguous.

  if (this._user && this._user.profile) {
    if (this._user.profile.sid && urlNavigate.indexOf('&prompt=none') !== -1) {
      // don't add sid twice if user provided it in the extraQueryParameter value
      if (!this._urlContainsQueryStringParameter("sid", urlNavigate)) {
        // add sid
        urlNavigate += '&sid=' + encodeURIComponent(this._user.profile.sid);
      }
    } else if (this._user.profile.upn || this._user.profile.email) {

      let loginHint;
      if (this._user.profile.upn) {
        loginHint = this._user.profile.upn;
      } else {
        loginHint = this._user.profile.email;
      }

      // don't add login_hint twice if user provided it in the extraQueryParameter value
      if (!this._urlContainsQueryStringParameter("login_hint", urlNavigate)) {
        // add login_hint
        urlNavigate += '&login_hint=' + encodeURIComponent(loginHint);
      }
      // don't add domain_hint twice if user provided it in the extraQueryParameter value
      if (!this._urlContainsQueryStringParameter("domain_hint", urlNavigate) && loginHint.indexOf('@') > -1) {
        var parts = loginHint.split('@');
        // local part can include @ in quotes. Sending last part handles that.
        urlNavigate += '&domain_hint=' + encodeURIComponent(parts[parts.length - 1]);
      }
    } else if (this._user.userName) {
      // don't add login_hint twice if user provided it in the extraQueryParameter value
      if (!this._urlContainsQueryStringParameter("login_hint", urlNavigate)) {
        // add login_hint
        urlNavigate += '&login_hint=' + encodeURIComponent(this._user.userName);
      }
    }

  }

  return urlNavigate;
};
  // tslint:enable:quotemark
  // tslint:enable:no-var-keyword
  // tslint:enable:prefer-const
