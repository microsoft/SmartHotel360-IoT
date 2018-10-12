<#
 .SYNOPSIS
    Deploys a template to Azure

 .DESCRIPTION
    Deploys an Azure Resource Manager template

 .PARAMETER subscriptionId
    The subscription id where the template will be deployed.

 .PARAMETER resourceGroupName
    The resource group where the template will be deployed. Can be the name of an existing or a new resource group.

 .PARAMETER resourceGroupLocation
    Optional, a resource group location. If specified, will try to create a new resource group in this location. If not specified, assumes resource group is existing.

 .PARAMETER managerObjId
    The Azure Object Id for the Manager user.

 .PARAMETER employeeObjId
    The Azure Object Id for the Employee user.

 .PARAMETER clientId
    The Id of the Azure Active directory application.

 .PARAMETER clientSecret
    The key from the Azure Active Directory App.

 .PARAMETER clientServicePrincipalId
    The id of the Service Principal for the Azure Active Directory App.

 .PARAMETER aksServicePrincipalId
    The Id of the Azure Active directory Service Principal created for the Azure Kubernetes Service to use.

 .PARAMETER aksServicePrincipalKey
    The key from the Azure Active directory Service Principal created for the Azure Kubernetes Service to use.

 .PARAMETER templateFilePath
    Optional, path to the template file. Defaults to template.json.

 .PARAMETER parametersFilePath
    Optional, path to the parameters file. Defaults to parameters.json. If file is not found, will prompt for parameter values based on template.
#>

param(
 [Parameter(Mandatory=$True)]
 [string]
 $subscriptionId,

 [Parameter(Mandatory=$True)]
 [string]
 $resourceGroupName,

 [string]
 $resourceGroupLocation,

 [Parameter(Mandatory=$True)]
 [string]
 $managerObjId,

 [Parameter(Mandatory=$True)]
 [string]
 $employeeObjId,

 [Parameter(Mandatory=$True)]
 [string]
 $clientId,

 [Parameter(Mandatory=$True)]
 [string]
 $clientSecret,

 [Parameter(Mandatory=$True)]
 [string]
 $clientServicePrincipalId,

 [Parameter(Mandatory=$True)]
 [string]
 $aksServicePrincipalId,

 [Parameter(Mandatory=$True)]
 [string]
 $aksServicePrincipalKey,

 [string]
 $templateFilePath = "template.json",

 [string]
 $parametersFilePath = "parameters.json"
)

