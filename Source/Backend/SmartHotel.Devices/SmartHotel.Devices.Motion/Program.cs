using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartHotel.Devices.Motion.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHotel.Devices.Motion
{
	class SmartMotion
	{
		private static readonly string ManagementApiUrlSetting = "ManagementApiUrl";
		private static readonly string SasTokenSetting = "SasToken";
		private static readonly string HardwareIdSetting = "HardwareId";
		private static readonly string MessageIntervalInMilliSecondsSetting = "MessageIntervalInMilliSeconds";
		private static readonly string RandomizationDelaySetting = "RandomizationDelay";
		private static readonly string SensorDataTypeSetting = "SensorDataType";

		private static IConfiguration Configuration { get; set; }
		private static Device DeviceInfo { get; set; }
		private static DeviceClient TopologyDeviceClient { get; set; }

		private static bool? _lastMotionDetectedSent;
		private static bool _motionDetected = false;
		private static Timer _motionTimer;
		private static Random _random = new Random();
		private static int _randomizationDelay = 60000;

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
				if ( _motionTimer != null )
					_motionTimer.Dispose();

				e.Cancel = true;
				cts.Cancel();
				Console.WriteLine( "Exiting..." );
			};

			Console.WriteLine( "SmartMotion - Simulate a Digital Twins Motion Sensor. Ctrl-C to exit.\n" );

			if ( !ValidateSettings() )
			{
				Console.WriteLine( "SmartMotion - Your settings are invalid.  Please check your setting values and try again." );
				cts.Token.WaitHandle.WaitOne();
				return;
			}

			if ( !int.TryParse( Configuration[RandomizationDelaySetting], out _randomizationDelay ) )
			{
				_randomizationDelay = 60000;
			}

			var hardwareId = Configuration[HardwareIdSetting];
			Console.WriteLine( $"Your hardware ID is: {hardwareId}" );

			TimeSpan startupDelay = TimeSpan.FromSeconds(new Random().Next(1, 10));
			Console.WriteLine($"Waiting {startupDelay.TotalSeconds} seconds to startup...");
			await Task.Delay(startupDelay, cts.Token);

			_motionTimer = new Timer( RandomizeMotionValue, null, 5000, _randomizationDelay );

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
		}

		private static void RandomizeMotionValue( object state )
		{
			_motionDetected = Convert.ToBoolean( _random.Next( 0, 2 ) );
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

				if ( _lastMotionDetectedSent != _motionDetected )
				{
					_lastMotionDetectedSent = _motionDetected;
					var telemetryMessage = new TelemetryMessage()
					{
						SensorId = sensor.Id,
						SensorReading = _motionDetected.ToString(),
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
