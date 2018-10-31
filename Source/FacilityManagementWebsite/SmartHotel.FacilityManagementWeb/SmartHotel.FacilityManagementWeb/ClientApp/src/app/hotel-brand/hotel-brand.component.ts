import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { ISpace } from '../services/models/ISpace';

@Component({
  selector: 'app-hotel-brand',
  templateUrl: './hotel-brand.component.html',
  styleUrls: ['./hotel-brand.component.css']
})
export class HotelBrandComponent implements OnInit {

  constructor(private router: Router,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  tenantId: string;
  hotelBrandName: string;
  hotelBrandId: string;
  hotels: ISpace[] = null;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tenantId = params['tId'];
      this.hotelBrandId = params['hbId'];
      this.facilityService.executeWhenInitialized(this, this.loadHotels);
    });
  }


  loadHotels(self: HotelBrandComponent) {
    if (self.tenantId) {
      const hotelBrand = self.facilityService.getSpace(self.tenantId, self.hotelBrandId);
      self.hotelBrandName = hotelBrand.name;
    }

    self.hotels = self.facilityService.getChildSpaces(self.hotelBrandId);
  }

  returnToHome() {
    this.router.navigate(['/', { tId: this.tenantId }]);
  }

  chooseHotel(hotel) {
    const navArgs = {
      hbId: this.hotelBrandId, hbName: this.hotelBrandName,
      hId: hotel.id, hIndex: this.hotels.indexOf(hotel)
    };
    if (this.tenantId) {
      navArgs['tId'] = this.tenantId;
    }
    this.router.navigate(['/hotel', navArgs]);
  }

  getHotelImage(idx) {
    const index = idx >= 2 ? 1 : idx;
    return 'url(/assets/images/h' + index + '.jpg)';
  }

}
