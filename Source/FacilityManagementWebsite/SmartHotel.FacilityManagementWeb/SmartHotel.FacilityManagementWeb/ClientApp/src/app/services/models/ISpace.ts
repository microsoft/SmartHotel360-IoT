import { IThermostat, IMotion, ILight } from './IDeviceValues';
import { IProperty } from './IProperty';
import { IPushpinLocation } from 'src/app/map/IPushPinLocation';
import { IDevice } from './IDevice';

export interface ISpace {
  id: string;
  parentSpaceId: string;
  name: string;
  friendlyName: string;
  number?: number;
  type: string;
  typeId: number;
  subtype: string;
  subtypeId: number;
  imagePath: string;
  detailedImagePath: string;
  childSpaces: ISpace[];
  properties: IProperty[];
  devices: IDevice[];
  thermostat?: IThermostat;
  motion?: IMotion;
  light?: ILight;
  hasAlert: boolean;
  alertMessage: string;
}
