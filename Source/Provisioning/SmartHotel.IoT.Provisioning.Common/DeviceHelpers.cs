using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Extensions;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class DeviceHelpers
	{
		public static IDictionary<string, List<DeviceDescription>> GetAllDeviceDescriptionsByDeviceIdPrefix(
			this IList<SpaceDescription> spaceDescriptions, string parentDeviceIdPrefix)
		{
			var devices = new Dictionary<string, List<DeviceDescription>>();
			foreach (SpaceDescription spaceDescription in spaceDescriptions)
			{
				if (spaceDescription.devices != null)
				{
					var spaceName = spaceDescription.name.FirstLetterToUpperCase().Replace(" ", string.Empty);
					string deviceIdPrefix = $"{parentDeviceIdPrefix}{spaceName}";

					if (!devices.ContainsKey(deviceIdPrefix))
						devices.Add(deviceIdPrefix, new List<DeviceDescription>());

					devices[deviceIdPrefix].AddRange(spaceDescription.devices);
				}

				if (spaceDescription.spaces != null)
				{
					string additionToParentDeviceIdPrefix = string.Empty;
					PropertyDescription deviceIdPrefixProperty =
						spaceDescription.properties?.FirstOrDefault( p => p.name == PropertyKeyDescription.DeviceIdPrefixName );
					if ( deviceIdPrefixProperty != null )
					{
						additionToParentDeviceIdPrefix = deviceIdPrefixProperty.value;
					}

					var children = GetAllDeviceDescriptionsByDeviceIdPrefix(spaceDescription.spaces,
						$"{parentDeviceIdPrefix}{additionToParentDeviceIdPrefix}");

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
			this ICollection<DeviceDescription> deviceDescriptions, HttpClient httpClient )
		{
			var existingDevices = new List<Device>();

			int itemsPerGroup = 10;
			int numberOfGroups = (int)Math.Ceiling( deviceDescriptions.Count / (double)itemsPerGroup );
			for ( int i = 0; i < numberOfGroups; i++ )
			{
				var nextDeviceDescriptions = deviceDescriptions.Skip( i * itemsPerGroup ).Take( itemsPerGroup ).ToArray();
				if ( nextDeviceDescriptions.Any() )
				{
					var filter = $"hardwareIds={string.Join( ";", nextDeviceDescriptions.Select( d => d.hardwareId ) )}";

					var request = HttpMethod.Get.CreateRequest( $"devices?{filter}" );
					var response = await httpClient.SendAsync( request );
					if ( response.IsSuccessStatusCode )
					{
						var content = await response.Content.ReadAsStringAsync();
						var nextExistingDevices = JsonConvert.DeserializeObject<IReadOnlyCollection<Device>>( content );
						if ( nextExistingDevices.Any() )
						{
							existingDevices.AddRange( nextExistingDevices );
						}
					}
					else
					{
						return null;
					}
				}
			}

			return existingDevices;
		}
	}
}