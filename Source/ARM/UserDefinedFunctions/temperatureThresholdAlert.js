const temperatureAlertValueDataType = 'TemperatureAlert'
const noTempAlertMessage = 'Temperature is within desired norms.';
const minTemperatureAlertThresholdPropertyName = 'MinTemperatureAlertThreshold';
const maxTemperatureAlertThresholdPropertyName = 'MaxTemperatureAlertThreshold';

const maxTemperatureThreshold = 85;
const minTemperatureThreshold = 65;

function process(telemetry, executionContext) {
    try {
        // Log SensorId and Message
        log(`Sensor ID: ${telemetry.SensorId}.`);
        log(`Sensor value: ${JSON.stringify(telemetry.Message)}.`);

        // Get sensor metadata
        var sensor = getSensorMetadata(telemetry.SensorId);

        // Retrieve the sensor reading
        var temperatureValue = JSON.parse(telemetry.Message);
        log(`TemperatureValue: ${temperatureValue}`);

        // Get parent space
        var parentSpace = sensor.Space();
        // setSpaceValue(parentSpace.Id, 'Test', null)
        // getMinTempThreshold(parentSpace);
        // getMaxTempThreshold(parentSpace);

        if (temperatureValue > maxTemperatureThreshold) {
            const highTempAlert = `${temperatureAlertValueDataType}: ${temperatureValue}° is above maximum threshold of ${maxTemperatureThreshold}°.`;
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, highTempAlert);

        } else if (temperatureValue < minTemperatureThreshold) {
            const lowTempAlert = `${temperatureAlertValueDataType}: ${temperatureValue}° is below minimum threshold of ${minTemperatureThreshold}°.`;
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, lowTempAlert);

        } else {
            log(`${parentSpace.Id}(${parentSpace.Name}): ${noTempAlertMessage}.`);
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, null);
        }
    } catch (error) {
        log(`An error has occurred processing the UDF Error: ${error.name} Message ${error.message}.`);
    }
}

// function getMinTempThreshold(space) {
//     if (!space) {
//         return;
//     }
//     setSpaceValue(space.Id, 'Test', 'Hello')

//     // let minTempAlertThresholdProperty;
//     try {
//         setSpaceValue(space.Id, 'Test', minTemperatureAlertThresholdPropertyName)
//         minTempAlertThresholdProperty = getSpaceExtendedProperty(space.Id, minTemperatureAlertThresholdPropertyName);
//         setSpaceValue(space.Id, 'Test', 'Hello-There')
//     } catch (error) {
//         setSpaceValue(space.Id, 'ErrorMinThresholdProperty', error);
//     }
//     // if (minTempAlertThresholdProperty) {
//     //     const message = `${minTemperatureAlertThresholdPropertyName}: ${JSON.stringify(minTempAlertThresholdProperty)}`;
//     //     log(message)
//     //     setSpaceValue(space.Id, 'MinThresholdPropery', message);
//     // }
//     // else {
//     //     return getMinTempThreshold(space.Parent())
//     // }
// }

// function getMaxTempThreshold(space) {
//     if (!space) {
//         return;
//     }

//     const maxTempAlertThresholdProperty = space.ExtendedProperty(maxTemperatureAlertThresholdPropertyName);
//     if (maxTempAlertThresholdProperty) {
//         const message = `${maxTemperatureAlertThresholdPropertyName}: ${JSON.stringify(maxTempAlertThresholdProperty)}`;
//         log(message)
//         setSpaceValue(space.Id, 'MaxThresholdPropery', message);
//     }
//     else {
//         return getMaxTempThreshold(space.Parent())
//     }
// }