// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Microsoft Azure Event Hubs Client for .NET
// For samples see: https://github.com/Azure/azure-event-hubs/tree/master/samples/DotNet
// For documenation see: https://docs.microsoft.com/azure/event-hubs/
using System;
using Microsoft.Azure.EventHubs;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Azure.Devices;

namespace IoTHubMonitor
{
    class IotHubMonitor
    {
        private static readonly string EventHubEndpointSetting = "EventHubEndpoint";
        private static readonly string EventHubCompatibleNameSetting = "EventHubCompatibleName";
        private static readonly string SasKeyNameSetting = "SasKeyName";
        private static readonly string SasKeySetting = "SasKey";
        private static readonly string SimulateEventsSetting = "SimulateEvents";
        private static readonly string ServerConnectionStringSetting = "ServerConnectionString";
        private static readonly string ThermostatDeviceIdSetting = "ThermostatDeviceId";
        private static readonly string LightDeviceIdSetting = "LightDeviceId";

        private static IConfiguration Configuration { get; set; }
        private static EventHubClient _eventHubClient;

        private static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Console.WriteLine("IoT Hub Monitor - Read device to cloud messages. Ctrl-C to exit.\n");

            // Create an EventHubClient instance to connect to the
            // IoT Hub Event Hubs-compatible endpoint.
            var hubConnectionString = new EventHubsConnectionStringBuilder(new Uri(Configuration[EventHubEndpointSetting]), Configuration[EventHubCompatibleNameSetting], Configuration[SasKeyNameSetting], Configuration[SasKeySetting]);
            _eventHubClient = EventHubClient.CreateFromConnectionString(hubConnectionString.ToString());

            // Create a PartitionReciever for each partition on the hub.
            var hubRuntimeInfo = await _eventHubClient.GetRuntimeInformationAsync();
            List<Tuple<EventHubClient, string>> d2CPartitions =
                hubRuntimeInfo.PartitionIds
                    .Select(id => new Tuple<EventHubClient, string>(_eventHubClient, id))
                    .ToList();

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (Tuple<EventHubClient, string> eventHubClientPartitionPair in d2CPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(eventHubClientPartitionPair.Item1, eventHubClientPartitionPair.Item2, cts.Token));
            }

            if (Configuration[SimulateEventsSetting] == "true")
            {
                tasks.Add(SimulateEvents(cts.Token));
            }

            // Wait for all the PartitionReceivers to finish.
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task SimulateEvents(CancellationToken token)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(Configuration[ServerConnectionStringSetting]);

            var rnd = new Random();

            while (true)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    // Desired Temperature
                    var desiredTemperatureInvocation = new CloudToDeviceMethod("SetDesiredTemperature") { ResponseTimeout = TimeSpan.FromSeconds(30) };
                    desiredTemperatureInvocation.SetPayloadJson(rnd.Next(50, 90).ToString());

                    var responseDesiredTemperature = await serviceClient.InvokeDeviceMethodAsync(Configuration[ThermostatDeviceIdSetting], desiredTemperatureInvocation);

                    Console.WriteLine("Desired Temperature Response status: {0}, payload:", responseDesiredTemperature.Status);
                    Console.WriteLine(responseDesiredTemperature.GetPayloadAsJson());
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }

                try
                { 
                    // Ambient Light
                    var ambientLightInvocation = new CloudToDeviceMethod("SetDesiredAmbientLight") { ResponseTimeout = TimeSpan.FromSeconds(30) };
                    ambientLightInvocation.SetPayloadJson(rnd.NextDouble().ToString());

                    var responseAmbientLight = await serviceClient.InvokeDeviceMethodAsync(Configuration[LightDeviceIdSetting], ambientLightInvocation);

                    Console.WriteLine("Ambient Light Response status: {0}, payload:", responseAmbientLight.Status);
                    Console.WriteLine(responseAmbientLight.GetPayloadAsJson());
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }

                await Task.Delay(10000);
            }
        }

        // Asynchronously create a PartitionReceiver for a partition and then start 
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(EventHubClient eventHubClient, string partition, CancellationToken ct)
        {
            // Create the receiver using the default consumer group.
            // For the purposes of this sample, read only messages sent since 
            // the time the receiver is created. Typically, you don't want to skip any messages.
            var eventHubReceiver = eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
            Console.WriteLine($"Create receiver on hub {eventHubClient.EventHubName}, partition: {partition}");
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine($"Listening for messages on: {eventHubClient.EventHubName}-{partition}");
                // Check for EventData - this methods times out if there is nothing to retrieve.
                var events = await eventHubReceiver.ReceiveAsync(100);

                // If there is data in the batch, process it.
                if (events == null) continue;

                foreach (EventData eventData in events)
                {
                    string data = Encoding.UTF8.GetString(eventData.Body.Array);
                    var sb = new StringBuilder();
                    sb.AppendLine($"Message received on hub {eventHubClient.EventHubName}, partition: {partition}");
                    sb.AppendLine($"  {data}:");
                    sb.AppendLine("Application properties (set by device):");
                    foreach (var prop in eventData.Properties)
                    {
                        sb.AppendLine($"  {prop.Key}: {prop.Value}");
                    }
                    sb.AppendLine("System properties (set by IoT Hub):");
                    foreach (var prop in eventData.SystemProperties)
                    {
                        sb.AppendLine($"  {prop.Key}: {prop.Value}");
                    }
                    Console.WriteLine(sb.ToString());
                }
            }
        }
    }
}
