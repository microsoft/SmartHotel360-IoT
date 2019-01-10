import { Pipe, PipeTransform } from '@angular/core';
import { FacilityService } from '../services/facility.service';
import { Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Pipe({
  name: 'secure'
})
export class SecurePipe implements PipeTransform {
  constructor(private httpClient: HttpClient,
    private facilityService: FacilityService) { }

  // https://stackoverflow.com/a/49115219/10752002
  transform(url: string) {
    return new Observable((observer) => {
      // This is a tiny blank image
      observer.next('data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==');

      this.httpClient.get(url, {
        headers: this.getHeaders(),
        responseType: 'blob'
      })
        .subscribe(response => {
          const reader = new FileReader();
          reader.readAsDataURL(response);
          reader.onloadend = function () {
            observer.next(reader.result);
          };
        });

      return { unsubscribe() { } };
    });
  }

  private getHeaders() {

    const authToken = this.facilityService.getDigitalTwinsToken();
    return { 'Authorization': `Bearer ${authToken}` };
  }
}
