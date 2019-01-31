import { Injectable } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { IAdalConfig } from './models/IAdalConfig';
import { IAdalEndpoint } from './models/IAdalEndpoint';

@Injectable()
export class EnvironmentService {

  private httpClient: HttpClient;
  constructor(private handler: HttpBackend) {
    // This approach is used INSTEAD of just getting the HttpClient injected because we do NOT want
    //  any of the HttpInterceptors used since we need information from this config file before we can
    //  initialized authentication.
    this.httpClient = new HttpClient(handler);
  }

  public loadEnvironment() {
    return this.
      httpClient.get('/config/environment')
      .toPromise()
      .then((environmentVariables: { [key: string]: string }) => {
        const adalConfig = JSON.parse(environmentVariables['adalConfig']) as IAdalConfig;
        environment.adalConfig.tenant = adalConfig.tenant;
        environment.adalConfig.clientId = adalConfig.clientId;
        adalConfig.endpoints.forEach((endpoint: IAdalEndpoint) => {
          environment.adalConfig.endpoints[endpoint.url] = endpoint.resourceId;
        });
        environment.apiEndpoint = environmentVariables['apiEndpoint'];
        environment.azureMapsKey = environmentVariables['azureMapsKey'];
        environment.useBasicAuth = environmentVariables['useBasicAuth'] === 'true';
        environment.tsiFqdn = environmentVariables['tsiFqdn'];
        const tsiHowManyDays = environmentVariables['tsiHowManyDays'];
        if (tsiHowManyDays) {
          environment.tsiHowManyDays = +tsiHowManyDays;
        }
      });
  }
}
