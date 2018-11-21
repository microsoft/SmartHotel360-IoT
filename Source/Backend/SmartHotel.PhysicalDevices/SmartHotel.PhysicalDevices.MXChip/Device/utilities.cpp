#include "AZ3166WiFi.h"
#include "HTS221Sensor.h"
#include "AzureIotHub.h"
#include "Arduino.h"
#include "parson.h"
#include "config.h"
#include "RGB_LED.h"
#include "Sensor.h"
#include "http_client.h"
#include "config.h"
#include "utilities.h"

#define RGB_LED_BRIGHTNESS 32
#define MAGNET_PRESENT_DELTA 250
#define MAX_UPLOAD_SIZE 1024

#define DEVICES_INCLUDE_ARGUMENT "includes=Sensors,ConnectionString,Types,SensorsTypes"

DevI2C *i2c;
HTS221Sensor *tempSensor;
LIS2MDLSensor *magnetSensor;
int magnetAxes[3];
int magnetBaseX;
int magnetBaseY;
int magnetBaseZ;
bool lastMagnetStatus;
RGB_LED rgbLed;
int sendInterval = SENSOR_DATA_SEND_INTERVAL;
float lastTemperatureSent;
int lastLightValueSent;
bool lastRoomOccupied;
bool lastRoomOccupiedSent;

char outputString[20];

char* initializeWiFi()
{
  if (WiFi.begin() == WL_CONNECTED)
  {
    IPAddress ip = WiFi.localIP();
    return ip.get_address();
  }
  return nullptr;
}

int sensorDataSendInterval()
{
    return sendInterval;
}

void showSendConfirmation()
{
    pinMode(LED_USER, OUTPUT);
    DigitalOut LedUser(LED_BUILTIN);
    digitalWrite(LED_USER, 1);
    delay(500);
    digitalWrite(LED_USER, 0);
}

void updateOccupancyMessage()
{
    sprintf(outputString, "%s", lastRoomOccupied ? "Occupied" : "Vacant");
    Screen.print(3, outputString);
}

void userASwitch()
{
    lastRoomOccupied = !lastRoomOccupied;

    updateOccupancyMessage();
}

void initSensors()
{
    i2c = new DevI2C(D14, D15);

    // Initialize temperature sensor
    tempSensor = new HTS221Sensor(*i2c);
    tempSensor->init(NULL);

    lastTemperatureSent = -1000;
    lastLightValueSent = -1;
    lastRoomOccupied = false;
    lastRoomOccupiedSent = false;

    // Initializing Button A
    attachInterrupt(USER_BUTTON_A, userASwitch, RISING);

    // Initialize magnetometer
    magnetSensor = new LIS2MDLSensor(*i2c);
    magnetSensor->init(NULL);
  
    magnetSensor->getMAxes(magnetAxes);
    magnetBaseX = magnetAxes[0];
    magnetBaseY = magnetAxes[1];
    magnetBaseZ = magnetAxes[2];

    int count = 0;
    int delta = 10;
    char buffer[20];
    while (true)
    {
        delay(1000);
        magnetSensor->getMAxes(magnetAxes);
        
        // Waiting for the data from magnetSensor to become stable
        if (abs(magnetBaseX - magnetAxes[0]) < delta && abs(magnetBaseY - magnetAxes[1]) < delta && abs(magnetBaseZ - magnetAxes[2]) < delta)
        {
            count++;
            if (count >= 5)
            {
                break;
            }
        }
        else
        {
            count = 0;
            magnetBaseX = magnetAxes[0];
            magnetBaseY = magnetAxes[1];
            magnetBaseZ = magnetAxes[2];
        }
    }
}

float readTemperature()
{
    tempSensor->reset();

    float temperature = 0;
    tempSensor->getTemperature(&temperature);

    return temperature;
}

bool readMagnetometerStatus()
{
    magnetSensor->getMAxes(magnetAxes);
    return (abs(magnetBaseX - magnetAxes[0]) > MAGNET_PRESENT_DELTA || abs(magnetBaseY - magnetAxes[1]) > MAGNET_PRESENT_DELTA || abs(magnetBaseZ - magnetAxes[2]) > MAGNET_PRESENT_DELTA);
}

bool readRoomOccupied()
{
    bool magnetStatus = readMagnetometerStatus();

    if (magnetStatus && magnetStatus != lastMagnetStatus)
    {
        lastRoomOccupied = !lastRoomOccupied;
    }

    lastMagnetStatus = magnetStatus;

    updateOccupancyMessage();

    return lastRoomOccupied;
}

void setDeviceLightLevel(int lightLevel)
{
    if (lightLevel <=30)
    {
        rgbLed.turnOff();
        rgbLed.setColor(0, 0, RGB_LED_BRIGHTNESS);
    }
    else if (lightLevel <= 65)
    {
        rgbLed.turnOff();
        rgbLed.setColor(0, RGB_LED_BRIGHTNESS, 0);
    }
    else
    {
        rgbLed.turnOff();
        rgbLed.setColor(RGB_LED_BRIGHTNESS, 0, 0);
    }
}

