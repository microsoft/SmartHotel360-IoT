using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartHotel.Services.SensorDataFunction.Models;

namespace SmartHotel.Services.SensorDataFunction
{
    public static class DeviceRelayFunction
    {
        [FunctionName("DeviceRelayFunction")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            try
            {
                string requestBody = await req.Content.ReadAsStringAsync();

                var telemetryMessage = JsonConvert.DeserializeObject<TelemetryMessage>(requestBody);

                log.LogInformation($"DeviceRelayFunction received telemetry: {telemetryMessage}");

                var topologyDeviceClient = DeviceClient.CreateFromConnectionString(telemetryMessage.ConnectionString);

                if (topologyDeviceClient == null)
                {
                    string error = $"DeviceRelayFunction failed to create a TopologyDeviceClient.";
                    log.LogError(error);
                    return req.CreateResponse(HttpStatusCode.InternalServerError, error);
                }

                telemetryMessage.ConnectionString = null;

                var serializer = new DataContractJsonSerializer(typeof(TelemetryMessage));

                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, telemetryMessage);
                    var binaryMessage = stream.ToArray();
                    Message eventMessage = new Message(binaryMessage);
                    eventMessage.Properties.Add("Sensor", "");
                    eventMessage.Properties.Add("MessageVersion", "1.0");
                    eventMessage.Properties.Add("x-ms-flighting-udf-execution-manually-enabled", "true");
                    Console.WriteLine(
                        $"\t{DateTime.UtcNow.ToLocalTime()}> Sending message: {Encoding.ASCII.GetString(binaryMessage)}");

                    await topologyDeviceClient.SendEventAsync(eventMessage);
                }
            }
            catch (Exception ex)
            {
                string error = $"DeviceRelayFunction failed: {ex}";
                log.LogError(error);
                return req.CreateResponse(HttpStatusCode.InternalServerError, error);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
