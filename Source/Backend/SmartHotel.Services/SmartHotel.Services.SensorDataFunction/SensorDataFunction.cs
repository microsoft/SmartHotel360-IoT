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
		[FunctionName( "SensorDataFunction" )]
        public static void Run([EventHubTrigger("smarthotel-iot-eventhub", Connection = "EventHubConnectionString")]string myEventHubMessage, TraceWriter log)
		{
			dynamic telemetry = JsonConvert.DeserializeObject( myEventHubMessage );

			log.Info( $"EventHub Trigger received telemetry: {telemetry}" );

			string sensorId = telemetry.SensorId;

			MongoClient client = new MongoClient( System.Environment.GetEnvironmentVariable( "CosmosDBConnectionString" ) );

			var db = client.GetDatabase( "DeviceData" );
			var coll = db.GetCollection<DeviceSensorData>( "SensorData" );
			
			var document = coll.Find( new BsonDocument( "sensorId", sensorId ) ).FirstOrDefault();

			if ( document != null )
			{
				document.SensorReading = telemetry.SensorReading;
				document.EventTimestamp = telemetry.EventTimestamp;
				coll.ReplaceOne( new BsonDocument( "sensorId", sensorId ), document );
			}
			else
			{
				DeviceSensorData sensorData = new DeviceSensorData();
				sensorData.SensorId = telemetry.SensorId;
				sensorData.RoomId = telemetry.SpaceId;
				sensorData.SensorReading = telemetry.SensorReading;
				sensorData.EventTimestamp = telemetry.EventTimestamp;
				sensorData.SensorDataType = telemetry.SensorDataType;

				coll.InsertOne( sensorData );
			}
		}
	}
}
