import { IRoom } from './IRoom';

export interface IFloor {
  id: string;
  name: string;
  rooms: IRoom[];
}
