import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { Subscription } from 'rxjs';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';
import { IPushpinLocation, getPushpinLocation } from '../map/IPushPinLocation';
import { SensorType } from '../services/models/SensorType';

@Component({
  selector: 'app-hotel-brand',
  templateUrl: './hotel-brand.component.html',
  styleUrls: ['./hotel-brand.component.css']
})
export class HotelBrandComponent implements OnInit, OnDestroy {

  private subscriptions: Subscription[] = [];

  constructor(private navigationService: NavigationService,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  @ViewChild('breadcumbs') private breadcrumbs: BreadcrumbComponent;

  public tenantId: string;
  public hotelBrandName: string;
  public hotelBrandId: string;
  public hotels: ISpace[] = null;
  public hotelGeoLocations: IPushpinLocation[] = [];
  public motionSensorIds: string[] = [];
  public lightSensorIds: string[] = [];
  public tempSensorIds: string[] = [];

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tenantId = params['tId'];
      this.hotelBrandId = params['hbId'];
      this.facilityService.executeWhenInitialized(this, this.loadHotels);
    });
  }

  ngOnDestroy() {
    this.subscriptions.forEach(s => SubscriptionUtilities.tryUnsubscribe(s));
  }

  loadHotels(self: HotelBrandComponent) {
    if (self.tenantId) {
      const hotelBrand = self.facilityService.getSpace(self.tenantId, self.hotelBrandId);
      if (!hotelBrand) {
        self.breadcrumbs.returnToHome();
        return;
      }
      self.hotelBrandName = hotelBrand.friendlyName;
    }

    const hotels = self.facilityService.getChildSpaces(self.hotelBrandId);
    if (!hotels) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.hotels = hotels;

    self.filterSensorIds(hotels);

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.hotels, tempAlerts)));

    self.hotels.forEach(hotel => {
      const pushpinLocation = getPushpinLocation(hotel);
      if (pushpinLocation) {
        self.hotelGeoLocations.push(pushpinLocation);
      }
    });
  }

  private filterSensorIds(hotels: ISpace[]) {
    for (const hotel of hotels) {
      const motionSensorIds = this.facilityService.getDescendantSensorIds(hotel.id, SensorType.Motion);
      motionSensorIds.forEach(id => {
        if (id) {
          this.motionSensorIds.push(id);
        }
      });
      const lightSensorIds = this.facilityService.getDescendantSensorIds(hotel.id, SensorType.Light);
      lightSensorIds.forEach(id => {
        if (id) {
          this.lightSensorIds.push(id);
        }
      });
      const tempSensorIds = this.facilityService.getDescendantSensorIds(hotel.id, SensorType.Temperature);
      tempSensorIds.forEach(id => {
        if (id) {
          this.tempSensorIds.push(id);
        }
      });
    }
  }

  chooseHotel(hotel: ISpace) {
    this.navigationService.chooseHotel(this.tenantId, this.hotelBrandId, this.hotelBrandName, hotel.id, this.hotels.indexOf(hotel));
  }

  getHotelImage(hotel: ISpace) {
    return hotel.imagePath;
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
