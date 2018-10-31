import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';

@Component({
  selector: 'app-hotel',
  templateUrl: './hotel.component.html',
  styleUrls: ['./hotel.component.css']
})
export class HotelComponent implements OnInit {
  constructor(private navigationService: NavigationService,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  breadcrumbsHtml: string;

  tenantId: string;
  hotelBrandId: string;
  hotelBrandName: string;
  hotelName: string;
  hotelId: string;
  hotelIndex: number;
  floors: ISpace[] = null;

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

  loadFloors(self: HotelComponent) {
    if (self.hotelBrandId) {
      const hotel = self.facilityService.getSpace(self.hotelBrandId, self.hotelId);
      self.hotelName = hotel.name;
    }

    self.floors = self.facilityService.getChildSpaces(self.hotelId);
  }

  returnToHome() {
    this.navigationService.returnToHome(this.tenantId);
  }

  returnToHotelBrand() {
    this.navigationService.chooseHotelBrand(this.tenantId, this.hotelBrandId);
  }

  chooseFloor(floor) {
    this.navigationService
      .chooseFloor(this.tenantId, this.hotelBrandId, this.hotelBrandName, this.hotelId, this.hotelIndex, this.hotelName, floor.id);
  }

  getFloorImage(idx) {
    const index = idx > 3 ? 3 : idx;
    return 'url(/assets/images/h' + this.hotelIndex + 'f' + index + '.jpg)';
  }
}
