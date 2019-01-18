import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FacilityService } from '../services/facility.service';
import { ILight, IThermostat, IMotion } from '../services/models/IDeviceValues';
import { ISensorReading } from '../services/models/ISensorReading';
import { environment } from '../../environments/environment';
import { IDesired } from '../services/models/IDesired';
import { ChangeContext, Options } from 'ng5-slider';
import { ISpace } from '../services/models/ISpace';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { BusyService } from '../services/busy.service';
import { ISpaceAlert } from '../services/models/ISpaceAlert';
import { Subscription } from 'rxjs';
import { SubscriptionUtilities } from '../helpers/subscription-utilities';

@Component({
  selector: 'app-floor',
  templateUrl: './floor.component.html',
  styleUrls: ['./floor.component.css']
})
export class FloorComponent implements OnInit, OnDestroy {

  private subscriptions: Subscription[] = [];

  constructor(private route: ActivatedRoute,
    private facilityService: FacilityService,
    private busyService: BusyService) {
    this.roomsById = new Map<string, ISpace>();
    this.desiredDataByRoomIdThenSensorId = new Map<string, Map<string, IDesired>>();
    this.sensorDataByRoomIdThenSensorId = new Map<string, Map<string, ISensorReading>>();
  }

  @ViewChild('breadcumbs') private breadcrumbs: BreadcrumbComponent;
  public tenantId: string;
  public hotelBrandId: string;
  public hotelBrandName: string;
  public hotelName: string;
  public hotelId: string;
  public hotelIndex: number;
  private floorId: string;
  public floorName: string;

  public rooms: ISpace[] = null;
  private roomsById: Map<string, ISpace>;
  private desiredDataByRoomIdThenSensorId: Map<string, Map<string, IDesired>>;
  private sensorDataByRoomIdThenSensorId: Map<string, Map<string, ISensorReading>>;
  private sensorInterval;
  private theromstatSliderTimeout;
  private lightSliderTimeout;
  private isUpdatingSliders = false;

