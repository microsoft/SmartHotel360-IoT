using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class UserDefinedFunctionHelpers
	{
		public static async Task CreateOrPatchUserDefinedFunctionAsync( this UserDefinedFunctionDescription description,
			HttpClient httpClient, string udfText, Guid spaceId, ICollection<Matcher> matchers )
		{
			UserDefinedFunction userDefinedFunction =
				await FindUserDefinedFunctionAsync( httpClient, description.name, spaceId );

			if ( userDefinedFunction != null )
			{
				await Api.UpdateUserDefinedFunction(
					httpClient,
					logger,
					description.ToUserDefinedFunctionUpdate( userDefinedFunction.Id, spaceId, matchers.Select( m => m.Id ) ),
					js );
			}
			else
			{
				await Api.CreateUserDefinedFunction(
					httpClient,
					logger,
					description.ToUserDefinedFunctionCreate( spaceId, matchers.Select( m => m.Id ) ),
					js );
			}
		}

		/// <summary>
		/// Returns a user defined fucntion with same name and spaceId if there is exactly one.
		/// Otherwise returns null.
		/// </summary>
		private static async Task<UserDefinedFunction> FindUserDefinedFunctionAsync( HttpClient httpClient, string name,
			Guid spaceId )
		{
			var filterNames = $"names={name}";
			var filterSpaceId = $"&spaceIds={spaceId.ToString()}";
			var filter = $"{filterNames}{filterSpaceId}";

			var response = await httpClient.GetAsync( $"userdefinedfunctions?{filter}&includes=matchers" );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var userDefinedFunctions = JsonConvert.DeserializeObject<IReadOnlyCollection<UserDefinedFunction>>( content );
				var userDefinedFunction = userDefinedFunctions.SingleOrDefault();
				if ( userDefinedFunction != null )
				{
					Console.WriteLine(
						$"Retrieved Unique UserDefinedFunction using 'name' and 'spaceId': {JsonConvert.SerializeObject( userDefinedFunction, Formatting.Indented )}" );
					return userDefinedFunction;
				}
			}

			return null;
		}

		private static async Task UpdateUserDefinedFunction( HttpClient httpClient, UserDefinedFunction userDefinedFunction, string udfText )
		{
			Console.WriteLine();
			Console.WriteLine( $"Updating UserDefinedFunction with Metadata: {JsonConvert.SerializeObject( userDefinedFunction, Formatting.Indented )}" );
			var displayContent = udfText.Length > 100 ? udfText.Substring( 0, 100 ) + "..." : udfText;
			Console.WriteLine( $"Updating UserDefinedFunction with Content: {displayContent}" );
			Console.WriteLine();

			var metadataContent = new StringContent( JsonConvert.SerializeObject( userDefinedFunction ), Encoding.UTF8, "application/json" );
			metadataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "application/json; charset=utf-8" );

			var multipartContent = new MultipartFormDataContent( "userDefinedFunctionBoundary" );
			multipartContent.Add( metadataContent, "metadata" );
			multipartContent.Add( new StringContent( udfText ), "contents" );

			await httpClient.PatchAsync( $"userdefinedfunctions/{userDefinedFunction.Id}", multipartContent );
		}
	}
}
