import { Component, OnInit, AfterViewInit, HostListener } from '@angular/core';
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
export class AppComponent implements OnInit, AfterViewInit {
  public static readonly LastLoginSessionStorageKey: string = 'FacilityService_LastLogin';
  public static readonly BasicAuthDataSessionStorageKey: string = 'FacilityService_BasicAuthData';

  constructor(private adalService: AdalService,
    private facilityService: FacilityService,
    private busyService: BusyService,
    private navigationService: NavigationService) {
    adalService.init(environment.adalConfig);
  }

  public async ngOnInit() {
    if (environment.useBasicAuth) {
      this.facilityService.executeWhenInitialized(this, this.navigateToDesiredRoute);
    } else {
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
  }

  public ngAfterViewInit() {
    this.handleWindowSize(window.innerWidth, window.innerHeight);
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

  @HostListener('window:resize', ['$event'])
  onresize(event) {
    const width: number = event.target.innerWidth;
    const height: number = event.target.innerHeight;
    this.handleWindowSize(width, height);
  }

  private handleWindowSize(width: number, height: number) {
    // Hack to ensure that when the browser viewport is >= 1920x938 no scrollbar shows
    // but the scrollbars show when less than that. hack because the Floorplan svg files cause scrollbars because of extra whitespace
    const body = document.body;
    if (width < 1920 || height < 931) {
      body.style.overflow = 'visible';
    } else {
      window.scroll(0, 0);
      body.style.overflow = 'hidden';
    }
  }
}
