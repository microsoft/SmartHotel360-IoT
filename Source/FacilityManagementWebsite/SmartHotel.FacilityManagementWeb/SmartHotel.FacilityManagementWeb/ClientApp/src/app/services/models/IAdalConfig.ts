
import { IAdalEndpoint } from './IAdalEndpoint';
export interface IAdalConfig {
  tenant: string;
  clientId: string;
  endpoints: IAdalEndpoint[];
}