char* getUtcDateTimeNow()
{
    time_t now;
    time(&now);
    int stringSize = sizeof "2011-10-08T07:07:09Z";
    char* timeString = (char*)malloc(stringSize);
    strftime(timeString, stringSize, "%FT%TZ", gmtime(&now));

    return timeString;
}

char* getDeviceNameFromConnectionString(char *connectionString)
{
    if (connectionString == NULL)
    {
        return NULL;
    }
    int start = 0;
    int cur = 0;
    bool find = false;
    while (connectionString[cur] > 0)
    {
        if (connectionString[cur] == '=')
        {
            // Check the key
            if (memcmp(&connectionString[start], "DeviceId", 8) == 0)
            {
                // This is the host name
                find = true;
            }
            start = ++cur;
            // Value
            while (connectionString[cur] > 0)
            {
                if (connectionString[cur] == ';')
                {
                    break;
                }
                cur++;
            }
            if (find && cur - start > 0)
            {
                char *devicename = (char *)malloc(cur - start + 1);
                memcpy(devicename, &connectionString[start], cur - start);
                devicename[cur - start] = 0;
                return devicename;
            }
            start = cur + 1;
        }
        cur++;
    }
    return NULL;
}

char* getIotDeviceName()
{
    // Load connection from EEPROM
    EEPROMInterface eeprom;
    uint8_t connString[AZ_IOT_HUB_MAX_LEN + 1] = {'\0'};
    int ret = eeprom.read(connString, AZ_IOT_HUB_MAX_LEN, 0x00, AZ_IOT_HUB_ZONE_IDX);
    if (ret < 0)
    {
        LogError("Unable to get the azure iot connection string from EEPROM. Please set the value in configuration mode.");
        return nullptr;
    }
    else if (ret == 0)
    {
        LogError("The connection string is empty.\r\nPlease set the value in configuration mode.");
        return nullptr;
    }
    char* iotHubConnectionString = (char *)malloc(AZ_IOT_HUB_MAX_LEN + 1);
    sprintf(iotHubConnectionString, "%s", connString);

    return getDeviceNameFromConnectionString(iotHubConnectionString);
}

void createSensorMessagePayload(const char* connectionString, SensorInfo* sensor, char* ioTHubDeviceId, char* value, char *payload)
{
    JSON_Value *root_value = json_value_init_object();
    JSON_Object *root_object = json_value_get_object(root_value);
    char* serialized_string = NULL;

    json_object_set_string(root_object, "SensorReading", value);
    json_object_set_string(root_object, "SensorId", sensor->id);
    json_object_set_string(root_object, "SensorType", sensor->type);
    json_object_set_string(root_object, "SensorDataType", sensor->dataType);
    json_object_set_string(root_object, "SpaceId", sensor->spaceId);
    json_object_set_string(root_object, "IoTHubDeviceId", ioTHubDeviceId);
    json_object_set_string(root_object, "EventTimestamp", getUtcDateTimeNow());
     json_object_set_string(root_object, "ConnectionString", connectionString);
   
    serialized_string = json_serialize_to_string_pretty(root_value);

    snprintf(payload, MAX_UPLOAD_SIZE, "%s", serialized_string);
    json_free_serialized_string(serialized_string);
    json_value_free(root_value);
}

bool createTemperatureSensorMessagePayload(const char* connectionString, SensorInfo* temperatureSensor, char* ioTHubDeviceId, float temperature, char *payload, bool forceSend)
{
    if (temperature == lastTemperatureSent && !forceSend)
    {
        return false;
    }

    lastTemperatureSent = temperature;

    sprintf(outputString, "%.1f", lastTemperatureSent);

    createSensorMessagePayload(connectionString, temperatureSensor, ioTHubDeviceId, outputString, payload);

    return true;
}

bool createLightSensorMessagePayload(const char* connectionString, SensorInfo* lightSensor, char* ioTHubDeviceId, int lightValue, char *payload, bool forceSend)
{
    if (lightValue == lastLightValueSent && !forceSend)
    {
        return false;
    }

    lastLightValueSent = lightValue;

    sprintf(outputString, "%.2f", (float)lastLightValueSent / 100.0f);

    createSensorMessagePayload(connectionString, lightSensor, ioTHubDeviceId, outputString, payload);

    return true;
}

bool createMotionSensorMessagePayload(const char* connectionString, SensorInfo* motionSensor, char* ioTHubDeviceId, bool roomOccupied, char *payload, bool forceSend)
{
    if (roomOccupied == lastRoomOccupiedSent && !forceSend)
    {
        return false;
    }

    lastRoomOccupiedSent = roomOccupied;

    sprintf(outputString, "%s", lastRoomOccupiedSent ? "True" : "False");

    createSensorMessagePayload(connectionString, motionSensor, ioTHubDeviceId, outputString, payload);

    return true;
}

