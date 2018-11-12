import { IThermostat, IMotion, ILight } from './IDevice';
import { IProperty } from './IProperty';

export interface ISpace {
  id: string;
  parentSpaceId: string;
  name: string;
  friendlyName: string;
  type: string;
  typeId: number;
  imagePath: string;
  childSpaces: ISpace[];
  properties: IProperty[];
  thermostat?: IThermostat;
  motion?: IMotion;
  light?: ILight;
}
