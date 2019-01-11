import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { Subscription } from 'rxjs';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';

@Component({
  selector: 'app-tenant',
  templateUrl: './tenant.component.html',
  styleUrls: ['./tenant.component.css']
})
export class TenantComponent implements OnInit, OnDestroy {

  private subscriptions: Subscription[] = [];

  constructor(private navigationService: NavigationService,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  tenantId: string;
  hotelBrands: ISpace[] = null;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tenantId = params['tId'];
      this.facilityService.executeWhenInitialized(this, this.loadHotelBrands);
    });
  }

  ngOnDestroy() {
    this.subscriptions.forEach(s => SubscriptionUtilities.tryUnsubscribe(s));
  }

  loadHotelBrands(self: TenantComponent) {
    const hotelBrands = self.facilityService.getChildSpaces(self.tenantId);
    if (!hotelBrands) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.hotelBrands = hotelBrands;

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.hotelBrands, tempAlerts)));
  }

  chooseHotelBrand(hotelBrand: ISpace) {
    this.navigationService.chooseHotelBrand(this.tenantId, hotelBrand.id);
  }

  getHotelBrandImage(hotelBrand: ISpace) {
    return hotelBrand.imagePath;
  }

  temperatureAlertsUpdated(spaces: ISpace[], spaceAlerts: ISpaceAlert[]) {
    if (!spaces) {
      return;
    }
    if (!spaceAlerts) {
      spaces.forEach(space => space.hasAlert = false);
    } else {
      spaces.forEach(space => space.hasAlert = spaceAlerts.some(alert => alert.ancestorSpaceIds.includes(space.id)));
    }
  }
}
