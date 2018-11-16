import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';
import { FacilityService } from './services/facility.service';
import { NavigationService } from './services/navigation.service';
import { BusyService } from './services/busy.service';
import { ITempAlert } from './services/models/ITempAlert';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  public static readonly LastLoginSessionStorageKey: string = 'FacilityService_LastLogin';
  private tempAlertsInterval;

  public temperatureAlerts: ITempAlert[];
  public hasAlerts = false;
  public areAlertsOpen = false;

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

  public ngOnDestroy() {
    if (this.tempAlertsInterval) {
      clearInterval(this.tempAlertsInterval);
    }
  }

  private navigateToDesiredRoute(self: AppComponent) {
    try {
      const spaces = self.facilityService.getSpaces();
      if (spaces && spaces.length > 0) {
        if (location.pathname.indexOf(';') < 0) {
          self.navigationService.navigateToTopSpaces(spaces);
        }
        self.setupTimer();
      }
    } finally {
      self.busyService.idle();
    }
  }

  private setupTimer() {
    this.loadTempAlerts();
    this.tempAlertsInterval = setInterval(this.loadTempAlerts.bind(this), environment.sensorDataTimer);
  }

  private async loadTempAlerts() {
    const tempAlerts = await this.facilityService.getTemperatureAlerts();
    this.temperatureAlerts = tempAlerts;
    this.hasAlerts = this.temperatureAlerts && this.temperatureAlerts.length > 0;
  }

  public toggleAlertPanel() {
    this.areAlertsOpen = !this.areAlertsOpen;
  }
}
