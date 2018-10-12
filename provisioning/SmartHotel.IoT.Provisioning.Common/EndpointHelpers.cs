using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class EndpointHelpers
	{
		public static async Task<IReadOnlyCollection<Endpoint>> GetEndpointsAsync( HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			var request = HttpMethod.Get.CreateRequest( "endpoints" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var endpoints = JsonConvert.DeserializeObject<IReadOnlyCollection<Endpoint>>( content );
				return endpoints;
			}

			return null;
		}

		public static async Task<bool> DeleteEndpointAsync( this Endpoint endpoint, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Deleting Endpoint: {JsonConvert.SerializeObject( endpoint, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Delete.CreateRequest( $"endpoints/{endpoint.Id}" );
			var response = await httpClient.SendAsync( request );
			return response.IsSuccessStatusCode;
		}
	}
}
