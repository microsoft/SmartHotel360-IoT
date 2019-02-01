import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AdalService } from 'adal-angular4';
import { IDesired } from './models/IDesired';
import { ISpace } from './models/ISpace';
import { ISpaceAlert } from './models/ISpaceAlert';
import { BehaviorSubject, Observable, forkJoin } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ISensor } from './models/ISensor';

class InitializationCallbackContainer {
  constructor(requester: any, callback: (requester: any) => void) {
    this.Requester = requester;
    this.Callback = callback;
  }

  public Requester: any;
  public Callback: (requester: any) => void;
}

@Injectable({
  providedIn: 'root'
})
export class FacilityService {

  private notInitializedError = new Error('FacilityService is not initialized. ' +
    'Be sure to use the \"executeWhenInitialized\" function when calling this service.');

  private isInitialized = false;
  private spaces: ISpace[] = null;
  private spacesByParentId: Map<string, ISpace[]>;
  private callbacksToExecuteWhenInitialized: InitializationCallbackContainer[] = [];
  private dtToken: string;
  private tsiToken: string;
  private temperatureAlertsSubject: BehaviorSubject<ISpaceAlert[]> = new BehaviorSubject<ISpaceAlert[]>(null);
  private temperatureAlertsObservable: Observable<ISpaceAlert[]>;
  private alertsTimerInterval;

  constructor(
    private http: HttpClient,
    private adalSvc: AdalService) {
    this.spacesByParentId = new Map<string, ISpace[]>();

    this.temperatureAlertsObservable = this.temperatureAlertsSubject.asObservable();
  }

  public async initialize() {
    try {
      await this.updateTsiToken();
      await this.updateDtToken()
        .then(() => {
          this.http.get<ISpace[]>(this.getEndpoint('spaces'), { headers: { 'adt_token': this.dtToken } }
          ).toPromise().then(data => {
            this.spaces = data;
            this.updateSpacesByParentIdMap(this.spaces);
            this.startAlertsTimer();
            this.isInitialized = true;
            this.onInitialized();
          });
        });
    } catch (error) {
      console.error('Failed to initialize and load spaces.');
      console.error(error);
    }
  }

  public basicAuthLogin(basicAuthHeader: string) {
    return this.http.get<string>(this.getEndpoint('auth'), {
      headers: { Authorization: basicAuthHeader }
    })
      .pipe(switchMap(() => {
        return forkJoin(this.basicAuthRetrieveDtToken(basicAuthHeader), this.basicAuthRetreiveTsiToken(basicAuthHeader));
      }));
  }

  private basicAuthRetrieveDtToken(basicAuthHeader: string) {
    return this.http.get<string>(this.getEndpoint('auth/getdttoken'), {
      headers: { Authorization: basicAuthHeader },
      responseType: 'text' as 'json'
    })
      .pipe(map((token: string) => {
        this.dtToken = token;
      }));
  }

  private basicAuthRetreiveTsiToken(basicAuthHeader: string) {
    return this.http.get<string>(this.getEndpoint('auth/gettsitoken'), {
      headers: { Authorization: basicAuthHeader },
      responseType: 'text' as 'json'
    })
      .pipe(map((token: string) => {
        this.tsiToken = token;
      }));
  }

  public terminate() {
    this.spaces = undefined;
    this.spacesByParentId.clear();
    this.temperatureAlertsSubject.next(null);
    clearInterval(this.alertsTimerInterval);
    this.isInitialized = false;
  }

  public getSpaces(): ISpace[] {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    return this.spaces;
  }

  public getDigitalTwinsToken(): string {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    return this.dtToken;
  }

  public getTimeSeriesInsightsToken(): string {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    return this.tsiToken;
  }

  public getSpace(parentSpaceId: string, spaceId: string): ISpace {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    const childSpaces = this.spacesByParentId.get(parentSpaceId);
    if (childSpaces) {
      return childSpaces.find(space => space.id === spaceId);
    }
  }

  public getChildSpaces(parentSpaceId: string): ISpace[] {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    return this.spacesByParentId.get(parentSpaceId);
  }

  public getSensorData(rooms: ISpace[]) {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    const promise = new Promise((resolve, reject) => {
      const roomIds: string[] = [];
      rooms.forEach(room => roomIds.push(room.id));
      this.http.get(this.getEndpoint('sensordata'), { params: { 'roomIds': roomIds } }
      ).toPromise().then(data => {
        console.log(data);
        resolve(data);
      });
    });

    return promise;

  }

  public getDesiredData(rooms: ISpace[]) {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    const promise = new Promise((resolve, reject) => {
      const roomIds: string[] = [];
      rooms.forEach(room => roomIds.push(room.id));
      this.http.get(this.getEndpoint('desireddata'), { params: { 'roomIds': roomIds } }
      ).toPromise().then(data => {
        console.log(data);
        resolve(data);
      });
    });

    return promise;
  }

