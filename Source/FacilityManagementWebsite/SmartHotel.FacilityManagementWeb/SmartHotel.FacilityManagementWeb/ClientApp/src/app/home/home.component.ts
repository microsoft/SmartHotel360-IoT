import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { ISpace } from '../services/models/ISpace';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {

  constructor(private router: Router,
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
    self.hotelBrands = self.facilityService.getChildSpaces(self.tenantId);
  }

  chooseHotelBrand(hotelBrand: ISpace) {
    this.router.navigate(['/hotelbrand', { tId: this.tenantId, hbId: hotelBrand.id }]);
  }

  getHotelBrandImage(idx) {
    const index = idx >= 3 ? 3 : idx;
    return 'url(/assets/images/hb' + index + '.jpg)';
  }
}