function Reset-Console-Coloring {
    $Host.UI.RawUI.BackgroundColor = ($bckgrnd = 'DarkBlue')
    $Host.UI.RawUI.ForegroundColor = 'White'
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************
Reset-Console-Coloring
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ErrorActionPreference = "Stop"
$powershellEscape = '--%'

# sign in
Write-Host "Logging in...";
az login 

# select subscription
Write-Host "Selecting subscription '$subscriptionId'";

az account set -s $subscriptionId
$azureCliIotExtensionName = "azure-cli-iot-ext"
try
{
    $extensionInfo = az extension show -n $azureCliIotExtensionName
}
catch
{
    az extension add -n $azureCliIotExtensionName
}

$tenantId = (az account show | ConvertFrom-Json).tenantId

#Create or check for existing resource group
$resourceGroup = az group exists -n $resourceGroupName
if($resourceGroup -eq $false)
{
    Write-Host "Resource group '$resourceGroupName' does not exist. To create a new resource group, please enter a location.";
    if(!$resourceGroupLocation) {
        $resourceGroupLocation = Read-Host "resourceGroupLocation";
    }
    Write-Host "Creating resource group '$resourceGroupName' in location '$resourceGroupLocation'";
    az group create -l $resourceGroupLocation -n $resourceGroupName
}
else{
    Write-Host "Using existing resource group '$resourceGroupName'";
}

# https://blog.tyang.org/2018/01/09/generating-unique-guids-in-azure-resource-manager-templates/
$UTCNow = (Get-Date).ToUniversalTime()
 
$UTCTimeTick = $UTCNow.Ticks.tostring()

$TemplateParameters = "_currentDateTimeInTicks=$UTCTimeTick"

# Start the Azure deployment
$StartTimeLocal = Get-Date
Write-Host "Starting deployment at $StartTimeLocal (local time)...";
$deploymentName = "SmartHotel360-IoT-Demo"
if($parametersFilePath -and (Test-Path $parametersFilePath)) {
    $deploymentResultString = az group deployment create -n $deploymentName -g $resourceGroupName --template-file $templateFilePath --parameters $parametersFilePath --parameters $TemplateParameters
} else {
    $deploymentResultString = az group deployment create -n $deploymentName -g $resourceGroupName --template-file $templateFilePath --parameters $TemplateParameters
}

$deploymentResult = $deploymentResultString | ConvertFrom-Json
$outputs = $deploymentResult.properties.outputs

$acrName = ($outputs.acrName.value).ToLower()
$aksClusterName = $outputs.aksClusterName.value
$aksClusterLocation = $outputs.aksClusterLocation.value

$error.Clear()
Write-Host "Attempting to give the Kubernetes Cluster's service principal permission to read from the ACR."

$readRoleAssignmentResult = az role assignment create --assignee `"$aksServicePrincipalId`" --scope `"/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.ContainerRegistry/registries/$acrName`" --role Reader
if(!$?)
{
    Reset-Console-Coloring
    Write-Host
    Write-Host
    Write-Host "Have a user with Azure AD permissions in your subscription run the following command to giving the AKS Service Principal read access to the ACR"
    Write-Host "az role assignment create --assignee `"$aksServicePrincipalId`" --scope `"/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.ContainerRegistry/registries/$acrName`" --role Reader"
    Read-Host -Prompt "Press ENTER to continue once this step has been completed"
}

Write-Host
Write-Host "Creating the AKS Cluster. This can take at least 15 minutes."
$aksClusterCreationResult = az aks create --resource-group "$resourceGroupName" --name "$aksClusterName" --node-count 1 --service-principal "$aksServicePrincipalId" --client-secret "$aksServicePrincipalKey" --location "$aksClusterLocation" --generate-ssh-keys
Write-Host "Finished Creating the AKS Cluster"

$EndTimeLocal = Get-Date
Write-Host "Finished deployment at $EndTimeLocal (local time)...";
Write-Host

$iotHubName = $outputs.iotHubName.value
$iotHubServiceConnectionString = ((az iot hub show-connection-string -n $iotHubName --resource-group $resourceGroupName --policy-name service) | ConvertFrom-Json).cs

$cosmosDbName = $outputs.cosmosDbName.value
$cosmosDbConnectionString = ((az cosmosdb list-connection-strings -n $cosmosDbName -g $resourceGroupName) | ConvertFrom-Json).connectionStrings[0].connectionString

$dtManagementEndpoint = $outputs.digitalTwinsManagementEndpoint.value
$dtApiEndpoint = $outputs.digitalTwinsManagementApiEndpoint.value

$currentUserDigitalTwinsAccessToken = ((az account get-access-token --resource 0b07f429-9f4b-4714-9392-cc5e8e80c8b0) | ConvertFrom-Json).accessToken

