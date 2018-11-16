using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class SpaceHelpers
	{
		public static async Task<Space> GetUniqueSpaceAsync( HttpClient httpClient, string name, Guid parentId, JsonSerializerSettings jsonSerializerSettings )
		{
			var filterName = $"Name eq '{name}'";
			var filterParentSpaceId = parentId != Guid.Empty
				? $"ParentSpaceId eq guid'{parentId}'"
				: "ParentSpaceId eq null";
			var odataFilter = $"$filter={filterName} and {filterParentSpaceId}";

			var request = HttpMethod.Get.CreateRequest( $"spaces?{odataFilter}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var spaces = JsonConvert.DeserializeObject<IReadOnlyCollection<Space>>( content );
				var matchingSpace = spaces.Count == 1 ? spaces.First() : null;
				if ( matchingSpace != null )
				{
					return matchingSpace;
				}
			}

			return null;
		}

		public static async Task<bool> DeleteSpaceAsync( this Space space, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Deleting Space: {JsonConvert.SerializeObject( space, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Delete.CreateRequest( $"spaces/{space.Id}" );
			var response = await httpClient.SendAsync( request );
			return response.IsSuccessStatusCode;
		}

		public static async Task<string> GetSpaceFullPathAsync( HttpClient httpClient, Guid spaceId )
		{
			var space = await GetSpaceAsync( httpClient, spaceId, "fullpath" );
			return space?.SpacePaths.FirstOrDefault();
		}

		public static async Task<Space> GetSpaceAsync( HttpClient httpClient, Guid spaceId, string includes = null )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"{nameof( GetSpaceAsync )} must have a {nameof( spaceId )}" );
			}
			string includesQueryParam = string.IsNullOrWhiteSpace( includes )
				? string.Empty
				: $"?includes={includes}";
			var request = HttpMethod.Get.CreateRequest( $"spaces/{spaceId}{includesQueryParam}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var resource = JsonConvert.DeserializeObject<Space>( content );
				return resource;
			}

			return null;
		}

		public static async Task CreatePropertyAsync( this Property property, Guid spaceId, HttpClient httpClient,
			JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Creating Property for Space {spaceId}: {JsonConvert.SerializeObject( property, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( $"spaces/{spaceId}/properties", JsonConvert.SerializeObject( property, jsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			Console.WriteLine( response.IsSuccessStatusCode ? "succeeded..." : "failed..." );
		}
	}
}
