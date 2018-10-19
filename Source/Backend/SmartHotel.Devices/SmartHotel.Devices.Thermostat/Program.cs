using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using SmartHotel.Devices.Thermostat.Models;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace SmartHotel.Devices.Thermostat
{
	class SmartThermostat
	{
		private static readonly string ManagementApiUrlSetting = "ManagementApiUrl";
		private static readonly string SasTokenSetting = "SasToken";
		private static readonly string IoTHubDeviceConnectionStringSetting = "IoTHubDeviceConnectionString";
		private static readonly string HardwareIdSetting = "HardwareId";
		private static readonly string MessageIntervalInMilliSecondsSetting = "MessageIntervalInMilliSeconds";
		private static readonly string SensorDataTypeSetting = "SensorDataType";

		private static IConfiguration Configuration { get; set; }
		private static Device DeviceInfo { get; set; }
		private static DeviceClient TopologyDeviceClient { get; set; }
		private static DeviceClient HubDeviceClient { get; set; }

		private static int _lastCurrentTemperatureSent = int.MinValue;
		private static int _currentTemperature = 74;
		private static int _desiredValue = 74;
		private static readonly Random _randomOffset = new Random();
		private static int _offsetModifier = 0;
		private static Timer _offsetModifierTimer;

		private static async Task Main( string[] args )
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath( Directory.GetCurrentDirectory() )
				.AddJsonFile( "appsettings.json", true )
				.AddEnvironmentVariables();

			Configuration = builder.Build();

			CancellationTokenSource cts = new CancellationTokenSource();

			Console.CancelKeyPress += ( s, e ) =>
			{
				e.Cancel = true;
				cts.Cancel();
				Console.WriteLine( "Exiting..." );
			};

			Console.WriteLine( "SmartTheromstat - Simulate a Digital Twins Thermostat. Ctrl-C to exit.\n" );

			if ( !ValidateSettings() )
			{
				Console.WriteLine( "SmartThermostat - Your settings are invalid.  Please check your setting values and try again." );
				cts.Token.WaitHandle.WaitOne();
				return;
			}

			var hardwareId = Configuration[HardwareIdSetting];

			Console.WriteLine( $"Your hardware ID is: {hardwareId}" );

			var topologyClient = new TopologyClient( Configuration[ManagementApiUrlSetting], Configuration[SasTokenSetting] );
			DeviceInfo = topologyClient.GetDeviceForHardwareId( hardwareId ).Result;

			if ( DeviceInfo == null )
			{
				Console.WriteLine( "ERROR: Could not retrieve device information." );
				cts.Token.WaitHandle.WaitOne();
				return;
			}

			try
			{
				if ( !string.IsNullOrWhiteSpace( Configuration[IoTHubDeviceConnectionStringSetting] ) )
				{
					HubDeviceClient =
						DeviceClient.CreateFromConnectionString( Configuration[IoTHubDeviceConnectionStringSetting], TransportType.Mqtt );
					await HubDeviceClient.SetMethodHandlerAsync( "SetDesiredTemperature", SetDesiredTemperature, null );
				}
				else
				{
					_offsetModifierTimer = new Timer( s =>
						{
							int newOffset = _offsetModifier;
							while ( newOffset == _offsetModifier )
							{
								// Ensuring that every tick the offset will be randomly different than its current offset
								newOffset = _randomOffset.Next( -3, 4 );
							}
							Interlocked.Exchange( ref _offsetModifier, newOffset );
						}, null, 0, (int)TimeSpan.FromMinutes( 1 ).TotalMilliseconds );
				}

				Console.WriteLine( "Connection to Digital Twins: " + DeviceInfo.ConnectionString );

				TopologyDeviceClient = DeviceClient.CreateFromConnectionString( DeviceInfo.ConnectionString );

				if ( TopologyDeviceClient == null )
				{
					Console.WriteLine( "Failed to create DeviceClient!" );
					cts.Token.WaitHandle.WaitOne();
				}
				else
				{
					await Task.WhenAll( SimulateData( cts.Token ) );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Error in sample: {0}", ex.Message );
				cts.Token.WaitHandle.WaitOne();
			}
			finally
			{
				_offsetModifierTimer?.Dispose();
				_offsetModifierTimer = null;
			}
		}

		private static Task<MethodResponse> SetDesiredTemperature( MethodRequest methodRequest, object userContext )
		{
			var data = Encoding.UTF8.GetString( methodRequest.Data );

			// Check the payload is a single integer value
			if ( int.TryParse( data, out int newDesiredTemperature ) )
			{
				Interlocked.Exchange( ref _desiredValue, newDesiredTemperature );
				Interlocked.Exchange( ref _currentTemperature, newDesiredTemperature );
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine( "Current Temperature set to {0}°", data );
				Console.ResetColor();

				// Acknowlege the direct method call with a 200 success message
				string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
				return Task.FromResult( new MethodResponse( Encoding.UTF8.GetBytes( result ), 200 ) );
			}
			else
			{
				// Acknowlege the direct method call with a 400 error message
				string result = "{\"result\":\"Invalid parameter\"}";
				return Task.FromResult( new MethodResponse( Encoding.UTF8.GetBytes( result ), 400 ) );
			}
		}

		private static async Task SimulateData( CancellationToken ct )
		{
			var serializer = new DataContractJsonSerializer( typeof( TelemetryMessage ) );

			var sensor = DeviceInfo.Sensors.FirstOrDefault( x => ( x.DataType == Configuration[SensorDataTypeSetting] ) );
			if ( sensor == null )
			{
				throw new Exception( $"No preconfigured Sensor for DataType '{Configuration[SensorDataTypeSetting]}' found." );
			}

			while ( true )
			{
				if ( ct.IsCancellationRequested ) break;

				int newTemperature = _currentTemperature + _offsetModifier;

				if ( newTemperature != _lastCurrentTemperatureSent )
				{
					_lastCurrentTemperatureSent = newTemperature;
					var telemetryMessage = new TelemetryMessage()
					{
						SensorId = sensor.Id,
						SensorReading = newTemperature.ToString( CultureInfo.InvariantCulture ),
						EventTimestamp = DateTime.UtcNow.ToString( "o" ),
						SensorType = sensor.Type,
						SensorDataType = sensor.DataType,
						SpaceId = sensor.SpaceId
					};

					try
					{
						List<Task> tasks = new List<Task>();

						using ( var stream = new MemoryStream() )
						{
							serializer.WriteObject( stream, telemetryMessage );
							var binaryMessage = stream.ToArray();
							Message eventMessage = new Message( binaryMessage );
							eventMessage.Properties.Add( "Sensor", "" );
							eventMessage.Properties.Add( "MessageVersion", "1.0" );
							eventMessage.Properties.Add( "x-ms-flighting-udf-execution-manually-enabled", "true" );
							Console.WriteLine(
								$"\t{DateTime.UtcNow.ToLocalTime()}> Sending message: {Encoding.ASCII.GetString( binaryMessage )}" );

							tasks.Add( TopologyDeviceClient.SendEventAsync( eventMessage ) );
						}

						await Task.WhenAll( tasks );
					}
					catch ( Exception ex )
					{
						Console.WriteLine( $"Error occurred in {nameof( SimulateData )}: {ex}" );
					}
				}

				await Task.Delay( int.Parse( Configuration[MessageIntervalInMilliSecondsSetting] ), ct );
			}
		}

		private static bool ValidateSettings()
		{
			return !string.IsNullOrWhiteSpace( Configuration.GetSection( ManagementApiUrlSetting ).Value )
				&& !string.IsNullOrWhiteSpace( Configuration.GetSection( HardwareIdSetting ).Value )
				&& !string.IsNullOrWhiteSpace( Configuration.GetSection( SasTokenSetting ).Value )
				&& !string.IsNullOrWhiteSpace( Configuration.GetSection( MessageIntervalInMilliSecondsSetting ).Value )
				&& !string.IsNullOrWhiteSpace( Configuration.GetSection( SensorDataTypeSetting ).Value );
		}
	}
}