  get useSimpleAuth() { return environment.useSimpleAuth; }

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
      this.hotelName = params['hName'];
      this.floorId = params['fId'];
      this.facilityService.executeWhenInitialized(this, this.loadRooms);
    });
  }

  ngOnDestroy() {
    if (this.sensorInterval != null) {
      clearInterval(this.sensorInterval);
    }

    this.subscriptions.forEach(s => SubscriptionUtilities.tryUnsubscribe(s));
  }

  loadRooms(self: FloorComponent) {

    self.busyService.busy();
    const floor = self.facilityService.getSpace(self.hotelId, self.floorId);
    if (!floor) {
      self.breadcrumbs.returnToHotel();
      return;
    }
    self.floorName = floor.friendlyName;
    self.rooms = self.facilityService.getChildSpaces(self.floorId);
    self.rooms.forEach(room => self.roomsById.set(room.id, room));
    self.loadDesiredData();
    self.setupTimer();

    self.subscriptions.push(self.facilityService.getTemperatureAlerts()
      .subscribe(tempAlerts => self.temperatureAlertsUpdated(self.rooms, tempAlerts)));
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
            let desiredDataForRoom = this.desiredDataByRoomIdThenSensorId.get(d.roomId);
            if (!desiredDataForRoom) {
              desiredDataForRoom = new Map<string, IDesired>();
              this.desiredDataByRoomIdThenSensorId.set(d.roomId, desiredDataForRoom);
            }

            desiredDataForRoom.set(d.sensorId, d);
          });
        }
        this.loadSensorData();
      });
      this.busyService.idle();
    }
  }

  loadSensorData() {
    if (this.rooms != null) {
      this.facilityService.getSensorData(this.rooms).then((sensors: ISensorReading[]) => {
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
          let sensorDataForRoom = this.sensorDataByRoomIdThenSensorId.get(s.roomId);
          if (!sensorDataForRoom) {
            sensorDataForRoom = new Map<string, ISensorReading>();
            this.sensorDataByRoomIdThenSensorId.set(s.roomId, sensorDataForRoom);
          }

          sensorDataForRoom.set(s.sensorId, s);
        });
      });
    }
  }

  setLightReading(sensor: ISensorReading) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const light: ILight = actual == null ? null :
      { desired: desired * 100.0, actual: actual * 100.0 };

    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.light = light;
    }
  }

  setTemperatureReading(sensor: ISensorReading) {
    const actual = this.getSensorReading(sensor);
    const desired = this.getDesiredValue(sensor);

    const temp: IThermostat = actual == null ? null :
      { desired: desired, actual: actual };

    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.thermostat = temp;
    }

  }

  setMotionReading(sensor: ISensorReading) {

    const motion: IMotion = { isMotion: sensor.sensorReading.toLowerCase() === 'true' };
    const room = this.roomsById.get(sensor.roomId);

    if (room != null) {
      room.motion = motion;
    }

  }

  getSensorReading(sensor: ISensorReading) {

    try {
      return JSON.parse(sensor.sensorReading);
    } catch (ex) { }

    return null;
  }

  getDesiredValue(sensor: ISensorReading) {

    try {
      let desired: IDesired = null;

      if (this.desiredDataByRoomIdThenSensorId !== null) {
        const desiredDatas = this.desiredDataByRoomIdThenSensorId.get(sensor.roomId);
        if (desiredDatas) {
          desired = desiredDatas.get(sensor.sensorId);
        }
      }

      return JSON.parse(desired ? desired.desiredValue : sensor.sensorReading);
    } catch (ex) { }

    return null;
  }

  thermostatSliderValueChange(room: ISpace, changeContext: ChangeContext) {
    const self = this;
    console.log(`${room.friendlyName} thermostat desired changed: ${changeContext.value}`);

    if (this.theromstatSliderTimeout) {
      clearTimeout(this.theromstatSliderTimeout);
    }

    const sensors = this.sensorDataByRoomIdThenSensorId.get(room.id);
    if (!sensors) {
      return;
    }

    const sensor = Array.from(sensors.values()).find(s => s.sensorDataType === 'Temperature');

    if (!sensor) {
      return;
    }

    const desiredDatas = this.desiredDataByRoomIdThenSensorId.get(room.id);
    let desired: IDesired;
    if (desiredDatas) {
      desired = desiredDatas.get(sensor.sensorId);
    }

    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: room.thermostat.desired.toString()
      };

      if (desiredDatas) {
        desiredDatas.set(desired.sensorId, desired);
      } else {
        const desiredDataForRoom = new Map<string, IDesired>();
        desiredDataForRoom.set(desired.sensorId, desired);
        this.desiredDataByRoomIdThenSensorId.set(room.id, desiredDataForRoom);
      }
    } else {
      desired.desiredValue = room.thermostat.desired.toString();
    }

    this.theromstatSliderTimeout = setTimeout((d: IDesired) => {
      const request = {
        roomId: d.roomId,
        sensorId: d.sensorId,
        desiredValue: d.desiredValue,
        methodName: 'SetDesiredTemperature',
        deviceId: sensor.iotHubDeviceId
      };
      this.facilityService.setDesiredData(request);
    }, 250, desired);
  }

  lightSliderValueChange(room: ISpace, changeContext: ChangeContext) {
    const self = this;
    console.log(`${room.friendlyName} light desired changed: ${changeContext.value}`);

    if (this.lightSliderTimeout) {
      clearTimeout(this.lightSliderTimeout);
    }

    const sensors = this.sensorDataByRoomIdThenSensorId.get(room.id);
    if (!sensors) {
      return;
    }

    const sensor = Array.from(sensors.values()).find(s => s.sensorDataType === 'Light');

    if (!sensor) {
      return;
    }

    const desiredDatas = this.desiredDataByRoomIdThenSensorId.get(room.id);
    let desired: IDesired;
    if (desiredDatas) {
      desired = desiredDatas.get(sensor.sensorId);
    }

    const desiredValue = (room.light.desired / 100.0).toString();
    if (!desired) {
      desired = {
        roomId: room.id,
        sensorId: sensor.sensorId,
        desiredValue: desiredValue
      };

      if (desiredDatas) {
        desiredDatas.set(desired.sensorId, desired);
      } else {
        const desiredDataForRoom = new Map<string, IDesired>();
        desiredDataForRoom.set(desired.sensorId, desired);
        this.desiredDataByRoomIdThenSensorId.set(room.id, desiredDataForRoom);
      }
    } else {
      desired.desiredValue = desiredValue;
    }

    this.lightSliderTimeout = setTimeout((d: IDesired) => {
      const request = {
        roomId: d.roomId,
        sensorId: d.sensorId,
        desiredValue: d.desiredValue,
        methodName: 'SetDesiredAmbientLight',
        deviceId: sensor.iotHubDeviceId
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

  getFriendlyRoomType(room: ISpace) {
    return room.subtype.replace('Room', '').replace('VIP', 'VIP ').replace('Conference', 'Conference Room');
  }

  temperatureAlertsUpdated(spaces: ISpace[], spaceAlerts: ISpaceAlert[]) {
    if (!spaces) {
      return;
    }
    if (!spaceAlerts) {
      spaces.forEach(space => {
        space.hasAlert = false;
        space.alertMessage = null;
      });
    } else {
      spaces.forEach(space => {
        const alertForSpace = spaceAlerts.find(alert => alert.spaceId === space.id);
        if (alertForSpace) {
          space.hasAlert = true;
          space.alertMessage = alertForSpace.message;
        } else {
          space.hasAlert = false;
          space.alertMessage = null;
        }
      });
    }
  }
}
