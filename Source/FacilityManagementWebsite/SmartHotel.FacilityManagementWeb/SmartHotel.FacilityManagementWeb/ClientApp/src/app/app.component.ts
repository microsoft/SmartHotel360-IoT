import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';
import { FacilityService } from './services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { Params, Router } from '@angular/router';
import { NavigationService } from './services/navigation.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  private params: Params;

  constructor(private adalService: AdalService,
    private facilityService: FacilityService,
    private spinnerService: Ng4LoadingSpinnerService,
    private navigationService: NavigationService) {
    adalService.init(environment.adalConfig);
  }

  public async ngOnInit() {
    this.adalService.handleWindowCallback();
    if (this.adalService.userInfo.authenticated) {
      this.spinnerService.show();

      this.facilityService.executeWhenInitialized(this, this.navigateToDesiredRoute);
      this.facilityService.initialize();
    }
  }

  private navigateToDesiredRoute(self: AppComponent) {
    try {
      const spaces = self.facilityService.getSpaces();
      if (spaces && spaces.length > 0) {
        if (location.pathname.indexOf(';') < 0) {
          const firstSpace = spaces[0];
          if (firstSpace.type.toLowerCase() === 'hotelbrand') {
            self.navigationService.returnToHome(firstSpace.parentSpaceId);
          } else if (firstSpace.type.toLowerCase() === 'venue') {
            self.navigationService.chooseHotelBrand(undefined, firstSpace.parentSpaceId);
          } else if (firstSpace.type.toLowerCase() === 'floor') {
            self.navigationService.chooseHotel(undefined, undefined, undefined, firstSpace.parentSpaceId, spaces.indexOf(firstSpace));
          }
        }
      }
    } finally {
      self.spinnerService.hide();
    }
  }
}
