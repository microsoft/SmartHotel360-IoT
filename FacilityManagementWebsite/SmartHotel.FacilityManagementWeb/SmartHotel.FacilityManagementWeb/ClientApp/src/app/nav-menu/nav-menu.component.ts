import { Component } from '@angular/core';
import { AdalService } from 'adal-angular4';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {

  constructor(private adalSvc: AdalService) { }

  login() {
    this.adalSvc.login();
  }

  logout() {
    sessionStorage.clear();
    this.adalSvc.logOut();
  }

  get authenticated(): boolean {
    return this.adalSvc.userInfo.authenticated;
  }

  get userName(): string {
    return this.adalSvc.userInfo.profile.name;
  }
}
