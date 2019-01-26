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

 .PARAMETER userAzureObjectIdsFilePath
    Optional, path to the file containing the Azure Object Ids for the various users that need RBAC (Role Based Access Control) permissions in Digital Twins.

 .PARAMETER digitalTwinsProvisioningTemplateFilePath
    Optional, path to the template file used to provision the Digital Twins instance along with device related utilities.

 .PARAMETER numberOfAksNodes
    Optional, number of nodes to have in the Azure Kubernetes service.

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
 $userAzureObjectIdsFilePath = "UserAADObjectIds.json",

 [string]
 $digitalTwinsProvisioningTemplateFilePath = "DigitalTwinsProvisioning-Demo/SmartHotel_Site_Provisioning.yaml",

 [int]
 $numberOfAksNodes = 3,

 [string]
 $templateFilePath = "template.json",

 [string]
 $parametersFilePath = "parameters.json"
)

function Reset-Console-Coloring {
    $Host.UI.RawUI.BackgroundColor = ($bckgrnd = 'DarkBlue')
    $Host.UI.RawUI.ForegroundColor = 'White'
}

# https://markheath.net/post/managing-azure-function-keys
function getKuduCreds([string]$appName, [string]$resourceGroup)
{
    $user = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
            --query "[?publishMethod=='MSDeploy'].userName" -o tsv

    $pass = az webapp deployment list-publishing-profiles -n $appName -g $resourceGroup `
            --query "[?publishMethod=='MSDeploy'].userPWD" -o tsv

    $pair = "$($user):$($pass)"
    $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
    return $encodedCreds
}

function getFunctionKey([string]$appName, [string]$functionName, [string]$encodedCreds)
{
    $jwt = Invoke-RestMethod -Uri "https://$appName.scm.azurewebsites.net/api/functions/admin/token" -Headers @{Authorization=("Basic {0}" -f $encodedCreds)} -Method GET

    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization=("Bearer {0}" -f $jwt)} `
            -Uri "https://$appName.azurewebsites.net/admin/functions/$functionName/keys" 

    $code = $keys.keys[0].value
    return $code
}

#******************************************************************************
# Script body
# Execution begins here
#******************************************************************************
Reset-Console-Coloring
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ErrorActionPreference = "Stop"
$powershellEscape = '--%'
$startTime = Get-Date

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
    Reset-Console-Coloring
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

$TemplateParameters = "_currentDateTimeInTicks=$UTCTimeTick _clientId=$clientId"

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
$aksClusterCreationResult = az aks create --resource-group "$resourceGroupName" --name "$aksClusterName" --node-count $numberOfAksNodes --service-principal "$aksServicePrincipalId" --client-secret "$aksServicePrincipalKey" --location "$aksClusterLocation" --generate-ssh-keys
Write-Host "Finished Creating the AKS Cluster"

$EndTimeLocal = Get-Date
Write-Host "Finished deployment at $EndTimeLocal (local time)...";
Write-Host

$iotHubName = $outputs.iotHubName.value
$iotHubServiceConnectionString = ((az iot hub show-connection-string -n $iotHubName --resource-group $resourceGroupName --policy-name service) | ConvertFrom-Json).cs
$iotHubRegistryReadWriteConnectionString = ((az iot hub show-connection-string -n $iotHubName --resource-group $resourceGroupName --policy-name registryReadWrite) | ConvertFrom-Json).cs

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

$dtProvisionTemplateFullFilePath = (Resolve-Path $digitalTwinsProvisioningTemplateFilePath).Path
$userAzureObjectIdsFullFilePath = (Resolve-Path $userAzureObjectIdsFilePath).Path

$storageAccountName = $outputs.storageAccountName.value

$storageConnectionString = ((az storage account show-connection-string -g $resourceGroupName -n $storageAccountName) | ConvertFrom-Json).connectionString

#Provision IoT Devices
Write-Host
Write-Host

Write-Host "Provisioning IoT Devices..."

$iotProvisioningOutput = 'iot-device-connectionstring.json'