$params = @{
    "RoleId"="98e44ad7-28d4-4007-853b-b9968ad132d1";
    "objectId"="$clientServicePrincipalId";
    "objectIdType"="ServicePrincipalId";
    "Path"="/";
    "tenantId"="$tenantId"
}
$bearer = @{ "Authorization" = ("Bearer", $currentUserDigitalTwinsAccessToken -join " ") }
try
{
    Write-Host "Attempting to assign the SpaceAdmin role for the Service Principal."
    Invoke-RestMethod -Uri $dtApiEndpoint/roleassignments -Headers $bearer -Method Post -Body ($params|ConvertTo-Json) -ContentType "application/json"
    Write-Host "Successfully assigned."
}
catch
{
    if($_.ErrorDetails)
    {
        $errorMessage = $_.ErrorDetails.Message
    }
    else
    {
        $errorMessage = $_.Exception.Message
    }

    if($errorMessage -like '*SpaceRoleAssignment already exists*')
    {
        Write-Host "Service Principal already has SpaceAdmin Role Assignment."
    }
    else
    {
        Write-Error "Failed to assign the SpaceAdmin Role Assignment for the Service Principal with error: $errorMessage"
    }
}
Write-Host
Write-Host

#Provision IoT Devices
Write-Host
Write-Host
Write-Host "Provisioning IoT Devices..."

Invoke-Expression "./iot-provisioning.ps1 -iothub $iotHubName"

$eventHubConsumerConnnection = $outputs.eventHubConsumerConnnection.value
$eventHubProducerConnnection = $outputs.eventHubProducerConnnection.value
$eventHubProducerSecondaryConnnection = $outputs.eventHubProducerSecondaryConnnection.value
$eventHubName = $outputs.eventHubName.value

$provisioningOutput = 'ProvisioningOutput.json'
$iotProvisioningOutput = 'iot-device-connectionstring.json'

Copy-Item $iotProvisioningOutput -Destination "../provisioning/ProvisioningDevicesBits/"

#Update Devices and Services Docker/Kubernetes yaml

Write-Host "Provisioning Digital Twins Topology..."

pushd "../provisioning/ProvisioningBits/"
$dtProvisioningArgs = "-t `"$tenantId`" -ci `"$clientId`" -cs `"$clientSecret`" -dt `"$dtApiEndpoint`" -ehcs `"$eventHubProducerConnnection`" -ehscs `"$eventHubProducerSecondaryConnnection`" -ehn `"$eventHubName`" -moid `"$managerObjId`" -eoid `"$employeeObjId`" -o `"$provisioningOutput`""
dotnet SmartHotel.IoT.Provisioning.dll $powershellEscape $dtProvisioningArgs
Copy-Item $provisioningOutput -Destination "../ProvisioningDevicesBits"
$room11SpaceId = (Get-Content "$provisioningOutput" | Out-String | ConvertFrom-Json).room11[0].SpaceId
popd

Write-Host "Provisioning Device sample applications..."

pushd "../provisioning/ProvisioningDevicesBits/"
$deviceProvisioningArgs = "-dt `"$dtManagementEndpoint`" -i $provisioningOutput -d `"../../backend/src/SmartHotel.Devices/`" -cr `"$acrName`" -iot `"$iotProvisioningOutput`""
dotnet SmartHotel.IoT.ProvisioningDevices.dll $powershellEscape $deviceProvisioningArgs
popd

Write-Host "Provisioning APIs..."

pushd "../provisioning/ProvisioningApisBits/"
$apiProvisioningArgs = "-dt `"$dtManagementEndpoint`" -d `"../../backend/src/SmartHotel.Services/`" -cr `"$acrName`" -iot `"$iotHubServiceConnectionString`" -db `"$cosmosDbConnectionString`""
dotnet SmartHotel.IoT.ProvisioningApis.dll $powershellEscape $apiProvisioningArgs
popd

#Build and publish devices and services containers
Write-Host "Building and publishing device images..."
pushd "../backend/src/SmartHotel.Devices"
./build-push.ps1 -subscriptionId $subscriptionId -acrName $acrName
popd

Write-Host "Building and publishing service images..."
pushd "../backend/src/SmartHotel.Services"
./build-push.ps1 -subscriptionId $subscriptionId -acrName $acrName
popd

az aks get-credentials --resource-group "$resourceGroupName" --name "$aksClusterName"

Write-Host
Write-Host
#Deploy service(s) to Kubernetes
Write-Host "Deploying Services to Kubernetes..."
pushd "../backend/src/SmartHotel.Services"
kubectl apply -f deployments.demo.yaml

