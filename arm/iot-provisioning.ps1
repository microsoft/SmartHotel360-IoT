param(
    [Parameter(Mandatory = $True)]
    [string]
    $iothub
)

$deviceHashTable = @{}

echo 'Create devices'
$ids = ( 'Room11Light' , 'Room11Thermostat', 'Room12Light' , 'Room12Thermostat', 'Room13Light' , 'Room13Thermostat', 
    'Room21Light' , 'Room21Thermostat', 'Room22Light' , 'Room22Thermostat', 'Room23Light' , 'Room23Thermostat',
    'Room31Light' , 'Room31Thermostat', 'Room32Light' , 'Room32Thermostat', 'Room33Light' , 'Room33Thermostat',
    'Room41Light' , 'Room41Thermostat', 'Room42Light' , 'Room42Thermostat', 'Room43Light' , 'Room43Thermostat')

foreach ($id in $ids) {
    echo 'Creating device: ' $id
    $creationResult = az iot hub device-identity create `
                    --hub-name $iothub `
                    --device-id $id

    $connectionString = az iot hub device-identity show-connection-string `
    --hub-name $iothub `
    --device-id $id `
    | ConvertFrom-Json `

    $deviceHashTable[$id] = $connectionString.cs
}

$output = @{
    room11 = @{
        light       = $deviceHashTable.Room11Light
        temperature = $deviceHashTable.Room11Thermostat 
    };
    room12 = @{
        light       = $deviceHashTable.Room12Light
        temperature = $deviceHashTable.Room12Thermostat 
    };
    room13 = @{
        light       = $deviceHashTable.Room13Light
        temperature = $deviceHashTable.Room13Thermostat 
    };
    room21 = @{
        light       = $deviceHashTable.Room21Light
        temperature = $deviceHashTable.Room21Thermostat 
    };
    room22 = @{
        light       = $deviceHashTable.Room22Light
        temperature = $deviceHashTable.Room22Thermostat 
    };
    room23 = @{
        light       = $deviceHashTable.Room23Light
        temperature = $deviceHashTable.Room23Thermostat 
    };
    room31 = @{
        light       = $deviceHashTable.Room31Light
        temperature = $deviceHashTable.Room31Thermostat 
    };
    room32 = @{
        light       = $deviceHashTable.Room32Light
        temperature = $deviceHashTable.Room32Thermostat 
    };
    room33 = @{
        light       = $deviceHashTable.Room33Light
        temperature = $deviceHashTable.Room33Thermostat 
    };
    room41 = @{
        light       = $deviceHashTable.Room41Light
        temperature = $deviceHashTable.Room41Thermostat 
    };
    room42 = @{
        light       = $deviceHashTable.Room42Light
        temperature = $deviceHashTable.Room42Thermostat 
    };
    room43 = @{
        light       = $deviceHashTable.Room43Light
        temperature = $deviceHashTable.Room43Thermostat 
    };
};

$path = Get-Location
$outfile = $path.ToString() + '\iot-device-connectionstring.json'

$output | ConvertTo-Json | Out-File $outfile

echo 'created file : ' $outfile