Push-Location "../Provisioning/IoTHubDeviceProvisioningBits"
$iotHubDeviceProvisioningArgs = "-iotr `"$iotHubRegistryReadWriteConnectionString`" -ascs `"$storageConnectionString`" -dtpf `"$dtProvisionTemplateFullFilePath`" -o `"$iotProvisioningOutput`""
dotnet SmartHotel.IoT.IoTHubDeviceProvisioning.dll $powershellEscape $iotHubDeviceProvisioningArgs
if( -not (Test-Path $iotProvisioningOutput))
{
    Write-Error "An error occurred while creating the IoT Hub devices. Please attempt to fix the issue and re-deploy."
    exit
}

Copy-Item $iotProvisioningOutput -Destination "../ProvisioningDevicesBits"
Pop-Location

$eventHubConsumerConnnection = $outputs.eventHubConsumerConnnection.value
$eventHubProducerConnnection = $outputs.eventHubProducerConnnection.value
$eventHubProducerSecondaryConnnection = $outputs.eventHubProducerSecondaryConnnection.value
$eventHubName = $outputs.eventHubName.value

$provisioningOutput = 'ProvisioningOutput.json'

Write-Host "Provisioning Digital Twins Topology..."

Push-Location "../Provisioning/ProvisioningBits/"
$dtProvisioningArgs = "-t `"$tenantId`" -ci `"$clientId`" -cs `"$clientSecret`" -dt `"$dtApiEndpoint`" -ehcs `"$eventHubProducerConnnection`" -ehscs `"$eventHubProducerSecondaryConnnection`" -ehn `"$eventHubName`" -oids `"$userAzureObjectIdsFullFilePath`" -dtpf `"$dtProvisionTemplateFullFilePath`" -o `"$provisioningOutput`""
dotnet SmartHotel.IoT.Provisioning.dll $powershellEscape $dtProvisioningArgs
if( -not (Test-Path $provisioningOutput))
{
    Write-Error "An error occurred while provisioning Azure Digital Twins. Please attempt to fix the issue and re-deploy."
    exit
}

Copy-Item $provisioningOutput -Destination "../ProvisioningDevicesBits"
$demoRoomName = 'SmartHotel360-SH360Elite1-Room101'
$demoRoomNameLowercase = $demoRoomName.ToLower()
$demoRoom = (Get-Content "$provisioningOutput" | Out-String | ConvertFrom-Json).$demoRoomName[0]
$demoRoomSpaceId = $demoRoom.SpaceId
$demoRoomDeviceHardwareId = $demoRoom.hardwareId
$demoRoomDeviceSaSToken = $demoRoom.SasToken
$demoRoomKubernetesDeploymentName = "sh.d.room.$demoRoomNameLowercase"
Pop-Location

#Update Devices and Services Docker/Kubernetes yaml

Write-Host "Provisioning Device sample applications..."

Push-Location "../Provisioning/ProvisioningDevicesBits/"
$deviceProvisioningArgs = "-dt `"$dtManagementEndpoint`" -i $provisioningOutput -d `"../../Backend/SmartHotel.Devices/`" -cr `"$acrName`" -iot `"$iotProvisioningOutput`""
dotnet SmartHotel.IoT.ProvisioningDevices.dll $powershellEscape $deviceProvisioningArgs
Pop-Location

Write-Host "Provisioning APIs..."

Push-Location "../Provisioning/ProvisioningApisBits/"
$apiProvisioningArgs = "-dt `"$dtManagementEndpoint`" -d `"../../Backend/SmartHotel.Services/`" -cr `"$acrName`" -iot `"$iotHubServiceConnectionString`" -db `"$cosmosDbConnectionString`""
dotnet SmartHotel.IoT.ProvisioningApis.dll $powershellEscape $apiProvisioningArgs
Pop-Location

#Build and publish devices and services containers
Write-Host "Building and publishing device images..."
Push-Location "../Backend/SmartHotel.Devices"
./build-push.ps1 -subscriptionId $subscriptionId -acrName $acrName
Pop-Location

