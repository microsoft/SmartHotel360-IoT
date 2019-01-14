import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';
import { FacilityService } from './services/facility.service';
import { NavigationService } from './services/navigation.service';
import { BusyService } from './services/busy.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public static readonly LastLoginSessionStorageKey: string = 'FacilityService_LastLogin';

  constructor(private adalService: AdalService,
    private facilityService: FacilityService,
    private busyService: BusyService,
    private navigationService: NavigationService) {
    adalService.init(environment.adalConfig);
  }

  public async ngOnInit() {
    this.adalService.handleWindowCallback();
    if (this.adalService.userInfo.authenticated) {
      const lastLoginString = sessionStorage.getItem(AppComponent.LastLoginSessionStorageKey);
      if (lastLoginString) {
        const currentLoginTime = new Date().getTime();
        const lastLoginDate: Date = new Date(lastLoginString);
        const timeDifference = currentLoginTime - lastLoginDate.getTime();
        if (timeDifference < 3000) {
          // duplicate caused by ADAL authentication, stopped execution
          return;
        }
      } else {
        sessionStorage.setItem(AppComponent.LastLoginSessionStorageKey, new Date().toISOString());
      }
      this.busyService.busy();

      this.facilityService.executeWhenInitialized(this, this.navigateToDesiredRoute);
      this.facilityService.initialize();
    }
  }

  private navigateToDesiredRoute(self: AppComponent) {
    try {
      const spaces = self.facilityService.getSpaces();
      if (spaces && spaces.length > 0) {
        if (location.pathname.indexOf(';') < 0) {
          self.navigationService.navigateToTopSpaces(spaces);
        }
      }
    } finally {
      self.busyService.idle();
    }
  }
}
