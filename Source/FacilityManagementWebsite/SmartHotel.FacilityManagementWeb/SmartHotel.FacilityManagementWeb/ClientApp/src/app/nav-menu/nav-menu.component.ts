import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { FacilityService } from '../services/facility.service';
import { AppComponent } from '../app.component';
import { environment } from 'src/environments/environment';
import { Router } from '@angular/router';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent implements OnInit {

  constructor(private adalSvc: AdalService,
    private facilityService: FacilityService,
    private router: Router) { }

  public useBasicAuth: boolean;

  ngOnInit() {
    this.useBasicAuth = environment.useBasicAuth;
  }

  login() {
    this.adalSvc.login();
  }

  logout() {
    sessionStorage.removeItem(AppComponent.LastLoginSessionStorageKey);
    sessionStorage.removeItem(AppComponent.BasicAuthDataSessionStorageKey);
    this.facilityService.terminate();
    if (this.useBasicAuth) {
      this.adalSvc.userInfo.authenticated = false;
      this.router.navigate(['/login']);
    } else {
      this.adalSvc.logOut();
    }
  }

  get authenticated(): boolean {
    return this.adalSvc.userInfo.authenticated;
  }

  get userName(): string {
    if (environment.useBasicAuth) {
      return 'Head of Operations SmartHotel';
    }
    return this.adalSvc.userInfo.profile.name;
  }
}
