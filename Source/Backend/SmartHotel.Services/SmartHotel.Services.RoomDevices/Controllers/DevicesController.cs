using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartHotel.Services.RoomDevices.Models;

namespace SmartHotel.Services.RoomDevices.Controllers
{
	[Route( "api/[controller]" )]
	[ApiController]
	public class DevicesController : ControllerBase
	{
		private readonly string DatabaseName = "DeviceData";
		private readonly string TableName = "SensorData";

		private IConfiguration Configuration { get; }
		private ServiceClient _serviceClient;
		private MongoClient _documentClient;

		public DevicesController( IConfiguration config )
		{
			Configuration = config;
			_serviceClient = ServiceClient.CreateFromConnectionString( Configuration["IoTHubConnectionString"] );
			_documentClient = new MongoClient( Configuration["DatabaseConnectionString"] );
		}

		// GET api/devices/room/roomId
		/// <summary>
		/// Returns device(s) sensor data for a given room.
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns>DeviceSensorData</returns>
		[HttpGet( "room/{roomId}" )]
		public ActionResult<IEnumerable<DeviceSensorData>> Get( string roomId )
		{
			try
			{
				var sensorData = new List<DeviceSensorData>();
				var db = _documentClient.GetDatabase( DatabaseName );
				var table = db.GetCollection<DeviceSensorData>( TableName );

				List<DeviceSensorData> results = table.Find( new BsonDocument( "roomId", roomId ) ).ToList();

				DeviceSensorData[] latestSensorDatas = results
					.GroupBy( sd => sd.SensorDataType )
					.Select(g => g.OrderByDescending( sd => sd.EventTimestamp ).First()).ToArray();
				sensorData.AddRange( latestSensorDatas );

				return sensorData;
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine( e );
				return null;
			}
		}

        // GET api/devices/desired/roomId
        /// <summary>
        /// Returns device(s) desired data for a given room.
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns>DesiredData</returns>
        [HttpGet("desired/{roomId}")]
        public ActionResult<IEnumerable<DesiredData>> GetDesired(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                return NotFound();

            var desiredData = new List<DesiredData>();

            try
            {
                var db = _documentClient.GetDatabase(DatabaseName);
                var desiredDataTable = db.GetCollection<DesiredData>("DesiredData");

                var filter = $"{{roomId: \"{roomId}\"}}";
                Console.WriteLine(filter);

                List<DesiredData> results = desiredDataTable.Find(filter).ToList();
                if (results != null && results.Count > 0)
                {
                    Dictionary<string, DesiredData[]> sensorDatasByRoom = results.GroupBy(sd => sd.RoomId)
                        .ToDictionary(g => g.Key, g => g.ToArray());
                    foreach (KeyValuePair<string, DesiredData[]> kvp in sensorDatasByRoom)
                    {
                        DesiredData[] latestDesiredDatas = kvp.Value
                            .GroupBy(sd => sd.SensorId)
                            .Select(g => g.OrderByDescending(sd => sd.EventTimestamp).First()).ToArray();

                        desiredData.AddRange(latestDesiredDatas);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            return Ok(desiredData);
        }

        // POST api/devices
        [HttpPost]
		public ActionResult Post( [FromBody] DeviceRequest request )
		{
            if (request == null)
                return BadRequest();

            try
            {
                // Store desiredValue in CosmosDB
                var db = _documentClient.GetDatabase(DatabaseName);
                var desiredDataTable = db.GetCollection<DesiredData>("DesiredData");

                var filter = $"{{roomId: \"{request.RoomId}\", sensorId: \"{request.SensorId}\"}}";
                Console.WriteLine(filter);

                DesiredData result = desiredDataTable.Find(filter).ToList().FirstOrDefault();

                if (result == null)
                {
                    result = new DesiredData();
                    result.RoomId = request.RoomId;
                    result.SensorId = request.SensorId;
                }

                result.DesiredValue = request.Value;
                result.EventTimestamp = DateTime.Now;

                var update = $"{{$set: {{ desiredValue: \"{request.Value}\"}}}}";
                var options = new UpdateOptions();
                options.IsUpsert = true;

                desiredDataTable.UpdateOne(filter, update, options);

                // Send to IoTHub
                var invocation = new CloudToDeviceMethod(request.MethodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
                invocation.SetPayloadJson(request.Value);

                var response = _serviceClient.InvokeDeviceMethodAsync(request.DeviceId, invocation).GetAwaiter().GetResult();

                Console.WriteLine("DeviceRequest Response status: {0}, payload:", response.Status);
                Console.WriteLine(response.GetPayloadAsJson());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return StatusCode(500, e.Message);
            }

            return Ok();
        }
	}
}
