import { Component, OnInit, Input } from '@angular/core';
import { NavigationService } from '../services/navigation.service';

@Component({
  selector: 'app-breadcrumb',
  templateUrl: './breadcrumb.component.html',
  styleUrls: ['./breadcrumb.component.css']
})
export class BreadcrumbComponent implements OnInit {

  @Input() public tenantId: string;
  @Input() public hotelBrandId: string;
  @Input() public hotelBrandName: string;
  @Input() public hotelId: string;
  @Input() public hotelIndex: number;
  @Input() public hotelName: string;
  @Input() public floorName: string;

  constructor(private navigationService: NavigationService) { }

  public ngOnInit() {
  }

  public returnToHome() {
    this.navigationService.returnToHome(this.tenantId);
  }

  public returnToHotelBrand() {
    this.navigationService.chooseHotelBrand(this.tenantId, this.hotelBrandId);
  }

  public returnToHotel() {
    this.navigationService.chooseHotel(this.tenantId, this.hotelBrandId, this.hotelBrandName, this.hotelId, this.hotelIndex);
  }
}