  public setDesiredData(desired) {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

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

  public getTemperatureAlerts(): Observable<ISpaceAlert[]> {
    if (!this.isInitialized) {
      throw this.notInitializedError;
    }

    return this.temperatureAlertsObservable;
  }

  public getDescendantSensorIds(topSpaceId: string, dataType: string): string[] {
    const childSpaces = this.spacesByParentId.get(topSpaceId);
    if (childSpaces) {
      let descendantSensorIds: string[] = [];
      childSpaces.forEach(childSpace => {
        if (childSpace.devices) {
          childSpace.devices.forEach(device => {
            if (device.sensors) {
              descendantSensorIds = descendantSensorIds
                .concat(device.sensors.filter(s => s.dataType === dataType).map(sensor => sensor.id));
            }
          });
        }

        descendantSensorIds = descendantSensorIds.concat(this.getDescendantSensorIds(childSpace.id, dataType));
      });
      return descendantSensorIds;
    } else {
      return [];
    }
  }



  public executeWhenInitialized(requester: any, callback: (requester: any) => void): boolean {
    if (this.isInitialized) {
      callback(requester);
      return true;
    }

    this.callbacksToExecuteWhenInitialized.push(new InitializationCallbackContainer(requester, callback));
    return false;
  }

  private async updateDtToken(): Promise<void> {
    if (environment.useBasicAuth) {
      return Promise.resolve();
    }
    await this.adalSvc.acquireToken(`${environment.resourceId}`)
      .toPromise()
      .then(token => {
        this.dtToken = token;
      });
  }

  private async updateTsiToken(): Promise<void> {
    if (environment.useBasicAuth) {
      return Promise.resolve();
    }
    await this.http.get<string>(this.getEndpoint('auth/gettsitoken'), {
      responseType: 'text' as 'json'
    })
      .toPromise()
      .then(token => {
        this.tsiToken = token;
      });
  }

  private startAlertsTimer() {
    this.loadAlerts()
      .then(() => {
        this.alertsTimerInterval = setInterval(this.loadAlerts.bind(this), environment.sensorDataTimer);
      });
  }

  private async loadAlerts() {
    await this.updateDtToken();
    const alerts = await this.http.get(this.getEndpoint('spaces/temperaturealerts'), { headers: { 'adt_token': this.dtToken } }
    ).toPromise() as { [spaceId: string]: ISpaceAlert };

    if (alerts !== undefined && alerts !== null) {
      const keys = Object.keys(alerts);
      const tempAlerts: ISpaceAlert[] = [];

      keys.forEach(k => tempAlerts.push(alerts[k]));
      this.temperatureAlertsSubject.next(tempAlerts);
    } else {
      this.temperatureAlertsSubject.next(null);
    }
  }

  private getEndpoint(path: string): string {
    let endPoint = environment.apiEndpoint;

    if (endPoint.substr(-1) !== '/') {
      endPoint += '/';
    }

    return endPoint + path;
  }

  private updateSpacesByParentIdMap(spaces: ISpace[]) {
    if (!spaces || spaces.length === 0) {
      return;
    }

    this.spacesByParentId.set(spaces[0].parentSpaceId, spaces.sort(this.getSpacesSortResult));

    spaces.forEach(space => {
      if (space.properties) {
        const imagePathProperty = space.properties.find(p => p.name === 'ImagePath');
        if (imagePathProperty) {
          space.imagePath = imagePathProperty.value;
        }
        const detailedImagePathProperty = space.properties.find(p => p.name === 'DetailedImagePath');
        if (detailedImagePathProperty) {
          space.detailedImagePath = detailedImagePathProperty.value;
        }
      }
      this.updateSpacesByParentIdMap(space.childSpaces);
    });
  }

  private getSpacesSortResult(a: ISpace, b: ISpace) {
    if (a.properties && b.properties) {
      const aDisplayOrderProperty = a.properties.find(p => p.name === 'DisplayOrder');
      const bDisplayOrderProperty = b.properties.find(p => p.name === 'DisplayOrder');
      if (aDisplayOrderProperty && bDisplayOrderProperty) {
        const aDisplayOrder = +aDisplayOrderProperty.value;
        const bDisplayOrder = +bDisplayOrderProperty.value;
        if (aDisplayOrder > bDisplayOrder) {
          return 1;
        }
        if (aDisplayOrder < bDisplayOrder) {
          return -1;
        }
        return 0;
      }
    }
    return a.name.localeCompare(b.name);
  }

  private onInitialized() {
    this.callbacksToExecuteWhenInitialized.forEach((callbackContainer: InitializationCallbackContainer) => {
      try {
        callbackContainer.Callback(callbackContainer.Requester);
      } catch (err) {
        console.error(err);
        let requesterName = '';
        if (callbackContainer.Requester.constructor && callbackContainer.Requester.constructor.name) {
          requesterName = callbackContainer.Requester.constructor.name;
        }
        console.error(`[FacilityService] Failure executing initialization callback for requester: ${requesterName} `);
      }
    });
  }
}
