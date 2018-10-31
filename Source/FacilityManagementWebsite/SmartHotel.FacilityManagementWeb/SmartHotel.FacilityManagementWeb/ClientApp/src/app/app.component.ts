import { Component, OnInit } from '@angular/core';
import { AdalService } from 'adal-angular4';
import { environment } from '../environments/environment';
import { FacilityService } from './services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  constructor(private adalService: AdalService,
    private facilityService: FacilityService,
    private spinnerService: Ng4LoadingSpinnerService,
    private router: Router) {
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
        const firstSpace = spaces[0];
        if (firstSpace.type.toLowerCase() === 'hotelbrand') {
          self.router.navigate(['/', { tId: firstSpace.parentSpaceId }]);
        } else if (firstSpace.type.toLowerCase() === 'venue') {
          self.router.navigate(['/hotelbrand', { hbId: firstSpace.parentSpaceId }]);
        } else if (firstSpace.type.toLowerCase() === 'floor') {
          self.router.navigate(['/hotel', { hId: firstSpace.parentSpaceId, hIndex: spaces.indexOf(firstSpace) }]);
        }
      }
    } finally {
      self.spinnerService.hide();
    }
  }
}
