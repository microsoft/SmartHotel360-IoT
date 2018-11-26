import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { ISpace } from './models/ISpace';

@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  constructor(private router: Router) { }

  public navigateToTopSpaces(spaces: ISpace[]) {
    const firstSpace = spaces[0];
    if (firstSpace.type.toLowerCase() === 'hotelbrand') {
      this.returnToHome(firstSpace.parentSpaceId);
    } else if (firstSpace.type.toLowerCase() === 'hotel') {
      this.chooseHotelBrand(undefined, firstSpace.parentSpaceId);
    } else if (firstSpace.type.toLowerCase() === 'floor') {
      this.chooseHotel(undefined, undefined, undefined, firstSpace.parentSpaceId, spaces.indexOf(firstSpace));
    }
  }

  public returnToHome(tenantId: string) {
    const navArgs = this.createNavArgs(tenantId);
    this.router.navigate(['/tenant', navArgs]);
  }

  public chooseHotelBrand(tenantId: string, hotelBrandId: string) {
    const navArgs = this.createNavArgs(tenantId, hotelBrandId);
    this.router.navigate(['/hotelbrand', navArgs]);
  }

  public chooseHotel(tenantId: string, hotelBrandId: string, hotelBrandName: string, hotelId: string, hotelIndex: number) {
    const navArgs = this.createNavArgs(tenantId, hotelBrandId, hotelBrandName, hotelId, hotelIndex);
    this.router.navigate(['/hotel', navArgs]);
  }

  public chooseFloor(tenantId: string, hotelBrandId: string, hotelBrandName: string,
    hotelId: string, hotelIndex: number, hotelName: string, floorId: string) {
    const navArgs = this.createNavArgs(tenantId, hotelBrandId, hotelBrandName, hotelId, hotelIndex, hotelName, floorId);
    this.router.navigate(['/floor', navArgs]);
  }

  private createNavArgs(tenantId?: string,
    hotelBrandId?: string, hotelBrandName?: string,
    hotelId?: string, hotelIndex?: number, hotelName?: string,
    floorId?: string): any {

    const navArgs = {};
    if (tenantId) {
      navArgs['tId'] = tenantId;
    }

    if (hotelBrandId) {
      navArgs['hbId'] = hotelBrandId;
    }

    if (hotelBrandName) {
      navArgs['hbName'] = hotelBrandName;
    }

    if (hotelId) {
      navArgs['hId'] = hotelId;
    }

    if (hotelIndex >= 0) {
      navArgs['hIndex'] = hotelIndex;
    }

    if (hotelName) {
      navArgs['hName'] = hotelName;
    }

    if (floorId) {
      navArgs['fId'] = floorId;
    }

    return navArgs;
  }
}
