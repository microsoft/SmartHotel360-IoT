using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning;
using SmartHotel.IoT.Provisioning.Common;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.ProvisioningRemoval
{
	public class Program
	{
		private const string AadInstance = "https://login.microsoftonline.com/";
		private const string DigitalTwinsResourceId = "0b07f429-9f4b-4714-9392-cc5e8e80c8b0";

		private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
		{
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore
		};

		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		[Option( "-t|--Tenant", Description = "Azure Tenant Id" )]
		[Required]
		public string Tenant { get; }

		[Option( "-ci|--ClientId", Description = "Azure Active Directory App Id" )]
		[Required]
		public string ClientId { get; }

		[Option( "-cs|--ClientSecret", Description = "Key from the Azure Active Directory App" )]
		[Required]
		public string ClientSecret { get; }

		[Option( "-dt|--DigitalTwinsApiEndpoint", Description = "Url for your Digital Twins resource e.g. (https://{resource name}.{resource location}.azuresmartspaces.net/management/api/v1.0" )]
		[Required]
		public string DigitalTwinsApiEndpoint { get; }

		[Option( "-re|--RemoveEndpoints", Description = "Flag to indicate if Endpoints should be removed" )]
		public bool RemoveEndpoints { get; }

        [Option("-dtpf|--DigitalTwinsProvisioningFile", Description = "Yaml file containing the tenant definition for Digital Twins provisioning")]
        [Required]
        public string DigitalTwinsProvisioningFile { get; }

        private async Task OnExecuteAsync()
		{
			HttpClient httpClient = await HttpClientHelper.GetHttpClientAsync( DigitalTwinsApiEndpoint, AadInstance, Tenant,
				DigitalTwinsResourceId, ClientId, ClientSecret );

			ProvisioningDescription provisioningDescription = ProvisioningHelper.LoadSmartHotelProvisioning(DigitalTwinsProvisioningFile);

			await RemoveAllExistingDevicesAsync( httpClient, provisioningDescription );

			IReadOnlyCollection<Space> rootSpaces = await GetRootSpacesAsync( httpClient, provisioningDescription );

			await RemoveAllExistingResourcesAsync( httpClient, rootSpaces );

			await RemoveAllExistingRootSpacesAsync( httpClient, rootSpaces );

			if ( RemoveEndpoints )
			{
				await RemoveAllExistingEndpoints( httpClient );
			}

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine( "Press Enter to continue..." );
			Console.ReadLine();
		}

		private static async Task RemoveAllExistingDevicesAsync( HttpClient httpClient, ProvisioningDescription provisioningDescription )
		{

			IDictionary<string, List<DeviceDescription>> deviceDictionary = provisioningDescription.spaces.GetAllDeviceDescriptions();
            ICollection<DeviceDescription> allDefinedDevices = deviceDictionary.Values.SelectMany(v => v).ToArray();
			IReadOnlyCollection<Device> existingDevices = await allDefinedDevices.GetExistingDevicesAsync( httpClient );

			if ( existingDevices == null || !existingDevices.Any() )
			{
				return;
			}

			Console.WriteLine();
			Console.WriteLine( $"Removing {existingDevices.Count} devices with the following hardware ids:" +
								  $" {string.Join( ", ", existingDevices.Select( d => d.HardwareId ) )}" );
			foreach ( Device deviceToRemove in existingDevices )
			{
				bool success = await deviceToRemove.DeleteDeviceAsync( httpClient, JsonSerializerSettings );
				if ( !success )
				{
					Console.WriteLine( $"Failed to remove Device, please try manually. (Id: {deviceToRemove.Id})" );
				}
			}

			Console.WriteLine();
		}

		private static async Task<IReadOnlyCollection<Space>> GetRootSpacesAsync( HttpClient httpClient,
			ProvisioningDescription provisioningDescription )
		{
			var rootSpaces = new List<Space>();
			foreach ( SpaceDescription spaceDescription in provisioningDescription.spaces )
			{
				Space existingSpace =
					await SpaceHelpers.GetUniqueSpaceAsync( httpClient, spaceDescription.name, Guid.Empty, JsonSerializerSettings );
				if ( existingSpace != null )
				{
					rootSpaces.Add( existingSpace );
				}
			}

			return rootSpaces.AsReadOnly();
		}

		private static async Task RemoveAllExistingResourcesAsync( HttpClient httpClient, IReadOnlyCollection<Space> rootSpaces )
		{
			if ( !rootSpaces.Any() )
			{
				return;
			}

			Console.WriteLine();

			IReadOnlyCollection<Resource> resourcesToRemove = await rootSpaces.GetExistingResourcesUnderSpacesAsync( httpClient );

			Console.WriteLine( $"Removing {resourcesToRemove.Count} resources." );

			var resourceIdsDeleting = new List<Guid>();
			foreach ( Resource resourceToRemove in resourcesToRemove )
			{
				bool success = await resourceToRemove.DeleteResourceAsync( httpClient, JsonSerializerSettings );
				if ( success )
				{
					resourceIdsDeleting.Add( Guid.Parse( resourceToRemove.Id ) );
				}
				else
				{
					Console.WriteLine( $"Failed to remove Resource, please try manually. (Id: {resourceToRemove.Id})" );
				}
			}

			if ( resourceIdsDeleting.Any() )
			{
				Console.WriteLine( "Polling until all resources have been deleted." );
				var tokenSource = new CancellationTokenSource();

				ICollection<Task> statusVerificationTasks = resourceIdsDeleting
					.Select( resourceId => ResourcesHelpers.WaitTillResourceDeletionCompletedAsync( httpClient, resourceId, tokenSource.Token ) )
					.ToArray();

				// Ensuring that it won't sit here forever.
				Task processingTasks = Task.WhenAny( Task.WhenAll( statusVerificationTasks ), Task.Delay( TimeSpan.FromMinutes( 10 ) ) );

				ConsoleSpinner spinner = new ConsoleSpinner();
				while (!processingTasks.IsCompleted)
				{
					Console.CursorVisible = false;
					spinner.Turn();
					await Task.Delay(250);
				}

				if ( statusVerificationTasks.Any( t => t.Status != TaskStatus.RanToCompletion ) )
				{
					// Timeout occurred, need to cancel all the other tasks
					tokenSource.Cancel();

					Console.WriteLine( "Timeout occurred. Unable to verify that all resources have been deleted." );
				}
				else
				{
					Console.WriteLine( "Resource deletion complete." );
				}
			}

			Console.WriteLine();
		}

		private static async Task RemoveAllExistingRootSpacesAsync( HttpClient httpClient, IReadOnlyCollection<Space> rootSpaces )
		{
			if ( !rootSpaces.Any() )
			{
				return;
			}

			Console.WriteLine();
			Console.WriteLine( $"Removing {rootSpaces.Count} root spaces with the following names:" +
							  $" {string.Join( ", ", rootSpaces.Select( s => s.Name ) )}" );
			foreach ( Space spaceToRemove in rootSpaces )
			{
				bool success = await spaceToRemove.DeleteSpaceAsync( httpClient, JsonSerializerSettings );
				if ( !success )
				{
					Console.WriteLine( $"Failed to remove Space, please try manually. (Id: {spaceToRemove.Id})" );
				}
			}
			Console.WriteLine();
		}

		private static async Task RemoveAllExistingEndpoints( HttpClient httpClient )
		{
			IReadOnlyCollection<Endpoint> existingEndpoints = await EndpointHelpers.GetEndpointsAsync( httpClient, JsonSerializerSettings );

			if ( !existingEndpoints.Any() )
			{
				return;
			}

			Console.WriteLine();

			Console.WriteLine( $"Removing {existingEndpoints.Count} endpoints." );
			foreach ( Endpoint endpointToRemove in existingEndpoints )
			{
				bool success = await endpointToRemove.DeleteEndpointAsync( httpClient, JsonSerializerSettings );
				if ( !success )
				{
					Console.WriteLine( $"Failed to remove Endpoint, please try manually. (Id: {endpointToRemove.Id})" );
				}
			}

			Console.WriteLine();
		}
	}
}
