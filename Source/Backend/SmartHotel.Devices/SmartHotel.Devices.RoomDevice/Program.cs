using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using SmartHotel.Devices.RoomDevice.Models;

namespace SmartHotel.Devices.RoomDevice
{
	class Program
	{
		private static readonly string ManagementApiUrlSetting = "ManagementApiUrl";
		private static readonly string SasTokenSetting = "SasToken";
		private static readonly string IoTHubDeviceConnectionStringSetting = "IoTHubDeviceConnectionString";
		private static readonly string HardwareIdSetting = "HardwareId";
		private static readonly string MessageIntervalInMilliSecondsSetting = "MessageIntervalInMilliSeconds";
		private static readonly string StartupDelayInSecondsSetting = "StartupDelayInSeconds";

		private static readonly ConcurrentDictionary<string, SensorInfo> SensorInfosByDataType =
			new ConcurrentDictionary<string, SensorInfo>( StringComparer.OrdinalIgnoreCase );

		private static IConfiguration Configuration { get; set; }
		private static Device DeviceInfo { get; set; }
		private static DeviceClient TopologyDeviceClient { get; set; }
		private static DeviceClient HubDeviceClient { get; set; }

		private const string TemperatureDataType = "Temperature";
		private const string LightDataType = "Light";
		private const string MotionDataType = "Motion";

		private static Timer _motionTimer;
		private static readonly Random _random = new Random();
		private static int _randomizationDelay = 60000;

