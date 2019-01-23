import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { Subscription } from 'rxjs';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';
import { IPushpinLocation, getPushpinLocation } from '../map/IPushPinLocation';

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
  public sensorIds: string[] = [];

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

    self.hotelBrands.forEach(brand => {
      brand.childSpaces.forEach(hotel => {
        const pushpinLocation = getPushpinLocation(hotel, brand.friendlyName);
        if (pushpinLocation) {
          self.hotelGeoLocations.push(pushpinLocation);
        }
      });
    });

    const sensorIds = self.facilityService.getDescendantSensorIds(self.hotelBrands[0].id);

    // console.log(self.hotelBrands[0]);
    // console.log(`Hotel Brand Id: ${self.hotelBrands[0].id}`);
    // console.log(`Sensors: ${sensorIds}`);

    sensorIds.forEach(id => {
      if (id != null) {
        self.sensorIds.push(id);
      }
    });
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
