import { IThermostat, IMotion, ILight } from './IDevice';

export interface ISpace {
  id: string;
  parentSpaceId: string;
  name: string;
  friendlyName: string;
  type: string;
  typeId: number;
  childSpaces: ISpace[];
  thermostat?: IThermostat;
  motion?: IMotion;
  light?: ILight;
}
