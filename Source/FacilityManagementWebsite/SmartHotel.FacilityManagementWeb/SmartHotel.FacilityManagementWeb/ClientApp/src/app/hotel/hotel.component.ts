import { Component, OnInit, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { IHotel } from '../services/models/IHotel';
import { FacilityService } from '../services/facility.service';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';

@Component({
  selector: 'app-hotel',
  templateUrl: './hotel.component.html',
  styleUrls: ['./hotel.component.css']
})
export class HotelComponent implements OnInit {

  @Input() hotelIndex: number;

  constructor(private router: Router,
    private route: ActivatedRoute,
    private facilityService: FacilityService,
    private spinnerService: Ng4LoadingSpinnerService) {

    this.route.params.subscribe(params => {
      this.hotelId = params["id"];
      this.hotelIndex = params["index"];
      this.loadFloors();
    });
  }

  floors = null;
  hotelId;
  hotel = null;

  ngOnInit() {
  }

  loadFloors() {
    this.spinnerService.show();
    this.facilityService.getHotel().then((data: IHotel[]) => {
      let hotels = data;

      this.hotel = hotels.find(hotel => hotel.id == this.hotelId);
      if (this.hotel != null) {
        const fakeData = this.hotel.floors;
        this.hotel.floors.forEach(d => {
          fakeData.push(d);
          fakeData.push(d);
          fakeData.push(d);
          fakeData.push(d);
        });
        this.floors = fakeData.sort((a, b) => { return a.name < b.name ? -1 : 1; });
      }
      this.spinnerService.hide();
    },
      (err) => {
        console.log(err);
        this.spinnerService.hide();
      }
    );

  }

  chooseFloor(floor) {
    this.router.navigate(['/floor', { hotelId: this.hotelId, floorId: floor.id }]);
  }

  returnToHome() {
    this.router.navigate(["/"]);
  }

  getFloorImage(idx) {
    const index = idx > 3 ? 3 : idx;
    return 'url(/assets/images/h' + this.hotelIndex + 'f' + index + '.jpg)';
  }
}
