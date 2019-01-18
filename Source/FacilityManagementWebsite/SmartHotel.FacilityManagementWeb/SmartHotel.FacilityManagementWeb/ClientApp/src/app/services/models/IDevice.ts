import { ISensor } from './ISensor';

export interface IDevice {
  id: string;
  name: string;
  sensors: ISensor[];
}
