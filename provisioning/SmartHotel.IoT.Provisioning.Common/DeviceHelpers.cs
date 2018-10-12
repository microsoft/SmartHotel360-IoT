using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class DeviceHelpers
	{
		public static IDictionary<string, List<DeviceDescription>> GetAllDeviceDescriptions( this IList<SpaceDescription> spaceDescriptions )
		{
			var devices = new Dictionary<string, List<DeviceDescription>>();
			foreach ( SpaceDescription spaceDescription in spaceDescriptions )
			{
				if ( spaceDescription.devices != null )
				{
                    var spaceName = spaceDescription.name.Replace(" ", string.Empty).ToLower();

                    if (!devices.ContainsKey(spaceName))
                        devices.Add(spaceName, new List<DeviceDescription>());

					devices[spaceName].AddRange( spaceDescription.devices );
				}

				if ( spaceDescription.spaces != null )
				{
                    var children = GetAllDeviceDescriptions(spaceDescription.spaces);

                    foreach (var pair in children)
                    {
                        if (!devices.ContainsKey(pair.Key))
                            devices.Add(pair.Key, new List<DeviceDescription>());

                        devices[pair.Key].AddRange(pair.Value);
                    }
                }
			}

			return devices;
		}

		public static async Task<Guid> CreateDeviceAsync( this Device device, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Creating Device: {JsonConvert.SerializeObject( device, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( "devices", JsonConvert.SerializeObject( device, jsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			return await response.GetIdAsync();
		}

		public static async Task<bool> DeleteDeviceAsync( this Device device, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Deleting Device: {JsonConvert.SerializeObject( device, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Delete.CreateRequest( $"devices/{device.Id}" );
			var response = await httpClient.SendAsync( request );
			return response.IsSuccessStatusCode;
		}

		public static async Task<Device> GetSingleExistingDeviceAsync( this DeviceDescription deviceDescription, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			IReadOnlyCollection<Device> devices = await new[] { deviceDescription }.GetExistingDevicesAsync( httpClient );
			Device matchingDevice = devices?.SingleOrDefault();
			return matchingDevice;
		}

		public static async Task<IReadOnlyCollection<Device>> GetExistingDevicesAsync(
			this IEnumerable<DeviceDescription> deviceDescriptions, HttpClient httpClient )
		{
			var filter = $"hardwareIds={string.Join( ";", deviceDescriptions.Select( d => d.hardwareId ) )}";

			var request = HttpMethod.Get.CreateRequest( $"devices?{filter}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var existingDevices = JsonConvert.DeserializeObject<IReadOnlyCollection<Device>>( content );
				return existingDevices;
			}

			return null;
		}
	}
}