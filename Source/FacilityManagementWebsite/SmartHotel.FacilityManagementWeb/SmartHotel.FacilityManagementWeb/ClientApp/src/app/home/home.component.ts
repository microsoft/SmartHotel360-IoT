import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {

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


  loadHotelBrands(self: HomeComponent) {
    const hotelBrands = self.facilityService.getChildSpaces(self.tenantId);
    if (!hotelBrands) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.hotelBrands = hotelBrands;
  }

  chooseHotelBrand(hotelBrand: ISpace) {
    this.navigationService.chooseHotelBrand(this.tenantId, hotelBrand.id);
  }

  getHotelBrandImage(idx) {
    const index = idx >= 3 ? 3 : idx;
    return 'url(/assets/images/hb' + index + '.jpg)';
  }
}
