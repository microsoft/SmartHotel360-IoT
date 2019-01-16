import { SimpleAuth } from 'src/app/services/environment.service';

// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  production: false,
  version: 'Development',
  sensorDataTimer: 5000,
  adalConfig: {
    tenant: '{tenantId}',
    clientId: '{clientId}',
    endpoints: {
      '{apiUri}': '{clientId}'
    }
  } as adal.Config,
  apiEndpoint: '{apiEndpoint}',
  resourceId: '0b07f429-9f4b-4714-9392-cc5e8e80c8b0',
  azureMapsKey: '{azureMapsKey}',
  simpleAuth: {
    username: undefined,
    password: undefined,
    apiKey: undefined
  } as SimpleAuth
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
