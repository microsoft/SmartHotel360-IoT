#ifndef UTILITY_H
#define UTILITY_H

typedef struct
{
    const char* id;
    const char* dataType;
    const char* dataUnitType;
    const char* deviceId;
    int pollRate;
    const char* portType;
    const char* spaceId;
    const char* type;
} SensorInfo;

typedef struct
{
    const char* id;
    const char* connectionString;
    SensorInfo** sensors;
    const char* friendlyName;
    const char* deviceType;
    const char* deviceSubtype;
    const char* hardwareId;
    const char* spaceId;
    const char* status;
} DeviceInfo;

char* initializeWiFi(void);

void initSensors(void);

float readTemperature();
bool readRoomOccupied();

void setDeviceLightLevel(int desiredLightLevel);

int sensorDataSendInterval(void);

void showSendConfirmation(void);

char* getDeviceNameFromConnectionString(char *connectionString);

char* getIotDeviceName();

DeviceInfo* getDTIoTHubDeviceInfo(char* hardwareId, char* sasToken);

SensorInfo* getTemperatureSensorFromDevice(DeviceInfo* deviceInfo);

SensorInfo* getLightSensorFromDevice(DeviceInfo* deviceInfo);

SensorInfo* getMotionSensorFromDevice(DeviceInfo* deviceInfo);

bool createTemperatureSensorMessagePayload(const char* connectionString, SensorInfo* temperatureSensor, char* ioTHubDeviceId, float temperature, char *payload, bool forceSend);

bool createLightSensorMessagePayload(const char* connectionString, SensorInfo* lightSensor, char* ioTHubDeviceId, int lightValue, char *payload, bool forceSend);

bool createMotionSensorMessagePayload(const char* connectionString, SensorInfo* motionSensor, char* ioTHubDeviceId, bool roomOccupied, char *payload, bool forceSend);

bool sendPayloadToFunction(char *payload, char* functionUri);

#endif /* UTILITY_H */