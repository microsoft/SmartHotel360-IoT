using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public class KeyStoresHelper
	{
		public static async Task<Keystore> GetUniqueKeystoreAsync( HttpClient httpClient, string name, Guid spaceId, JsonSerializerSettings jsonSerializerSettings )
		{
			var filterName = $"Name eq '{name}'";
			var filterSpaceId = $"SpaceId eq guid'{spaceId}'";
			var odataFilter = $"$filter={filterName} and {filterSpaceId}";

			var request = HttpMethod.Get.CreateRequest( $"keystores?{odataFilter}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var keystores = JsonConvert.DeserializeObject<IReadOnlyCollection<Keystore>>( content );
				var matchingKeystore = keystores.Count == 1 ? keystores.First() : null;
				if ( matchingKeystore != null )
				{
					return matchingKeystore;
				}
			}

			return null;
		}
	}
}
