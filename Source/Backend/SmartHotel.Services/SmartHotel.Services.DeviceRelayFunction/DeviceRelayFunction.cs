using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using SmartHotel.Services.DeviceRelayFunction.Models;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SmartHotel.Services.DeviceRelayFunction
{
    public static class DeviceRelayFunction
    {
        [FunctionName("DeviceRelayFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var telemetryMessage = JsonConvert.DeserializeObject<TelemetryMessage>(requestBody);

                log.LogInformation($"DeviceRelayFunction received telemetry: {telemetryMessage}");

                var topologyDeviceClient = DeviceClient.CreateFromConnectionString(telemetryMessage.ConnectionString);

                if (topologyDeviceClient == null)
                {
                    log.LogError($"DeviceRelayFunction failed to create a TopologyDeviceClient.");
                    return new StatusCodeResult(500);
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
                log.LogError($"DeviceRelayFunction failed: {ex}");
                return new StatusCodeResult(500);
            }

            return new OkResult();
        }
    }
}
