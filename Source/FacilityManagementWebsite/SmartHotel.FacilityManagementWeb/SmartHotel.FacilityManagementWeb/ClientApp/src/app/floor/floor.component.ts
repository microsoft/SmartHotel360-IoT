import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ILight, IThermostat, IMotion } from '../services/models/IDevice';
import { ISensor } from '../services/models/ISensor';
import { environment } from '../../environments/environment';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { IDesired } from '../services/models/IDesired';
import { ChangeContext, Options } from 'ng5-slider';
import { ISpace } from '../services/models/ISpace';
import { not } from '@angular/compiler/src/output/output_ast';

@Component({
  selector: 'app-floor',
  templateUrl: './floor.component.html',
  styleUrls: ['./floor.component.css']
})
export class FloorComponent implements OnInit, OnDestroy {

  constructor(private router: Router,
    private route: ActivatedRoute,
    private facilityService: FacilityService,
    private spinnerService: Ng4LoadingSpinnerService) {
    this.roomsById = new Map<string, ISpace>();
    this.desiredDataByRoomId = new Map<string, IDesired[]>();
    this.sensorDataByRoomId = new Map<string, ISensor[]>();
  }

  tenantId: string;
  hotelBrandId: string;
  hotelBrandName: string;
  hotelName: string;
  hotelId: string;
  hotelIndex: number;
  floorId: string;
  floorName: string;

  rooms: ISpace[] = null;
  roomsById: Map<string, ISpace>;
  desiredDataByRoomId: Map<string, IDesired[]>;
  sensorDataByRoomId: Map<string, ISensor[]>;
  sensorInterval;
  theromstatSliderTimeout;
  lightSliderTimeout;
  isUpdatingSliders = false;

  thermostatSliderOptions: Options = {
    showTicks: false,
    floor: 60,
    ceil: 90,
    enforceStep: true,
    minLimit: 60,
    step: 1,
    maxLimit: 90,
    boundPointerLabels: true,
    hideLimitLabels: true,
    hidePointerLabels: true,
    showSelectionBar: true
  };

