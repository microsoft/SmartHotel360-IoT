using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class MatcherHelpers
	{
		public static async Task CreateMatcherAsync( this Matcher matcher, HttpClient httpClient,
			JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Creating {nameof( Matcher )}: {JsonConvert.SerializeObject( matcher, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( "matchers", JsonConvert.SerializeObject( matcher, jsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			Console.WriteLine( response.IsSuccessStatusCode ? "succeeded..." : "failed..." );
		}

		public static async Task<IReadOnlyCollection<Matcher>> GetExistingMatchersAsync( this Space space,
			HttpClient httpClient )
		{
			var request = HttpMethod.Get.CreateRequest( $"matchers?spaceId={space.Id}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<IReadOnlyCollection<Matcher>>( content );
			}

			return new List<Matcher>().AsReadOnly();
		}

		/// <summary>
		/// Returns a matcher with same name and spaceId if there is exactly one.
		/// Otherwise returns null.
		/// </summary>
		public static async Task<ICollection<Matcher>> FindMatchersAsync( HttpClient httpClient, ICollection<string> names, Guid spaceId )
		{
			string commaDelimitedNames = string.Join( ",", names );
			string filterNames = $"names={commaDelimitedNames}";
			string filterSpaceId = $"&spaceIds={spaceId}";
			string filter = $"{filterNames}{filterSpaceId}";

			var response = await httpClient.GetAsync( $"matchers?{filter}" );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var matchers = JsonConvert.DeserializeObject<ICollection<Matcher>>( content );
				if ( matchers != null )
				{
					Console.WriteLine( $"Retrieved Unique Matchers using 'name' and 'spaceId': {JsonConvert.SerializeObject( matchers, Formatting.Indented )}" );
					return matchers;
				}
			}
			return null;
		}
	}
}