Write-Host "Building and publishing service images..."
Push-Location "../Backend/SmartHotel.Services"
./build-push.ps1 -subscriptionId $subscriptionId -acrName $acrName
Pop-Location

az aks get-credentials --resource-group "$resourceGroupName" --name "$aksClusterName"

Write-Host
Write-Host
#Deploy service(s) to Kubernetes
Write-Host "Deploying Services to Kubernetes..."
Push-Location "../Backend/SmartHotel.Services"
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
Pop-Location

$facilityManagementApiName = $outputs.webapiName.value
$facilityManagementApiDefaultHostName = ((az webapp show -g $resourceGroupName -n $facilityManagementApiName) | ConvertFrom-Json).defaultHostName
$facilityManagementApiUri = "https://$facilityManagementApiDefaultHostName"
$functionSiteName = $outputs.functionSiteName.value

Write-Host
Write-Host "Setting Facility Manangement Api App Settings"
$facilityManagementApiSettings = "--settings ManagementApiUrl=`"$dtManagementEndpoint`" MongoDBConnectionString=`"$cosmosDbConnectionString`" AzureAd__Audience=`"$clientId`" IoTHubConnectionString=`"$iotHubServiceConnectionString`""
$facilityManagementApiSettingsResults = az webapp config appsettings set -n $facilityManagementApiName -g $resourceGroupName $powershellEscape $facilityManagementApiSettings
Write-Host "Setting Facility Manangement Api to be Always On"
$facilityManagementApiConfigResults = az webapp config set -n $facilityManagementApiName -g $resourceGroupName --always-on true
Write-Host "Setting Facility Manangement Api to be Https Only"
$facilityManagementApiUpdateResults = az webapp update -n $facilityManagementApiName -g $resourceGroupName --https-only true

Write-Host
Write-Host "Setting Azure Function App Settings"
$functionSettings = "--settings CosmosDBConnectionString=`"$cosmosDbConnectionString`" EventHubConnectionString=`"$eventHubConsumerConnnection`" AzureWebJobsDashboard=`"$storageConnectionString`" AzureWebJobsStorage=`"$storageConnectionString`""
$functionSettingsResults = az webapp config appsettings set -n $functionSiteName -g $resourceGroupName $powershellEscape $functionSettings

$facilityManagementWebsiteName = $outputs.websiteName.value
$facilityManagementApiEndpoint = "$facilityManagementApiUri/api"
$azureMapsKey = $outputs.mapsPrimaryKey.value

$adalEndpointsJson = @(
    @{
        url = "`"$facilityManagementApiUri`""
        resourceId = "`"$clientId`""
    }
)

$adalEndpointsString = ConvertTo-Json -InputObject $adalEndpointsJson -Compress

Write-Host
Write-Host "Setting Facility Management Website App Settings"
$websiteSettings = "--settings adalConfig__tenant=`"$tenantId`" adalConfig__clientId=`"$clientId`" adalConfig__endpointsString=`"$adalEndpointsString`" apiEndpoint=`"$facilityManagementApiEndpoint`" azureMapsKey=`"$azureMapsKey`""
$websiteSettingsResults = az webapp config appsettings set -n $facilityManagementWebsiteName -g $resourceGroupName $powershellEscape $websiteSettings
Write-Host "Setting Facility Manangement Website to be Https Only"
$websiteConfigResults = az webapp config set -n $facilityManagementWebsiteName -g $resourceGroupName --always-on true
Write-Host "Setting Facility Manangement Website to be Https Only"
$websiteUpdateResults = az webapp update -n $facilityManagementWebsiteName -g $resourceGroupName --https-only true

Write-Host
Write-Host "Updating Facility Management Website environment files to point to the deployed azure resources when running locally."
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
    $fileContent = $fileContent.Replace("{azureMapsKey}","$azureMapsKey")
    
    $fileContent | Set-Content $fullFilePath -Force

    Write-Host "Update complete."
    Write-Host
}

# Publish the Website
$publishOutputFolder = "./webapp"
$deploymentZip = "./SmartHotel.FacilityManagementWeb.Deployment.zip"
Write-Host "Publishing the Facility Management website..."
Push-Location "../FacilityManagementWebsite/SmartHotel.FacilityManagementWeb/SmartHotel.FacilityManagementWeb"

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
Pop-Location

