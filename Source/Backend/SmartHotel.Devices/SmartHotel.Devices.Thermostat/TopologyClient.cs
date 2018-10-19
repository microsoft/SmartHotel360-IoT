// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using SmartHotel.Devices.Thermostat.Models;

namespace SmartHotel.Devices.Thermostat
{
    class TopologyClient
    {
        private HttpClient httpClient = new HttpClient();
        private readonly string ApiPath = "api/v1.0/";
        private readonly string DevicesPath = "Devices";
        private readonly string DevicesIncludeArgument = "includes=Sensors,ConnectionString,Types,SensorsTypes";
        public TopologyClient(string managementBaseUrl, string sasToken)
        {
	        string protectedManagementBaseUrl = managementBaseUrl.EndsWith('/') ? managementBaseUrl : $"{managementBaseUrl}/";
	        this.httpClient.BaseAddress = new Uri(protectedManagementBaseUrl);
            this.httpClient.DefaultRequestHeaders.Add("Authorization", sasToken);
        }

        public async Task<Device> GetDeviceForHardwareId(string hardwareId)
        {
            Device device = null;
            var serializer = new DataContractJsonSerializer(typeof(List<Device>));
            try
            {
                var response = this.httpClient.GetStreamAsync($"{ApiPath}{DevicesPath}?hardwareIds={hardwareId}&{DevicesIncludeArgument}");
                device = (serializer.ReadObject(await response) as List<Device>).FirstOrDefault(x => x.HardwareId == hardwareId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return device;
        }
    }
}