#Wait for public IPs/Ports and save
Write-Host "Waiting for external ip to be configured..."

$roomDevicesApiUri = $null
while ($roomDevicesApiUri -eq "" -or $roomDevicesApiUri -eq $null)
{
    Start-Sleep -s 1
    $kubeOutput = kubectl get services roomdevices-api-service -o json | ConvertFrom-Json
    Try
    {
        $roomDevicesApiUri = $kubeOutput.status.loadBalancer.ingress.ip
    }
    Catch {}
}
$roomDevicesApiUri = $roomDevicesApiUri + ":" + $kubeOutput.spec.ports.port
popd

$facilityManagementApiName = $outputs.webapiName.value
$facilityManagementApiDefaultHostName = ((az webapp show -g $resourceGroupName -n $facilityManagementApiName) | ConvertFrom-Json).defaultHostName
$facilityManagementApiUri = "https://$facilityManagementApiDefaultHostName"
$functionSiteName = $outputs.functionSiteName.value
$storageAccountName = $outputs.storageAccountName.value

$storageConnectionString = ((az storage account show-connection-string -g $resourceGroupName -n $storageAccountName) | ConvertFrom-Json).connectionString

Write-Host
Write-Host "Setting Facility Manangement Api App Settings"
$facilityManagementApiSettings = "--settings ManagementApiUrl=`"$dtManagementEndpoint`" MongoDBConnectionString=`"$cosmosDbConnectionString`" AzureAd__Audience=`"$clientId`" IoTHubConnectionString=`"$iotHubServiceConnectionString`""
$facilityManagementApiSettingsResults = az webapp config appsettings set -n $facilityManagementApiName -g $resourceGroupName $powershellEscape $facilityManagementApiSettings

Write-Host
Write-Host "Setting Azure Function App Settings"
$functionSettings = "--settings CosmosDBConnectionString=`"$cosmosDbConnectionString`" EventHubConnectionString=`"$eventHubConsumerConnnection`" AzureWebJobsDashboard=`"$storageConnectionString`" AzureWebJobsStorage=`"$storageConnectionString`""
$functionSettingsResults =az webapp config appsettings set -n $functionSiteName -g $resourceGroupName $powershellEscape $functionSettings

$facilityManagementApiEndpoint = "$facilityManagementApiUri/api"

Write-Host
Write-Host "Updating Facility Management Website environment files to point to the deployed azure resources."
Write-Host

$websiteEnvironmentsDirectory = "../FacilityManagementWebsite/SmartHotel.FacilityManagementWeb/SmartHotel.FacilityManagementWeb/ClientApp/src/environments"
$environmentFileNames = @("environment.ts", "environment.prod.ts")

foreach($filename in $environmentFileNames)
{
    $fullFilePath = Resolve-Path "$websiteEnvironmentsDirectory/$filename"
    
    Write-Host "Updating $fullFilePath..."

    $fileContent = Get-Content $fullFilePath -raw
    $fileContent = $fileContent.Replace("{tenantId}","$tenantId")
    $fileContent = $fileContent.Replace("{clientId}","$clientId")
    $fileContent = $fileContent.Replace("{apiUri}","$facilityManagementApiUri")
    $fileContent = $fileContent.Replace("{apiEndpoint}","$facilityManagementApiEndpoint")
    
    $fileContent | Set-Content $fullFilePath -Force

    Write-Host "Update complete."
    Write-Host
}

# Publish the Website
$publishOutputFolder = "./webapp"
$facilityManagementWebsiteName = $outputs.websiteName.value
$deploymentZip = "./SmartHotel.FacilityManagementWeb.Deployment.zip"
Write-Host "Publishing the Facility Management website..."
pushd "../FacilityManagementWebsite/SmartHotel.FacilityManagementWeb/SmartHotel.FacilityManagementWeb"

