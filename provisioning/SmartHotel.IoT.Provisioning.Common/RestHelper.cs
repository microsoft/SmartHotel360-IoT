using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class RestHelper
	{
		public static HttpRequestMessage CreateRequest( this HttpMethod method, string query, string content = null )
		{
			return new HttpRequestMessage( method, query )
			{
				Content = content != null ? new StringContent( content, Encoding.UTF8, "application/json" ) : null
			};
		}

		public static async Task<Guid> GetIdAsync( this HttpResponseMessage response )
		{
			if ( !response.IsSuccessStatusCode )
				return Guid.Empty;
			string content = await response.Content.ReadAsStringAsync();

			// strip out the double quotes
			var contentSanitized = content.Trim( '"' );

			if ( !Guid.TryParse( contentSanitized, out var createdId ) )
			{
				Console.WriteLine( $"ERROR: Returned value from POST did not parse into a guid: {content}" );
				return Guid.Empty;
			}

			return createdId;
		}
	}
}
