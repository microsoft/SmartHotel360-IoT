using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common;
using SmartHotel.IoT.Provisioning.Common.Models;

namespace SmartHotel.IoT.IoTHubDeviceProvisioning
{
	class Program
	{
		private const string CreatedDevicesJsonFileName = "CreatedDevices.json";
		private static string _actionMessage = "Creating";
		private static string _actionMessagePastTense = "Created";

		private RegistryManager _registryManager;
		private CloudBlobContainer _deviceExportContainer;
		private IotHubConnectionStringBuilder _iotHubConnectionStringBuilder;
		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		[Option( "-iotr|--IoTHubRegistryConnectionString", Description = "Connection string used to make read/write calls on the IoT Hub Registry." )]
		[Required]
		public string IoTHubRegistryConnectionString { get; set; }

		[Option( "-ascs|--AzureStorageConnectionString", Description = "Connection string to access Azure Storage account." )]
		[Required]
		public string AzureStorageConnectionString { get; set; }

		[Option( "-dtpf|--DigitalTwinsProvisioningFile", Description =
			"Yaml file containing the tenant definition for Digital Twins provisioning" )]
		[Required]
		public string DigitalTwinsProvisioningFile { get; }

		[Option( "-o|--output", Description =
			"Name of the file to save provisioning data to.  This is used by SmartHotel.IoT.ProvisioningDevices to configure device settings" )]
		public string OutputFile { get; } = "iot-device-connectionstring.json";

		//[Option( "-s|--SubscriptionId", Description = "Id of the Azure subscription to select. If specified \"az login\" will be executed first." )]
		//public string SubscriptionId { get; }

		[Option( "-rd|--RemoveDevices", Description = "Whether devices should be removed or created." )]
		public bool RemoveDevices { get; }

		private async Task OnExecuteAsync()
		{
			try
			{
				if ( RemoveDevices )
				{
					_actionMessage = "Removing";
					_actionMessagePastTense = "Removed";
				}

				_iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create( IoTHubRegistryConnectionString );
				_registryManager = RegistryManager.CreateFromConnectionString( IoTHubRegistryConnectionString );

				Console.WriteLine( "Loading the provisioning files..." );

				ProvisioningDescription provisioningDescription = ProvisioningHelper.LoadSmartHotelProvisioning( DigitalTwinsProvisioningFile );

				Console.WriteLine( "Successfully loaded provisioning files." );

				if ( !string.IsNullOrEmpty( OutputFile ) )
				{
					var sw = Stopwatch.StartNew();
					IDictionary<string, List<DeviceDescription>> allDevices =
						provisioningDescription.spaces.GetAllDeviceDescriptionsByDeviceIdPrefix( string.Empty );

					Console.WriteLine( $"{_actionMessage} IoT Hub devices..." );
					Console.WriteLine();
					Console.WriteLine();

					IDictionary<string, string> deviceConnectionStringsByPrefix = await CreateIoTHubDevicesAndGetConnectionStringsAsync( allDevices );
					sw.Stop();

					Console.WriteLine();
					Console.WriteLine();
					Console.WriteLine( $"{_actionMessagePastTense} IoT Hub devices successfully in {sw.Elapsed.TotalMinutes} minutes." );

					if ( !RemoveDevices )
					{
						await File.WriteAllTextAsync( OutputFile,
							JsonConvert.SerializeObject(
								new SortedDictionary<string, string>( deviceConnectionStringsByPrefix ) ) );
					}
				}


				Console.WriteLine();
				Console.WriteLine();
			}
			catch ( Exception ex )
			{
				Console.WriteLine( $"Error occurred during IoT Hub Device provisioning: {ex}" );
			}
		}

