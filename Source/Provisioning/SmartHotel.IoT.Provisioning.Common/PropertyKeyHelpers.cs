using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
    public static class PropertyKeyHelpers
    {
	    public static async Task CreatePropertyKeyAsync(this PropertyKey propertyKey, Guid spaceId, HttpClient httpClient,
		    JsonSerializerSettings jsonSerializerSettings)
	    {
		    Console.WriteLine( $"Creating PropertyKey for Space {spaceId}: {JsonConvert.SerializeObject( propertyKey, Formatting.Indented, jsonSerializerSettings )}" );
		    var request = HttpMethod.Post.CreateRequest( "propertykeys", JsonConvert.SerializeObject( propertyKey, jsonSerializerSettings ) );
		    var response = await httpClient.SendAsync( request );
		    Console.WriteLine( response.IsSuccessStatusCode ? "succeeded..." : "failed..." );
	    }

	    public static async Task<IReadOnlyCollection<PropertyKey>> GetExistingPropertyKeysAsync( this Space space,
		    HttpClient httpClient )
	    {
		    var request = HttpMethod.Get.CreateRequest( $"propertykeys?spaceId={space.Id}" );
		    var response = await httpClient.SendAsync( request );
		    if ( response.IsSuccessStatusCode )
		    {
			    var content = await response.Content.ReadAsStringAsync();
			    return JsonConvert.DeserializeObject<IReadOnlyCollection<PropertyKey>>( content );
		    }

		    return new List<PropertyKey>().AsReadOnly();
	    }
    }
}
