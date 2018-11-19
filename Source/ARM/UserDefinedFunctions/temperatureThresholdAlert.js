const temperatureAlertValueDataType = 'TemperatureAlert'
const noTempAlertMessage = 'Temperature is within desired norms.';
const minTemperatureAlertThresholdPropertyName = 'MinTemperatureAlertThreshold';
const maxTemperatureAlertThresholdPropertyName = 'MaxTemperatureAlertThreshold';

const maxTempThresholdFallback = 85;
const minTempThresholdFallback = 65;

function process(telemetry, executionContext) {
    try {
        // Log SensorId and Message
        log(`Sensor ID: ${telemetry.SensorId}.`);
        log(`Sensor value: ${JSON.stringify(telemetry.Message)}.`);

        // Get sensor metadata
        var sensor = getSensorMetadata(telemetry.SensorId);

        // Retrieve the sensor reading
        var temperatureValue = JSON.parse(telemetry.Message);

        // Get parent space
        var parentSpace = sensor.Space();

        let minTempThreshold = getMinTempThreshold(parentSpace);
        if (minTempThreshold === undefined) {
            minTempThreshold = minTempThresholdFallback;
        }

        let maxTempThreshold = getMaxTempThreshold(parentSpace);
        if (maxTempThreshold === undefined) {
            maxTempThreshold = maxTempThresholdFallback;
        }

        if (temperatureValue > maxTempThreshold) {
            const highTempAlert = `${temperatureAlertValueDataType}: ${temperatureValue}° is above maximum threshold of ${maxTempThreshold}°.`;
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, highTempAlert);

        } else if (temperatureValue < minTempThreshold) {
            const lowTempAlert = `${temperatureAlertValueDataType}: ${temperatureValue}° is below minimum threshold of ${minTempThreshold}°.`;
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, lowTempAlert);

        } else {
            log(`${parentSpace.Id}(${parentSpace.Name}): ${noTempAlertMessage}.`);
            setSpaceValue(parentSpace.Id, temperatureAlertValueDataType, null);
        }
    } catch (error) {
        log(`An error has occurred processing the UDF Error: ${error.name} Message ${error.message}.`);
    }
}

function getMinTempThreshold(space) {
    if (!space) {
        return;
    }

    let minTempAlertThresholdProperty;
    try {
        minTempAlertThresholdProperty = getSpaceExtendedProperty(space.Id, minTemperatureAlertThresholdPropertyName);
    } catch (error) {
        // This will occur if the property does not exist on this space.
    }

    if (minTempAlertThresholdProperty) {
        return JSON.parse(minTempAlertThresholdProperty.Value);
    }
    else {
        try {
            const parentSpace = space.Parent();
            if (parentSpace === undefined || parentSpace === null) {
                return undefined;
            }
            return getMinTempThreshold()
        } catch (error) {
            // protecting against an error occurring when getting the parent space.
            return undefined;
        }
    }
}

function getMaxTempThreshold(space) {
    if (!space) {
        return;
    }

    let maxTempAlertThresholdProperty;
    try {
        maxTempAlertThresholdProperty = getSpaceExtendedProperty(space.Id, maxTemperatureAlertThresholdPropertyName);
    } catch (error) {
        // This will occur if the property does not exist on this space.
    }

    if (maxTempAlertThresholdProperty) {
        return JSON.parse(maxTempAlertThresholdProperty.Value);
    }
    else {
        try {
            const parentSpace = space.Parent();
            if (parentSpace === undefined || parentSpace === null) {
                return undefined;
            }
            return getMaxTempThreshold()
        } catch (error) {
            // protecting against an error occurring when getting the parent space.
            return undefined;
        }
    }
}