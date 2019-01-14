import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';
import { Subscription } from 'rxjs';
import { ISpaceAlert } from '../services/models/ISpaceAlert';

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
    }

    const floors = self.facilityService.getChildSpaces(self.hotelId);
    if (!floors) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.floors = floors;

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.floors, tempAlerts)));
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
