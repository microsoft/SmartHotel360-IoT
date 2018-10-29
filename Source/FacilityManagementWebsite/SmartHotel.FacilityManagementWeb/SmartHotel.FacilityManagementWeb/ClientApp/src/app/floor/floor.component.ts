import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { IHotel } from '../services/models/IHotel';
import { IFloor } from '../services/models/IFloor';
import { IRoom } from '../services/models/IRoom';
import { ILight, IThermostat, IMotion } from '../services/models/IDevice';
import { ISensor } from '../services/models/ISensor';
import { environment } from '../../environments/environment';
import { Ng4LoadingSpinnerService } from 'ng4-loading-spinner';
import { IDesired } from '../services/models/IDesired';
import { ChangeContext, Options } from 'ng5-slider';

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

    this.route.params.subscribe(params => {
      this.hotelId = params['hotelId'];
      this.floorId = params['floorId'];

      this.loadRooms();
    });
  }

  hotelId;
  hotelIndex: number;
  hotel: IHotel = null;
  floorId;
  floor: IFloor = null;
  rooms: IRoom[] = null;
  desiredData: IDesired[] = [];
  sensorData: ISensor[] = [];
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

  }

  ngOnDestroy() {
    if (this.sensorInterval != null) {
      clearInterval(this.sensorInterval);
    }
  }

  loadRooms() {
    this.spinnerService.show();

    this.facilityService.getHotel().then((data: IHotel[]) => {
      const hotels = data.sort((a, b) => a.name.localeCompare(b.name));
      this.hotel = hotels.find(hotel => hotel.id === this.hotelId);
      this.hotelIndex = data.indexOf(this.hotel);

      if (this.hotel != null) {
        this.floor = this.hotel.floors.find(floor => floor.id === this.floorId);

        if (this.floor != null) {
          const fakeData = this.floor.rooms;
          this.floor.rooms.forEach(d => {
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
            fakeData.push(d);
          });

          this.rooms = fakeData.sort((a, b) => a.name < b.name ? -1 : 1);
          this.loadDesiredData();
          this.setupTimer();
        }
      }
    });

  }

  setupTimer() {
    this.sensorInterval = setInterval(this.loadDesiredData.bind(this), environment.sensorDataTimer);
  }

  loadDesiredData() {
    if (this.isUpdatingSliders) {
      return;
    }

    if (this.floor != null && this.floor.rooms != null) {

      this.facilityService.getDesiredData(this.floor).then((desired: IDesired[]) => {
        if (desired != null && desired.length > 0) {
          this.desiredData = desired;
        }
        this.loadSensorData();
      });
    }
  }

  loadSensorData() {
    if (this.floor != null && this.floor.rooms != null) {

      this.facilityService.getSensorData(this.floor).then((sensors: ISensor[]) => {
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
        this.sensorData = sensors;
        this.spinnerService.hide();
      });
    }
  }

  setLightReading(sensor: ISensor) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const light: ILight = actual == null ? null :
      { desired: desired * 100.0, actual: actual * 100.0 };

    const room = this.floor.rooms.find(r => r.id === sensor.roomId);

    if (room != null) {
      room.light = light;
    }
  }

  setTemperatureReading(sensor: ISensor) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const temp: IThermostat = actual == null ? null :
      { desired: desired, actual: actual };

    const room = this.floor.rooms.find(r => r.id === sensor.roomId);

    if (room != null) {
      room.thermostat = temp;
    }

  }

  setMotionReading(sensor: ISensor) {

    const motion: IMotion = { isMotion: sensor.sensorReading.toLowerCase() === 'true' };
    const room = this.floor.rooms.find(r => r.id === sensor.roomId);

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

      if (this.desiredData !== null) {
        desired = this.desiredData.find((d) => d.roomId === sensor.roomId && d.sensorId === sensor.sensorId);
      }

      return JSON.parse(desired ? desired.desiredValue : sensor.sensorReading);
    } catch (ex) { }

    return null;
  }

  thermostatSliderValueChange(room: IRoom, changeContext: ChangeContext) {
    console.log(`${room.name} thermostat desired changed: ${changeContext.value}`);

    if (this.theromstatSliderTimeout) {
      clearTimeout(this.theromstatSliderTimeout);
    }

    const sensor = this.sensorData.find(s => s.roomId === room.id && s.sensorDataType === 'Temperature');

    if (!sensor) {
      return;
    }

    let desired: IDesired = this.desiredData.find((d) => d.roomId === room.id && d.sensorId === sensor.sensorId);

    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: room.thermostat.desired.toString()
      };

      this.desiredData.push(desired);
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

  lightSliderValueChange(room: IRoom, changeContext: ChangeContext) {
    console.log(`${room.name} light desired changed: ${changeContext.value}`);

    if (this.lightSliderTimeout) {
      clearTimeout(this.lightSliderTimeout);
    }

    const sensor = this.sensorData.find(s => s.roomId === room.id && s.sensorDataType === 'Light');

    if (!sensor) {
      return;
    }

    let desired: IDesired = this.desiredData.find((d) => d.roomId === room.id && d.sensorId === sensor.sensorId);

    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: (room.light.desired / 100.0).toString()
      };

      this.desiredData.push(desired);
    } else {
      desired.desiredValue = (room.light.desired / 100.0).toString();
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
    this.router.navigate(['/']);
  }

  returnToHotel() {
    this.router.navigate(['/hotel', { id: this.hotelId, index: this.hotelIndex }]);
  }
}