# Publish the Web Api
$publishOutputFolder = "./webapp"
$deploymentZip = "./SmartHotel.Services.FacilityManagement.Deployment.zip"
Write-Host "Publishing the Facility Management api..."
Push-Location "../Backend/SmartHotel.Services/SmartHotel.Services.FacilityManagement"

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
Pop-Location

# Publish the Function
$publishOutputFolder = "./functionapp"
$deploymentZip = "./SmartHotel.Services.SensorDataFunction.Deployment.zip"
Write-Host "Publishing the Azure Function..."
Push-Location "../Backend/SmartHotel.Services/SmartHotel.Services.SensorDataFunction"

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

$deviceRelayFunctionName = "DeviceRelayFunction"
$encodedFunctionCreds = getKuduCreds -appName $functionSiteName -resourceGroup $resourceGroupName
$deviceRelayFunctionKey = getFunctionKey -appName $functionSiteName -functionName $deviceRelayFunctionName -encodedCreds $encodedFunctionCreds

Remove-Item -Path $deploymentZip -Recurse -Force
Write-Host "Publishing completed"
Pop-Location

Write-Host
Write-Host "Updating the MXChip Device's config.h to point to the deployed azure resources."
Write-Host

$deviceRelayFunctionEndpoint = "https://$functionSiteName.azurewebsites.net/api/$deviceRelayFunctionName"

$mxChipDeviceConfigFile = "../Backend/SmartHotel.PhysicalDevices/SmartHotel.PhysicalDevices.MXChip/Device/config.h"
$fullMxDeviceConfigFilePath = Resolve-Path "$mxChipDeviceConfigFile"

Write-Host "Updating $fullMxDeviceConfigFilePath..."

$mxDeviceConfigFileContent = Get-Content $fullMxDeviceConfigFilePath -raw
$mxDeviceConfigFileContent = $mxDeviceConfigFileContent.Replace("{DTSasToken}","$demoRoomDeviceSaSToken")
$mxDeviceConfigFileContent = $mxDeviceConfigFileContent.Replace("{DTHardwareId}","$demoRoomDeviceHardwareId")
$mxDeviceConfigFileContent = $mxDeviceConfigFileContent.Replace("{DigitalTwinsManagementApiEndpoint}","$dtApiEndpoint")
$mxDeviceConfigFileContent = $mxDeviceConfigFileContent.Replace("{DeviceRelayFunctionEndpoint}","$deviceRelayFunctionEndpoint")
$mxDeviceConfigFileContent = $mxDeviceConfigFileContent.Replace("{DeviceRelayFunctionKey}","$deviceRelayFunctionKey")

$mxDeviceConfigFileContent | Set-Content $fullMxDeviceConfigFilePath -Force

Write-Host "Update complete."
Write-Host

Write-Host
Write-Host
#Deploy devices to Kubernetes
Write-Host "Deploying Devices to Kubernetes..."
Write-Host

Push-Location "../Backend/SmartHotel.Devices"
kubectl apply -f deployments.demo.yaml
Pop-Location

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
    azureMapsKey = $azureMapsKey
    roomDevicesApiEndpoint = "http://$roomDevicesApiUri/api"
    demoRoomSpaceId = $demoRoomSpaceId
    demoRoomKubernetesDeployment = $demoRoomKubernetesDeploymentName
    deviceRelayFunctionEndpoint = $deviceRelayFunctionEndpoint
    deviceRelayFunctionKey = $deviceRelayFunctionKey
};

$path = Get-Location
$outfile = $path.ToString() + '/userSettings.json'

$savedSettings | ConvertTo-Json | Out-File $outfile

$endTime = Get-Date
$totalTimeInMinutes = ($endTime - $startTime).TotalMinutes

Write-Host
Write-Host
Write-Host 'Required settings have been saved to ' $outfile
Write-Host
Write-Host "Deployment took $totalTimeInMinutes minutes"
Write-Host