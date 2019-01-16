import { IThermostat, IMotion, ILight } from './IDevice';
import { IProperty } from './IProperty';
import { IPushpinLocation } from 'src/app/map/IPushPinLocation';

export interface ISpace {
  id: string;
  parentSpaceId: string;
  name: string;
  friendlyName: string;
  type: string;
  typeId: number;
  subtype: string;
  subtypeId: number;
  imagePath: string;
  childSpaces: ISpace[];
  properties: IProperty[];
  thermostat?: IThermostat;
  motion?: IMotion;
  light?: ILight;
  hasAlert: boolean;
  alertMessage: string;
}