Write-Host "Running dotnet restore for the Website project"
dotnet restore SmartHotel.FacilityManagementWeb.csproj

Write-Host "Running dotnet build for the Website project"
dotnet build SmartHotel.FacilityManagementWeb.csproj -c Release -o $publishOutputFolder

Write-Host "Running dotnet publish for the Website project"
dotnet publish SmartHotel.FacilityManagementWeb.csproj -c Release -o $publishOutputFolder

Write-Host "Creating archive of the publish output for the Website"
Compress-Archive -Path "$publishOutputFolder/*" -DestinationPath "$deploymentZip"

Remove-Item -Path $publishOutputFolder -Recurse -Force

Write-Host "Publishing the Website to Azure"
az webapp deployment source config-zip --resource-group $resourceGroupName --name $facilityManagementWebsiteName --src $deploymentZip

Remove-Item -Path $deploymentZip -Recurse -Force
Write-Host "Publishing completed"
popd

# Publish the Web Api
$publishOutputFolder = "./webapp"
$deploymentZip = "./SmartHotel.Services.FacilityManagement.Deployment.zip"
Write-Host "Publishing the Facility Management api..."
pushd "../backend/src/SmartHotel.Services/SmartHotel.Services.FacilityManagement"

Write-Host "Running dotnet restore for the Facility Management Api project"
dotnet restore SmartHotel.Services.FacilityManagement.csproj

Write-Host "Running dotnet build for the Facility Management Api project"
dotnet build SmartHotel.Services.FacilityManagement.csproj -c Release -o $publishOutputFolder

Write-Host "Running dotnet publish for the Facility Management Api project"
dotnet publish SmartHotel.Services.FacilityManagement.csproj -c Release -o $publishOutputFolder

Write-Host "Creating archive of the publish output for the Facility Management Api"
Compress-Archive -Path "$publishOutputFolder/*" -DestinationPath "$deploymentZip"

Remove-Item -Path $publishOutputFolder -Recurse -Force

Write-Host "Publishing the Facility Management Api to Azure"
az webapp deployment source config-zip --resource-group $resourceGroupName --name $facilityManagementApiName --src $deploymentZip

Remove-Item -Path $deploymentZip -Recurse -Force
Write-Host "Publishing completed"
popd

# Publish the Function
$publishOutputFolder = "./functionapp"
$deploymentZip = "./SmartHotel.Services.SensorDataFunction.Deployment.zip"
Write-Host "Publishing the Azure Function..."
pushd "../backend/src/SmartHotel.Services/SmartHotel.Services.SensorDataFunction"

Write-Host "Running dotnet restore for the Azure Function project"
dotnet restore SmartHotel.Services.SensorDataFunction.csproj

Write-Host "Running dotnet build for the Azure Function project"
dotnet build SmartHotel.Services.SensorDataFunction.csproj -c Release -o $publishOutputFolder

Write-Host "Running dotnet publish for the Azure Function project"
dotnet publish SmartHotel.Services.SensorDataFunction.csproj -c Release -o $publishOutputFolder

Write-Host "Creating archive of the publish output for the Azure Function"
Compress-Archive -Path "$publishOutputFolder/*" -DestinationPath "$deploymentZip"

Remove-Item -Path $publishOutputFolder -Recurse -Force

Write-Host "Publishing the Azure Function to Azure"
az functionapp deployment source config-zip --resource-group $resourceGroupName --name $functionSiteName --src $deploymentZip

Remove-Item -Path $deploymentZip -Recurse -Force
Write-Host "Publishing completed"
popd

Write-Host
Write-Host
#Deploy devices to Kubernetes
Write-Host "Deploying Devices to Kubernetes..."
Write-Host

pushd "../backend/src/SmartHotel.Devices"
kubectl apply -f deployments.demo.yaml
popd

