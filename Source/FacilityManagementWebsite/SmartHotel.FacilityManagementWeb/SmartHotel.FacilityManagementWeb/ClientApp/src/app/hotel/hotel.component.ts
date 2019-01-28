import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';
import { Subscription } from 'rxjs';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { IPushpinLocation, getPushpinLocation } from '../map/IPushPinLocation';

@Component({
  selector: 'app-hotel',
  templateUrl: './hotel.component.html',
  styleUrls: ['./hotel.component.css']
})
export class HotelComponent implements OnInit, OnDestroy {

  private subscriptions: Subscription[] = [];

  constructor(private navigationService: NavigationService,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  @ViewChild('breadcumbs') private breadcrumbs: BreadcrumbComponent;

  public tenantId: string;
  public hotelBrandId: string;
  public hotelBrandName: string;
  public hotelName: string;
  public hotelId: string;
  public hotelIndex: number;
  public floors: ISpace[] = null;
  public hotelGeoLocations: IPushpinLocation[] = [];
  public motionSensorIds: string[] = [];
  public lightSensorIds: string[] = [];
  public tempSensorIds: string[] = [];


  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tenantId = params['tId'];
      this.hotelBrandId = params['hbId'];
      this.hotelBrandName = params['hbName'];
      this.hotelId = params['hId'];
      this.hotelIndex = params['hIndex'];

      this.facilityService.executeWhenInitialized(this, this.loadFloors);
    });
  }

  ngOnDestroy() {
    this.subscriptions.forEach(s => SubscriptionUtilities.tryUnsubscribe(s));
  }

  loadFloors(self: HotelComponent) {
    if (self.hotelBrandId) {

      const hotel = self.facilityService.getSpace(self.hotelBrandId, self.hotelId);
      if (!hotel) {
        self.breadcrumbs.returnToHotelBrand();
        return;
      }
      self.hotelName = hotel.friendlyName;

      const pushpinLocation = getPushpinLocation(hotel);
      if (pushpinLocation) {
        self.hotelGeoLocations.push(pushpinLocation);
      }
    }

    const floors = self.facilityService.getChildSpaces(self.hotelId);
    if (!floors) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.floors = floors;
    self.filterSensorIds(floors);

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.floors, tempAlerts)));
  }

  private filterSensorIds(floors: ISpace[]) {

      for (const floor of floors) {
      const motionSensorIds = this.facilityService.getDescendantSensorIds(floor.id, 'Motion');
      motionSensorIds.forEach(id => {
        if (id) {
          this.motionSensorIds.push(id);
        }
      });
      const lightSensorIds = this.facilityService.getDescendantSensorIds(floor.id, 'Light');
      lightSensorIds.forEach(id => {
        if (id) {
          this.lightSensorIds.push(id);
        }
      });
      const tempSensorIds = this.facilityService.getDescendantSensorIds(floor.id, 'Temperature');
      tempSensorIds.forEach(id => {
        if (id) {
          this.tempSensorIds.push(id);
        }
      });
    }
  }

  chooseFloor(floor: ISpace) {
    this.navigationService
      .chooseFloor(this.tenantId, this.hotelBrandId, this.hotelBrandName, this.hotelId, this.hotelIndex, this.hotelName, floor.id);
  }

  getFloorImage(floor: ISpace) {
    return floor.imagePath;
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
