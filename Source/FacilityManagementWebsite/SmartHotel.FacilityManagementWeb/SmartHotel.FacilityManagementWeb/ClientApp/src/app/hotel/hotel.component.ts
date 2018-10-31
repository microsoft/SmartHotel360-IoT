import { Component, OnInit, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { ISpace } from '../services/models/ISpace';

@Component({
  selector: 'app-hotel',
  templateUrl: './hotel.component.html',
  styleUrls: ['./hotel.component.css']
})
export class HotelComponent implements OnInit {
  constructor(private router: Router,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

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
    this.router.navigate(['/',
      {
        tId: this.tenantId
      }]);
  }

  returnToHotelBrand() {
    this.router.navigate(['/hotelbrand',
      {
        tId: this.tenantId,
        hbId: this.hotelBrandId
      }]);
  }

  chooseFloor(floor) {
    const navArgs = {
      hId: this.hotelId, hIndex: this.hotelIndex,
      fId: floor.id
    };
    if (this.tenantId) {
      navArgs['tId'] = this.tenantId;
    }
    if (this.hotelBrandId) {
      navArgs['hbId'] = this.hotelBrandId;
      navArgs['hbName'] = this.hotelBrandName;
    }
    this.router.navigate(['/floor', navArgs]);
  }

  getFloorImage(idx) {
    const index = idx > 3 ? 3 : idx;
    return 'url(/assets/images/h' + this.hotelIndex + 'f' + index + '.jpg)';
  }
}
