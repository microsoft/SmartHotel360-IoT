import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { IHotel } from './models/IHotel';
import { AdalService } from 'adal-angular4';
import { IFloor } from './models/IFloor';
import { IDesired } from './models/IDesired';

@Injectable()
export class FacilityService {

  constructor(
    private http: HttpClient,
    private adalSvc: AdalService
  ) { }

  public getHotel() {

    const promise = new Promise((resolve, reject) => {

      if (sessionStorage['hotels']) {
        console.log('loading hotels from cached');
        resolve(JSON.parse(sessionStorage['hotels']));
      } else {
        this.adalSvc.acquireToken(`${environment.resourceId}`)
          .toPromise()
          .then(
            token => {
              this.http.get<IHotel[]>(this.getEndpoint('spaces'), { headers: { 'azure_token': token } }
              ).toPromise().then(data => {
                sessionStorage['hotels'] = JSON.stringify(data);
                resolve(data);
              });
            }
          );
      }
    });

    return promise;

  }

  public getSensorData(floor: IFloor) {
    const promise = new Promise((resolve, reject) => {
      const roomIds: string[] = [];
      floor.rooms.forEach(room => roomIds.push(room.id));
      this.http.get(this.getEndpoint('sensordata'), { params: { 'roomIds': roomIds } }
      ).toPromise().then(data => {
        console.log(data);
        resolve(data);
      });
    });

    return promise;

  }

  public getDesiredData(floor: IFloor) {
    const promise = new Promise((resolve, reject) => {
      const roomIds: string[] = [];
      floor.rooms.forEach(room => roomIds.push(room.id));
      this.http.get(this.getEndpoint('desireddata'), { params: { 'roomIds': roomIds } }
      ).toPromise().then(data => {
        console.log(data);
        resolve(data);
      });
    });

    return promise;
  }

  public setDesiredData(desired) {
    const promise = new Promise((resolve, reject) => {
      const httpOptions = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        })
      };
      this.http.post<IDesired>(this.getEndpoint('desireddata'), JSON.stringify(desired), httpOptions).toPromise()
        .then(data => {
          console.log(data);
          resolve(data);
        })
        .catch(err => {
          console.log(err);
          reject(err);
        });
    });

    return promise;
  }

  private getEndpoint(path: string): string {
    let endPoint = environment.apiEndpoint;

    if (endPoint.substr(-1) !== '/') {
      endPoint += '/';
    }

    return endPoint + path;
  }
}
