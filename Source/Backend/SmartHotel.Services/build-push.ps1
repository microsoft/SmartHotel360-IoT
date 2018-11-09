<#
 .SYNOPSIS
    Builds and pushes docker images to an Azure Container Registry

 .PARAMETER subscriptionId
    The subscription id where the template will be deployed.

 .PARAMETER acrName
    The name of the Azure Container Register (ACR) to push the built docker images to.
#>

param(
 [Parameter(Mandatory=$True)]
 [string]
 $subscriptionId,

 [Parameter(Mandatory=$True)]
 [string]
 $acrName
)

$acrName = $acrName.ToLower()
Write-Host "------------------------------------------------------------"
Write-Host "Logging into the registry $acrName"
Write-Host "------------------------------------------------------------"
az account set -s $subscriptionId
az acr login -n $acrName

Write-Host "------------------------------------------------------------"
Write-Host "Building Docker images"
Write-Host "------------------------------------------------------------"
docker-compose build --no-cache

Write-Host "------------------------------------------------------------"
Write-Host "Pushing :public images to $acrName.azurecr.io..."
Write-Host "------------------------------------------------------------"
$apis = @("roomdevices")
foreach ($api in $apis)
{
	$imageFqdn = "$acrName.azurecr.io/$api-api"
    $devImage = $imageFqdn + ":dev"
    $publicImage = $imageFqdn + ":public"
	docker tag $devImage $publicImage
	Write-Host "Pushing $publicImage"
	docker push $publicImage
}

Write-Host "------------------------------------------------------------"
Write-Host "Build and Push Completed"
Write-Host "------------------------------------------------------------"