		static async Task Main( string[] args )
		{
			SensorInfosByDataType[TemperatureDataType] = new SensorInfo<int>( 74, int.MinValue );
			SensorInfosByDataType[LightDataType] = new SensorInfo<double>( 1.0, double.MinValue );
			SensorInfosByDataType[MotionDataType] = new SensorInfo<bool?>( false, null );

			var builder = new ConfigurationBuilder()
				.SetBasePath( Directory.GetCurrentDirectory() )
				.AddJsonFile( "appsettings.json", true )
				.AddEnvironmentVariables();

			Configuration = builder.Build();

			CancellationTokenSource cts = new CancellationTokenSource();

			try
			{
				Console.CancelKeyPress += ( s, e ) =>
				{
					_motionTimer?.Dispose();

					e.Cancel = true;
					cts.Cancel();
					Console.WriteLine( "Exiting..." );
				};

				Console.WriteLine( "RoomDevice - Simulate a Digital Twins device with Temperature, Light, and Motion sensors. Ctrl-C to exit.\n" );

				if ( !ValidateSettings() )
				{
					Console.WriteLine( "RoomDevice - Your settings are invalid.  Please check your setting values and try again." );
					Environment.Exit( 1 );
				}

				var hardwareId = Configuration[HardwareIdSetting];
				Console.WriteLine( $"Your hardware ID is: {hardwareId}" );

				TimeSpan startupDelay = TimeSpan.FromSeconds( double.Parse( Configuration[StartupDelayInSecondsSetting] ) );
				Console.WriteLine( $"Waiting {startupDelay.TotalSeconds} seconds to startup..." );
				await Task.Delay( startupDelay, cts.Token );

				_motionTimer = new Timer( RandomizeMotionValue, null, 5000, _randomizationDelay );

				var topologyClient = new TopologyClient( Configuration[ManagementApiUrlSetting], Configuration[SasTokenSetting] );
				DeviceInfo = topologyClient.GetDeviceForHardwareId( hardwareId ).Result;

				if ( DeviceInfo == null )
				{
					Console.WriteLine( "ERROR: Could not retrieve device information." );
					Environment.Exit( 2 );
				}

				HubDeviceClient =
					DeviceClient.CreateFromConnectionString( Configuration[IoTHubDeviceConnectionStringSetting], TransportType.Mqtt );
				await HubDeviceClient.SetMethodHandlerAsync( "SetDesiredTemperature", SetDesiredTemperature, null );
				await HubDeviceClient.SetMethodHandlerAsync( "SetDesiredAmbientLight", SetAmbientLight, null );

				Console.WriteLine( "Connection to Digital Twins: " + DeviceInfo.ConnectionString );
				TopologyDeviceClient = DeviceClient.CreateFromConnectionString( DeviceInfo.ConnectionString );

				if ( TopologyDeviceClient == null )
				{
					Console.WriteLine( "Failed to create Digital Twins DeviceClient!" );
					Environment.Exit( 3 );
				}
				else
				{
					await Task.WhenAll( SimulateData( cts.Token ) );
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Error in RoomDevice: {0}", ex.Message );
				Environment.Exit( 99 );
			}
		}

		private static async Task SimulateData( CancellationToken ct )
		{
			bool atLeastOneSensorMatchFound = false;
			var serializer = new DataContractJsonSerializer( typeof( TelemetryMessage ) );
			foreach ( Sensor sensor in DeviceInfo.Sensors )
			{
				if ( SensorInfosByDataType.ContainsKey( sensor.DataType ) )
				{
					atLeastOneSensorMatchFound = true;
					break;
				}
			}

			if ( !atLeastOneSensorMatchFound )
			{
				throw new Exception(
					$"No preconfigured Sensor found for any of the following datatypes: {string.Join( ", ", SensorInfosByDataType.Keys )}" );
			}

			while ( true )
			{
				if ( ct.IsCancellationRequested ) break;

				foreach ( Sensor sensor in DeviceInfo.Sensors )
				{
					if ( !SensorInfosByDataType.TryGetValue( sensor.DataType, out SensorInfo sensorInfo ) )
					{
						continue;
					}

					if ( sensorInfo.IsCurrentValueDifferent() )
					{
						sensorInfo.UpdateLastValueSent( sensorInfo.CurrentValue );
						var telemetryMessage = new TelemetryMessage()
						{
							SensorId = sensor.Id,
							SensorReading = sensorInfo.CurrentValue.ToString(),
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
				}

				await Task.Delay( int.Parse( Configuration[MessageIntervalInMilliSecondsSetting] ), ct );
			}
		}

		private static Task<MethodResponse> SetDesiredTemperature( MethodRequest methodRequest, object userContext )
		{
			var data = Encoding.UTF8.GetString( methodRequest.Data );

			// Check the payload is a single integer value
			if ( int.TryParse( data, out int newDesiredTemperature ) )
			{
				SensorInfo sensorInfo = SensorInfosByDataType[TemperatureDataType];
				sensorInfo.UpdateCurrentValue( newDesiredTemperature );
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

		private static Task<MethodResponse> SetAmbientLight( MethodRequest methodRequest, object userContext )
		{
			var data = Encoding.UTF8.GetString( methodRequest.Data );

			// Check the payload is a double value
			if ( double.TryParse( data, out double newAmbientLight ) )
			{
				SensorInfo sensorInfo = SensorInfosByDataType[LightDataType];
				sensorInfo.UpdateCurrentValue( newAmbientLight );
				Console.WriteLine( "Current Ambient Lighting set to {0} ({0:P})", newAmbientLight );
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

		private static void RandomizeMotionValue( object state )
		{
			bool motionDetected = Convert.ToBoolean( _random.Next( 0, 2 ) );
			SensorInfo sensorInfo = SensorInfosByDataType[MotionDataType];
			sensorInfo.UpdateCurrentValue( motionDetected );
		}

		private static bool ValidateSettings()
		{
			return !string.IsNullOrWhiteSpace( Configuration.GetSection( ManagementApiUrlSetting ).Value )
				   && !string.IsNullOrWhiteSpace( Configuration.GetSection( HardwareIdSetting ).Value )
				   && !string.IsNullOrWhiteSpace( Configuration.GetSection( SasTokenSetting ).Value )
				   && !string.IsNullOrWhiteSpace( Configuration.GetSection( MessageIntervalInMilliSecondsSetting ).Value )
				   && !string.IsNullOrWhiteSpace( Configuration.GetSection( IoTHubDeviceConnectionStringSetting ).Value )
				   && !string.IsNullOrWhiteSpace( Configuration.GetSection( StartupDelayInSecondsSetting ).Value );
		}
	}
}