  lightSliderOptions: Options = {
    showTicks: false,
    floor: 0,
    ceil: 100,
    enforceStep: true,
    minLimit: 0,
    step: 1,
    maxLimit: 100,
    boundPointerLabels: true,
    hideLimitLabels: true,
    hidePointerLabels: true,
    showSelectionBar: true
  };

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.tenantId = params['tId'];
      this.hotelBrandId = params['hbId'];
      this.hotelBrandName = params['hbName'];
      this.hotelId = params['hId'];
      this.hotelIndex = params['hIndex'];
      this.floorId = params['fId'];
      this.facilityService.executeWhenInitialized(this, this.loadRooms);
    });
  }

  ngOnDestroy() {
    if (this.sensorInterval != null) {
      clearInterval(this.sensorInterval);
    }
  }

  loadRooms(self: FloorComponent) {

    const floor = self.facilityService.getSpace(self.hotelId, self.floorId);
    self.floorName = floor.name;

    self.rooms = self.facilityService.getChildSpaces(self.floorId);
    self.rooms.forEach(room => self.roomsById.set(room.id, room));
    self.loadDesiredData();
    self.setupTimer();
  }

  setupTimer() {
    this.sensorInterval = setInterval(this.loadDesiredData.bind(this), environment.sensorDataTimer);
  }

  loadDesiredData() {
    if (this.isUpdatingSliders) {
      return;
    }

    if (this.rooms != null) {
      this.facilityService.getDesiredData(this.rooms).then((desired: IDesired[]) => {
        if (desired != null && desired.length > 0) {
          desired.forEach(d => {
            let desiredDataForRoom = this.desiredDataByRoomId.get(d.roomId);
            if (!desiredDataForRoom) {
              desiredDataForRoom = [];
              this.desiredDataByRoomId.set(d.roomId, desiredDataForRoom);
            }

            desiredDataForRoom.push(d);
          });
        }
        this.loadSensorData();
      });
    }
  }

  loadSensorData() {
    if (this.rooms != null) {
      this.facilityService.getSensorData(this.rooms).then((sensors: ISensor[]) => {
        if (sensors != null && sensors.length > 0) {
          sensors.forEach(sensor => {
            switch (sensor.sensorDataType) {
              case 'Temperature':
                this.setTemperatureReading(sensor);
                break;
              case 'Motion':
                this.setMotionReading(sensor);
                break;
              case 'Light':
                this.setLightReading(sensor);
                break;
            }
          });
        }
        sensors.forEach(s => {
          let sensorDataForRoom = this.sensorDataByRoomId.get(s.roomId);
          if (!sensorDataForRoom) {
            sensorDataForRoom = [];
            this.sensorDataByRoomId.set(s.roomId, sensorDataForRoom);
          }

          sensorDataForRoom.push(s);
        });
        this.spinnerService.hide();
      });
    }
  }

  setLightReading(sensor: ISensor) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const light: ILight = actual == null ? null :
      { desired: desired * 100.0, actual: actual * 100.0 };

    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.light = light;
    }
  }

  setTemperatureReading(sensor: ISensor) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const temp: IThermostat = actual == null ? null :
      { desired: desired, actual: actual };

    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.thermostat = temp;
    }

  }

  setMotionReading(sensor: ISensor) {

    const motion: IMotion = { isMotion: sensor.sensorReading.toLowerCase() === 'true' };
    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.motion = motion;
    }

  }

  getSensorReading(sensor: ISensor) {

    try {
      return JSON.parse(sensor.sensorReading);
    } catch (ex) { }

    return null;
  }

  getDesiredValue(sensor: ISensor) {

    try {
      let desired: IDesired = null;

      if (this.desiredDataByRoomId !== null) {
        const desiredDatas = this.desiredDataByRoomId.get(sensor.roomId);
        if (desiredDatas) {
          desired = desiredDatas.find((d) => d.sensorId === sensor.sensorId);
        }
      }

      return JSON.parse(desired ? desired.desiredValue : sensor.sensorReading);
    } catch (ex) { }

    return null;
  }

  thermostatSliderValueChange(room: ISpace, changeContext: ChangeContext) {
    console.log(`${room.name} thermostat desired changed: ${changeContext.value}`);

    if (this.theromstatSliderTimeout) {
      clearTimeout(this.theromstatSliderTimeout);
    }

    const sensors = this.sensorDataByRoomId.get(room.id);
    if (!sensors) {
      return;
    }

    const sensor = sensors.find(s => s.sensorDataType === 'Temperature');

    if (!sensor) {
      return;
    }

    const desiredDatas = this.desiredDataByRoomId.get(room.id);
    let desired: IDesired;
    if (desiredDatas) {
      desired = desiredDatas.find((d) => d.sensorId === sensor.sensorId);
    }

    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: room.thermostat.desired.toString()
      };

      if (desiredDatas) {
        desiredDatas.push(desired);
      } else {
        this.desiredDataByRoomId.set(room.id, [desired]);
      }
    } else {
      desired.desiredValue = room.thermostat.desired.toString();
    }

    this.theromstatSliderTimeout = setTimeout((d) => {
      const request = {
        roomId: d.roomId,
        sensorId: d.sensorId,
        desiredValue: d.desiredValue,
        methodName: 'SetDesiredTemperature',
        deviceId: `${room.name.charAt(0).toUpperCase() + room.name.slice(1).replace(' ', '')}Thermostat`
      };
      this.facilityService.setDesiredData(request);
    }, 250, desired);
  }

  lightSliderValueChange(room: ISpace, changeContext: ChangeContext) {
    console.log(`${room.name} light desired changed: ${changeContext.value}`);

    if (this.lightSliderTimeout) {
      clearTimeout(this.lightSliderTimeout);
    }

    const sensors = this.sensorDataByRoomId.get(room.id);
    if (!sensors) {
      return;
    }

    const sensor = sensors.find(s => s.sensorDataType === 'Light');

    if (!sensor) {
      return;
    }

    const desiredDatas = this.desiredDataByRoomId.get(room.id);
    let desired: IDesired;
    if (desiredDatas) {
      desired = desiredDatas.find((d) => d.sensorId === sensor.sensorId);
    }

    const desiredValue = (room.light.desired / 100.0).toString();
    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: desiredValue
      };
      if (desiredDatas) {
        desiredDatas.push(desired);
      } else {
        this.desiredDataByRoomId.set(room.id, [desired]);
      }
    } else {
      desired.desiredValue = desiredValue;
    }

    this.lightSliderTimeout = setTimeout((d) => {
      const request = {
        roomId: d.roomId,
        sensorId: d.sensorId,
        desiredValue: d.desiredValue,
        methodName: 'SetDesiredAmbientLight',
        deviceId: `${room.name.charAt(0).toUpperCase() + room.name.slice(1).replace(' ', '')}Light`
      };
      this.facilityService.setDesiredData(request);
    }, 250, desired);
  }

  sliderChangeBegin() {
    this.isUpdatingSliders = true;
  }

  sliderChangeEnd() {
    this.isUpdatingSliders = false;
  }

  returnToHome() {
    this.router.navigate(['/', { tId: this.tenantId }]);
  }

  returnToHotelBrand() {
    this.router.navigate(['/hotelbrand',
      {
        tId: this.tenantId,
        hbId: this.hotelBrandId
      }]);
  }

  returnToHotel() {
    this.router.navigate(['/hotel', { id: this.hotelId, index: this.hotelIndex }]);
  }
}
