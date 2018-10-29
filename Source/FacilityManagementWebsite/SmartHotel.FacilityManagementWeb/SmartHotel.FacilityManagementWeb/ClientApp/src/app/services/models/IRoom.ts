import { IThermostat, IMotion, ILight } from './IDevice';

export interface IRoom {
  id: string;
  name: string;
  thermostat?: IThermostat;
  motion?: IMotion;
  light?: ILight;
}
