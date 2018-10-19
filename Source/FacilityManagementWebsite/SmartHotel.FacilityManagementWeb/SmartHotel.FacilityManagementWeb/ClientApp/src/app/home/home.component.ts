import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { IHotel } from '../services/models/IHotel';
import { AdalService } from 'adal-angular4';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {

  constructor(private router: Router,
    private facilityService: FacilityService,
    private adalSvc: AdalService,
    private spinnerService: Ng4LoadingSpinnerService
  ) { }

  hotels = null;

  ngOnInit() {
    this.loadHotels();
  }


  loadHotels() {
    this.spinnerService.show();
    this.facilityService.getHotel().then((data: IHotel[]) => {
      this.hotels = data.sort((a, b) => a.name.localeCompare(b.name));
      this.spinnerService.hide();
    });

  }

  chooseHotel(hotel) {
    this.router.navigate(['/hotel', { id: hotel.id, index: this.hotels.indexOf(hotel) }]);
  }

  getHotelImage(idx) {
    const index = idx >= 2 ? 1 : idx;
    return 'url(/assets/images/h' + index + '.jpg)';
  }
}
