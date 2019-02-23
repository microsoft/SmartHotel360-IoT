using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartHotel.Services.FacilityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHotel.Services.FacilityManagement.Controllers
{
    [Route("api/desireddata")]
    [ApiController]
    public class DesiredDataController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ServiceClient _serviceClient;
        private readonly MongoClient _documentClient;

        public DesiredDataController(IConfiguration config)
        {
            _config = config;
            _documentClient = new MongoClient(config["MongoDBConnectionString"]);
            _serviceClient = ServiceClient.CreateFromConnectionString(config["IoTHubConnectionString"]);
        }

        [HttpGet]
        public IActionResult Get([FromQuery(Name = "roomIds")]string[] roomIds)
        {
            if (roomIds == null || roomIds.Length == 0)
                return NotFound();

            var desiredData = new List<DesiredData>();

            try
            {
                var db = _documentClient.GetDatabase(_config["MongoDBName"]);
                var desiredDataTable = db.GetCollection<DesiredData>("DesiredData");

                var filter = $"{{roomId: {{'$in': [{ToMongoArrayString(roomIds)}]}}}}";
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

        [HttpPost]
        public IActionResult Post([FromBody] DesiredDataRequest request)
        {
            if (request == null)
                return BadRequest();

            try
            {
                // Store desiredValue in CosmosDB
                var db = _documentClient.GetDatabase(_config["MongoDBName"]);
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

                result.DesiredValue = request.DesiredValue;
                result.EventTimestamp = DateTime.Now;

                var update = $"{{$set: {{ desiredValue: \"{request.DesiredValue}\"}}}}";
                var options = new UpdateOptions();
                options.IsUpsert = true;

                desiredDataTable.UpdateOne(filter, update, options);

                // Send to IoTHub
                var invocation = new CloudToDeviceMethod(request.MethodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
                invocation.SetPayloadJson(request.DesiredValue);

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

        private string ToMongoArrayString(string[] arr)
        {
            string result = string.Empty;

            for (int i = 0; i < arr.Length; i++)
            {
                result += $"\"{arr[i]}\"";

                if (i < arr.Length - 1)
                    result += ",";
            }

            return result;
        }
    }
}
