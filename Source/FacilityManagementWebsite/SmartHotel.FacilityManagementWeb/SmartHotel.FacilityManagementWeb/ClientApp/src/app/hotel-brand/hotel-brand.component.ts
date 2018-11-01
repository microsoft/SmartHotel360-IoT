import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ISpace } from '../services/models/ISpace';
import { NavigationService } from '../services/navigation.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-hotel-brand',
  templateUrl: './hotel-brand.component.html',
  styleUrls: ['./hotel-brand.component.css']
})
export class HotelBrandComponent implements OnInit {

  constructor(private navigationService: NavigationService,
    private route: ActivatedRoute,
    private facilityService: FacilityService) {
  }

  @ViewChild('breadcumbs') private breadcrumbs: BreadcrumbComponent;

  public tenantId: string;
  public hotelBrandName: string;
  public hotelBrandId: string;
  public hotels: ISpace[] = null;

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
      if (!hotelBrand) {
        self.breadcrumbs.returnToHome();
        return;
      }
      self.hotelBrandName = hotelBrand.name;
    }

    const hotels = self.facilityService.getChildSpaces(self.hotelBrandId);
    if (!hotels) {
      self.navigationService.navigateToTopSpaces(self.facilityService.getSpaces());
      return;
    }
    self.hotels = hotels;
  }

  chooseHotel(hotel) {
    this.navigationService.chooseHotel(this.tenantId, this.hotelBrandId, this.hotelBrandName, hotel.id, this.hotels.indexOf(hotel));
  }

  getHotelImage(idx) {
    const index = idx >= 2 ? 1 : idx;
    return 'url(/assets/images/h' + index + '.jpg)';
  }

}
