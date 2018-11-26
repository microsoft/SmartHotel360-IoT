using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using SmartHotel.Services.SensorDataFunction.Models;

namespace SmartHotel.Services.SensorDataFunction
{
	public static class SensorDataFunctionApp
	{
		private static readonly Lazy<IMongoCollection<DeviceSensorData>> LazyCollection = new Lazy<IMongoCollection<DeviceSensorData>>(InitializeMongoCollection);
		private static readonly IMongoCollection<DeviceSensorData> Collection = LazyCollection.Value;
		[FunctionName( "SensorDataFunction" )]
		public static void Run( [EventHubTrigger( "smarthotel-iot-eventhub", Connection = "EventHubConnectionString" )]string myEventHubMessage, TraceWriter log )
		{
			log.Info(string.Empty);
			log.Info(string.Empty);
			log.Info( $"EventHub Trigger received telemetry: {myEventHubMessage}" );
			log.Info(string.Empty);
			log.Info(string.Empty);
			var telemetry = JsonConvert.DeserializeObject<TelemetryMessage>( myEventHubMessage );

			string sensorId = telemetry.SensorId;
			
			var document = Collection.Find( new BsonDocument( "sensorId", sensorId ) ).FirstOrDefault();

			if ( document != null )
			{
				document.SensorReading = telemetry.SensorReading;
				document.EventTimestamp = telemetry.EventTimestamp;
				Collection.ReplaceOne( new BsonDocument( "sensorId", sensorId ), document );
			}
			else
			{
				DeviceSensorData sensorData = new DeviceSensorData();
				sensorData.SensorId = telemetry.SensorId;
				sensorData.RoomId = telemetry.SpaceId;
				sensorData.SensorReading = telemetry.SensorReading;
				sensorData.EventTimestamp = telemetry.EventTimestamp;
				sensorData.SensorDataType = telemetry.SensorDataType;
				sensorData.IoTHubDeviceId = telemetry.IoTHubDeviceId;

				Collection.InsertOne( sensorData );
			}
		}

		private static IMongoCollection<DeviceSensorData> InitializeMongoCollection()
		{
			var client = new MongoClient( System.Environment.GetEnvironmentVariable( "CosmosDBConnectionString" ) );
			var db = client.GetDatabase( "DeviceData" );
			return db.GetCollection<DeviceSensorData>( "SensorData" );
		}
	}
}
