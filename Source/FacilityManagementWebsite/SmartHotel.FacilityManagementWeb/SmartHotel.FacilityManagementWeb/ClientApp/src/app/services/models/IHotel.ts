import { IFloor } from "./IFloor";

export interface IHotel {
  id: string;
  name: string;
  floors: IFloor[];
}
