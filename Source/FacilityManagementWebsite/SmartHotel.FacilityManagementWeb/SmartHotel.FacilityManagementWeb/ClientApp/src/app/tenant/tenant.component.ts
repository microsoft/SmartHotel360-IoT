import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { Subscription } from 'rxjs';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';
import { IPushpinLocation, getPushpinLocation } from '../map/IPushPinLocation';
import { environment } from 'src/environments/environment.prod';

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

  public tenantId: string;
  public hotelBrands: ISpace[] = null;
  public hotelGeoLocations: IPushpinLocation[] = [];
  public motionSensorIds: string[] = [];
  public lightSensorIds: string[] = [];
  public tempSensorIds: string[] = [];

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
    console.log(environment.adalConfig);
    const hotelBrands = self.facilityService.getChildSpaces(self.tenantId);
    if (!hotelBrands) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.hotelBrands = hotelBrands;

    // console.log(hotelBrands);

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.hotelBrands, tempAlerts)));

    self.hotelBrands.forEach(brand => {
      brand.childSpaces.forEach(hotel => {
        const pushpinLocation = getPushpinLocation(hotel, brand.friendlyName);
        if (pushpinLocation) {
          self.hotelGeoLocations.push(pushpinLocation);
        }
      });
    });

    for (const brand of self.hotelBrands) {
      const motionSensorIds = self.facilityService.getDescendantSensorIds(brand.id, 'Motion');
      motionSensorIds.forEach(id => {
        if (id != null) {
          self.motionSensorIds.push(id);
        }
      });
      const lightSensorIds = self.facilityService.getDescendantSensorIds(brand.id, 'Light');
      lightSensorIds.forEach(id => {
        if (id != null) {
          self.lightSensorIds.push(id);
        }
      });
      const tempSensorIds = self.facilityService.getDescendantSensorIds(brand.id, 'Temperature');
      tempSensorIds.forEach(id => {
        if (id != null) {
          self.tempSensorIds.push(id);
        }
      });
    }
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
