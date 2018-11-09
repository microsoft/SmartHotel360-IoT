import { Component } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { FacilityService } from '../services/facility.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {

  constructor(private adalSvc: AdalService,
    private facilityService: FacilityService) { }

  login() {
    this.adalSvc.login();
  }

  logout() {
    this.facilityService.terminate();
    this.adalSvc.logOut();
  }

  get authenticated(): boolean {
    return this.adalSvc.userInfo.authenticated;
  }

  get userName(): string {
    return this.adalSvc.userInfo.profile.name;
  }
}
