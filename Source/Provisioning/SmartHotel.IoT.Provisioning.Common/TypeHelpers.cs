using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;
using Type = SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins.Type;

namespace SmartHotel.IoT.Provisioning.Common
{
    public static class TypeHelpers
    {
	    public static async Task CreateTypeAsync(this Type type, HttpClient httpClient,
		    JsonSerializerSettings jsonSerializerSettings)
	    {
		    Console.WriteLine( $"Creating {nameof(Type)}: {JsonConvert.SerializeObject( type, Formatting.Indented, jsonSerializerSettings )}" );
		    var request = HttpMethod.Post.CreateRequest( "types", JsonConvert.SerializeObject( type, jsonSerializerSettings ) );
		    await httpClient.SendAsync( request );
	    }

	    public static async Task<IReadOnlyCollection<Type>> GetExistingTypesAsync( this Space space,
		    HttpClient httpClient )
	    {
		    var request = HttpMethod.Get.CreateRequest( $"types?spaceId={space.Id}" );
		    var response = await httpClient.SendAsync( request );
		    if ( response.IsSuccessStatusCode )
		    {
			    var content = await response.Content.ReadAsStringAsync();
			    return JsonConvert.DeserializeObject<IReadOnlyCollection<Type>>( content );
		    }

		    return new List<Type>().AsReadOnly();
	    }
    }
}
