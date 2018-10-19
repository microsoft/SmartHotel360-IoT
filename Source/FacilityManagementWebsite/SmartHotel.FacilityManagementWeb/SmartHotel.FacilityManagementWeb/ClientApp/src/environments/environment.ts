// The file contents for the current environment will overwrite these during build.
// The build system defaults to the dev environment which uses `environment.ts`, but if you do
// `ng build --env=prod` then `environment.prod.ts` will be used instead.
// The list of which env maps to which file can be found in `.angular-cli.json`.

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
  resourceId: '0b07f429-9f4b-4714-9392-cc5e8e80c8b0'
};