$facilityManagementWebsiteDefaultHostName = ((az webapp show -g $resourceGroupName -n $facilityManagementWebsiteName) | ConvertFrom-Json).defaultHostName
$facilityManagementWebsiteUri = "https://$facilityManagementWebsiteDefaultHostName"

Write-Host
Write-Host "Update CORS for FacilityManagementApi"
az webapp cors add -g $resourceGroupName -n $facilityManagementApiName --allowed-origins $facilityManagementWebsiteUri

Write-Host
Write-Host "Update AAD Application Reply Urls and OAuth2AllowImplicitFlow"

$localhostReplyUrl = "http://localhost/*"
$localhostReplyUrlWithoutWildcard = "http://localhost"
$postmanReplyUrl = "https://www.getpostman.com/oauth2/callback"
$facilityManagementWebsiteReplyUrl = "$facilityManagementWebsiteUri/*"

$desiredNewReplyUrls = @($localhostReplyUrl, $localhostReplyUrlWithoutWildcard, $postmanReplyUrl, $facilityManagementWebsiteReplyUrl)

$error.Clear()
$getExistingReplyUrlsResult = az ad app show --id $clientId
if(!$?)
{
    Reset-Console-Coloring
    Write-Host
    Write-Host
    Write-Host "Have a user with Azure AD permissions in your subscription update the reply urls to include the following for the AAD Application."
    foreach($replyUrl in $desiredNewReplyUrls)
    {
        Write-Host "$replyUrl"
    }
    Write-Host
    Write-Host 'ALSO, have a user with Azure AD permissions in your subscription update the Manifest for the AAD Application to have "oauth2AllowImplicitFlow" set to "true". Additional instructions can be found here: https://docs.microsoft.com/en-us/skype-sdk/websdk/docs/troubleshooting/auth/aadauth-enableimplicitoauth#the-solution'
    Read-Host -Prompt "Press ENTER to continue once these steps have been completed"
}
else
{
    $existingReplyUrls = ($getExistingReplyUrlsResult| ConvertFrom-Json).replyUrls
    $allReplyUrls = ""
    foreach($existingReplyUrl in $existingReplyUrls)
    {
        $allReplyUrls = $allReplyUrls + " $existingReplyUrl"
    }

    foreach($desiredReplyUrl in $desiredNewReplyUrls)
    {
        if(!$existingReplyUrls.Contains($desiredReplyUrl))
        {
            $allReplyUrls = $allReplyUrls + " $desiredReplyUrl"
        }
    }
    $allReplyUrls = $allReplyUrls.Trim()

    $updateReplyUrls = az ad app update $powershellEscape --id $clientId --password "$clientSecret" --reply-urls $allReplyUrls --oauth2-allow-implicit-flow "true"
}

Write-Host
Write-Host "*****************************************************************************************"
Write-Host "Provisoning is complete.  In order to complete frontend deployment, you will need to manually update settings for the Xamarin Client."

$savedSettings = [PSCustomObject]@{
    tenantId = $tenantId
    clientId = $clientId
    aadReplyUrl = $facilityManagementWebsiteReplyUrl
    digitalTwinsManagementEndpoint = $dtManagementEndpoint
    facilityManagementWebsiteUri = $facilityManagementWebsiteUri
    facilityManagementApiUri = $facilityManagementApiUri
    facilityManagementApiEndpoint = $facilityManagementApiEndpoint
    storageConnectionString = $storageConnectionString
    eventHubConsumerConnectionString = $eventHubConsumerConnnection
    iotHubConnectionString = $iotHubServiceConnectionString
    cosmosDbConnectionString = $cosmosDbConnectionString
    roomDevicesApiEndpoint = "http://$roomDevicesApiUri/api"
    room11SpaceId = $room11SpaceId 
};

$path = Get-Location
$outfile = $path.ToString() + '\userSettings.json'

$savedSettings | ConvertTo-Json | Out-File $outfile

Write-Host
Write-Host
Write-Host 'Required settings have been saved to ' $outfile
Write-Host
Write-Host