		private async Task<IDictionary<string, string>> CreateIoTHubDevicesAndGetConnectionStringsAsync(
			IDictionary<string, List<DeviceDescription>> allDevices )
		{
			var result = new ConcurrentDictionary<string, string>();

			string[] deviceIds = allDevices.Keys.ToArray();

			Console.WriteLine( $"{_actionMessage} devices..." );
			if ( RemoveDevices )
			{
				await RemoveDevicesAsync( deviceIds );
			}
			else
			{
				const int maxDevicesPerCall = 100;
				int numberOfCallsRequired = (int)Math.Ceiling( deviceIds.Length / (double)maxDevicesPerCall );
				for ( int i = 0; i < numberOfCallsRequired; i++ )
				{
					string[] deviceIdsForCall = deviceIds.Skip( i * maxDevicesPerCall ).Take( maxDevicesPerCall ).ToArray();
					BulkRegistryOperationResult bulkCreationResult =
						await _registryManager.AddDevices2Async( deviceIdsForCall.Select( id => new Device( id ) ) );
					if ( !bulkCreationResult.IsSuccessful )
					{
						Console.WriteLine( "Failed creating devices..." );
						foreach ( DeviceRegistryOperationError creationError in bulkCreationResult.Errors )
						{
							Console.WriteLine( $"Device {creationError.DeviceId}: {creationError.ErrorStatus}" );
						}

						throw new Exception();
					}
				}
			}

			Console.WriteLine( $"{_actionMessage} devices completed." );


			if ( !RemoveDevices )
			{
				Console.WriteLine("Retrieving connection strings...");
				await InitializeBlobContainerAsync();

				string sasToken = _deviceExportContainer.GetSharedAccessSignature( new SharedAccessBlobPolicy(), "saspolicy" );

				var containerSasUri = $"{_deviceExportContainer.Uri}{sasToken}";

				var job = await _registryManager.ExportDevicesAsync( containerSasUri, false );

				while ( true )
				{
					job = await _registryManager.GetJobAsync( job.JobId );

					if ( job.Status == JobStatus.Completed
						|| job.Status == JobStatus.Failed
						|| job.Status == JobStatus.Cancelled )
					{
						// Job has finished executing

						break;
					}

					await Task.Delay( TimeSpan.FromSeconds( 5 ) );
				}

				var exportedDevices = new List<ExportImportDevice>();
				var blob = _deviceExportContainer.GetBlobReference( "devices.txt" );
				using ( var streamReader = new StreamReader( await blob.OpenReadAsync(), Encoding.UTF8 ) )
				{
					while ( streamReader.Peek() != -1 )
					{
						string line = await streamReader.ReadLineAsync();
						var device = JsonConvert.DeserializeObject<ExportImportDevice>( line );
						exportedDevices.Add( device );
					}
				}

				await _deviceExportContainer.DeleteIfExistsAsync();

				await File.WriteAllTextAsync( CreatedDevicesJsonFileName, JsonConvert.SerializeObject( exportedDevices ) );

				foreach ( ExportImportDevice device in exportedDevices.OrderBy( d => d.Id ) )
				{
					result[device.Id] =
						$"HostName={_iotHubConnectionStringBuilder.HostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
				}

				Console.WriteLine("Retrieval complete.");
			}

			return result;
		}

		private async Task RemoveDevicesAsync( string[] deviceIds )
		{
			if ( File.Exists( CreatedDevicesJsonFileName ) )
			{
				var contents = await File.ReadAllTextAsync( CreatedDevicesJsonFileName );
				var createdDevices = JsonConvert.DeserializeObject<List<ExportImportDevice>>( contents );
				const int maxDevicesPerCall = 100;
				int numberOfCallsRequired = (int)Math.Ceiling( deviceIds.Length / (double)maxDevicesPerCall );
				for ( int i = 0; i < numberOfCallsRequired; i++ )
				{
					string[] deviceIdsForCall = deviceIds.Skip( i * maxDevicesPerCall ).Take( maxDevicesPerCall ).ToArray();
					BulkRegistryOperationResult bulkRemovalResult =
						await _registryManager.RemoveDevices2Async( deviceIdsForCall.Select( id => new Device( id )
						{
							ETag = createdDevices.FirstOrDefault( d => d.Id == id )?.ETag
						} ) );
					if ( !bulkRemovalResult.IsSuccessful )
					{
						Console.WriteLine( "Failed removing devices..." );
						foreach ( DeviceRegistryOperationError creationError in bulkRemovalResult.Errors )
						{
							Console.WriteLine( $"Device {creationError.DeviceId}: {creationError.ErrorStatus}" );
						}

						throw new Exception();
					}
				}
			}
			else
			{
				foreach ( var device in deviceIds.Select( id => new Device( id ) ) )
				{
					try
					{
						Console.WriteLine( $"{_actionMessage} device: {device.Id}" );
						await _registryManager.RemoveDeviceAsync( device.Id );
						Console.WriteLine( $"Finished for device: {device.Id}" );
					}
					catch ( DeviceNotFoundException )
					{
						Console.WriteLine( "Device not found, continuing" );
					}
				}
			}
		}

		private async Task InitializeBlobContainerAsync()
		{
			var storageAccount = CloudStorageAccount.Parse( AzureStorageConnectionString );
			var client = storageAccount.CreateCloudBlobClient();
			string iotDevicesContainerName = "iotdevices";
			_deviceExportContainer = client.GetContainerReference( iotDevicesContainerName );
			await _deviceExportContainer.CreateIfNotExistsAsync();

			var permissions = new BlobContainerPermissions
			{
				PublicAccess = BlobContainerPublicAccessType.Off
			};

			permissions.SharedAccessPolicies.Add(
				"saspolicy",
				new SharedAccessBlobPolicy()
				{
					SharedAccessExpiryTime = DateTime.UtcNow.AddHours( 1 ),
					Permissions = SharedAccessBlobPermissions.Write
								  | SharedAccessBlobPermissions.Read
								  | SharedAccessBlobPermissions.Delete
				} );

			await _deviceExportContainer.SetPermissionsAsync( permissions );
		}
	}
}