DeviceInfo* getDTIoTHubDeviceInfo(char* hardwareId, char* sasToken)
{
    char azureFunctionUri[256];
    sprintf(azureFunctionUri, "%sDevices?hardwareIds=%s&%s", ensureStringEndsWithSlash(DIGITAL_TWINS_MANAGEMENT_API_ENDPOINT), hardwareId, DEVICES_INCLUDE_ARGUMENT);

    HTTPClient *httpClient = new HTTPClient(HTTP_GET, azureFunctionUri);
    httpClient->set_header("Authorization", sasToken);
    const Http_Response* result = httpClient->send();

    char* status = (char *)malloc(20);

    JSON_Value* root_value = json_parse_string(result->body);
    JSON_Array* devices = json_value_get_array(root_value);
    if (json_array_get_count(devices) != 1)
    {
        return nullptr;
    }

    DeviceInfo* deviceInfo = new DeviceInfo;

    JSON_Object* device = json_array_get_object(devices, 0);
    deviceInfo->id = json_object_get_string(device, "id");
    deviceInfo->connectionString = json_object_get_string(device, "connectionString");
    deviceInfo->friendlyName = json_object_get_string(device, "friendlyName");
    deviceInfo->deviceType = json_object_get_string(device, "deviceType");
    deviceInfo->deviceSubtype = json_object_get_string(device, "deviceSubtype");
    deviceInfo->hardwareId = json_object_get_string(device, "hardwareId");
    deviceInfo->spaceId = json_object_get_string(device, "spaceId");
    deviceInfo->status = json_object_get_string(device, "status");

    JSON_Array* sensors = json_object_get_array(device, "sensors");
    int numberSensors = json_array_get_count(sensors);
    deviceInfo->sensors = (SensorInfo**)malloc(numberSensors);

    for (int sensorIndex=0; sensorIndex<numberSensors; sensorIndex++)
    {
        SensorInfo* sensorInfo = new SensorInfo;

        JSON_Object* sensor = json_array_get_object(sensors, sensorIndex);
        sensorInfo->id = json_object_get_string(sensor, "id");
        sensorInfo->dataType = json_object_get_string(sensor, "dataType");
        sensorInfo->dataUnitType = json_object_get_string(sensor, "dataUnitType");
        sensorInfo->deviceId = json_object_get_string(sensor, "deviceId");
        sensorInfo->pollRate = (int)(json_object_get_number(sensor, "pollRate"));
        sensorInfo->portType = json_object_get_string(sensor, "portType");
        sensorInfo->spaceId = json_object_get_string(sensor, "spaceId");
        sensorInfo->type = json_object_get_string(sensor, "type");

        deviceInfo->sensors[sensorIndex] = sensorInfo;
    }

    delete httpClient;

    return deviceInfo;
}

SensorInfo* getTemperatureSensorFromDevice(DeviceInfo* deviceInfo)
{
    for (int sensorIndex=0; sensorIndex<3; sensorIndex++)
    {
        SensorInfo* sensorInfo = deviceInfo->sensors[sensorIndex];

        if (strcmp(sensorInfo->dataType, "Temperature") == 0)
        {
            return sensorInfo;
        }
    }

    return nullptr;
}

SensorInfo* getLightSensorFromDevice(DeviceInfo* deviceInfo)
{
    for (int sensorIndex=0; sensorIndex<3; sensorIndex++)
    {
        SensorInfo* sensorInfo = deviceInfo->sensors[sensorIndex];

        if (strcmp(sensorInfo->dataType, "Light") == 0)
        {
            return sensorInfo;
        }
    }

    return nullptr;
}

SensorInfo* getMotionSensorFromDevice(DeviceInfo* deviceInfo)
{
    for (int sensorIndex=0; sensorIndex<3; sensorIndex++)
    {
        SensorInfo* sensorInfo = deviceInfo->sensors[sensorIndex];

        if (strcmp(sensorInfo->dataType, "Motion") == 0)
        {
            return sensorInfo;
        }
    }

    return nullptr;
}

bool sendPayloadToFunction(char *payload, char* functionUri)
{
    HTTPClient *httpClient = new HTTPClient(HTTP_POST, functionUri);
    const Http_Response* result = httpClient->send(payload, strlen(payload));

    delete httpClient;
}

char* ensureStringEndsWithSlash(char* originalString)
{
    int length = strlen(originalString);
    if(originalString[length - 1] == '/')
    {
        return originalString;
    }
    
    char* correctedString = (char*)malloc(length + 1);
    sprintf(correctedString, "%s/", originalString);

    return correctedString;
}