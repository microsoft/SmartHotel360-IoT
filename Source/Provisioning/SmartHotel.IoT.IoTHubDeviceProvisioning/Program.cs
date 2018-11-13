using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using SmartHotel.IoT.IoTHubDeviceProvisioning.Extensions;
using SmartHotel.IoT.Provisioning.Common;
using SmartHotel.IoT.Provisioning.Common.Extensions;
using SmartHotel.IoT.Provisioning.Common.Models;

namespace SmartHotel.IoT.IoTHubDeviceProvisioning
{
	class Program
	{
		private static string _actionMessage = "Creating";
		private static string _actionMessagePastTense = "Created";
		private RetryPolicy _retryPolicy;
		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		[Option( "-az|--AzureCliPath", Description = "Path to the Azure Cli." )]
		[Required]
		public string AzureCliPath { get; }

		[Option( "-iot|--IoTHubName", Description = "Name of the IoT Hub to create devices in." )]
		[Required]
		public string IoTHubName { get; }

		[Option( "-dtpf|--DigitalTwinsProvisioningFile", Description =
			"Yaml file containing the tenant definition for Digital Twins provisioning" )]
		[Required]
		public string DigitalTwinsProvisioningFile { get; }

		[Option( "-o|--output", Description =
			"Name of the file to save provisioning data to.  This is used by SmartHotel.IoT.ProvisioningDevices to configure device settings" )]
		public string OutputFile { get; } = "iot-device-connectionstring.json";

		[Option( "-s|--SubscriptionId", Description = "Id of the Azure subscription to select. If specified \"az login\" will be executed first." )]
		public string SubscriptionId { get; }

		[Option( "-rd|--RemoveDevices", Description = "Whether devices should be removed or created." )]
		public bool RemoveDevices { get; }

		private async Task OnExecuteAsync()
		{
			try
			{
				_retryPolicy = Policy.Handle<ThrottlingBacklogTimeoutException>()
					.WaitAndRetry( 5, retryAttempt => TimeSpan.FromSeconds( 10 * retryAttempt ),
						( ex, t ) => Console.WriteLine( $"Device action throttled, retrying in {t.TotalSeconds} seconds..." ) );

				if ( RemoveDevices )
				{
					_actionMessage = "Removing";
					_actionMessagePastTense = "Removed";
				}

				if ( !string.IsNullOrWhiteSpace( SubscriptionId ) )
				{
					Console.WriteLine( "Logging into Azure..." );
					ProcessExecutionResult loginResult = AzureCliPath.ExecuteProcess( "login" );
					if ( ErrorOccurred( loginResult ) )
					{
						return;
					}

					ProcessExecutionResult accountSetResult = AzureCliPath.ExecuteProcess( $"account set -s {SubscriptionId}" );
					if ( ErrorOccurred( accountSetResult ) )
					{
						return;
					}
				}

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

					IDictionary<string, string> deviceConnectionStringsByPrefix = CreateIoTHubDevicesAndGetConnectionStrings( allDevices );

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

		private IDictionary<string, string> CreateIoTHubDevicesAndGetConnectionStrings( IDictionary<string, List<DeviceDescription>> allDevices )
		{
			var result = new ConcurrentDictionary<string, string>();
			Parallel.ForEach( allDevices,
				kvp =>
				{
					string deviceIdPrefix = kvp.Key;

					string deviceId = $"{deviceIdPrefix}";

					Console.WriteLine( $"{_actionMessage} device: {deviceId}" );

					_retryPolicy.Execute( () => ExecuteIoTHubAction( deviceId ) );

					if ( !RemoveDevices )
					{
						string deviceConnectionString = _retryPolicy.Execute( () => ExecuteGetDeviceConnectionString( deviceId ) );
						result[deviceIdPrefix] = deviceConnectionString;
					}
				} );

			return result;
		}


		private void ExecuteIoTHubAction( string deviceId )
		{
			string iotHubAction = RemoveDevices ? "delete" : "create";

			ProcessExecutionResult iotHubActionResult = AzureCliPath.ExecuteProcess( $"iot hub device-identity {iotHubAction} --hub-name {IoTHubName} --device-id {deviceId}" );
			if ( ErrorOccurred( iotHubActionResult, false ) )
			{
				string message = $"Failed {_actionMessage} device: {iotHubActionResult.Error}";
				if ( iotHubActionResult.Error.Contains( "ThrottlingBacklogTimeout", StringComparison.OrdinalIgnoreCase ) )
				{
					throw new ThrottlingBacklogTimeoutException( message );
				}

				if ( iotHubActionResult.Error.Contains( "DeviceNotFound", StringComparison.OrdinalIgnoreCase ) )
				{
					Console.WriteLine( $"Unable to find device with id: {deviceId}" );
				}
				else
				{
					throw new Exception( message );
				}
			}
		}

		private string ExecuteGetDeviceConnectionString( string deviceId )
		{
			ProcessExecutionResult connectionStringResult = AzureCliPath.ExecuteProcess(
				$"iot hub device-identity show-connection-string --hub-name {IoTHubName} --device-id {deviceId}" );
			if ( ErrorOccurred( connectionStringResult, false ) )
			{
				string message = $"Failed getting device connection string: {connectionStringResult.Error}";
				if ( connectionStringResult.Error.Contains( "ThrottlingBacklogTimeout", StringComparison.OrdinalIgnoreCase ) )
				{
					throw new ThrottlingBacklogTimeoutException( message );
				}

				throw new Exception( message );
			}

			string deviceConnectionString = JObject.Parse( connectionStringResult.Output ).Value<string>( "cs" );
			return deviceConnectionString;
		}

		private bool ErrorOccurred( ProcessExecutionResult result, bool outputError = true )
		{
			if ( result.HasError && outputError )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine( result.Error );
				Console.ResetColor();
			}

			return result.HasError;
		}
	}
}
