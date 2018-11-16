using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class ResourcesHelpers
	{
		public static async Task<IReadOnlyCollection<Resource>> GetExistingResourcesUnderSpacesAsync( this IEnumerable<Space> spaces,
			HttpClient httpClient )
		{
			var resources = new List<Resource>();
			foreach ( Space space in spaces )
			{
				resources.AddRange( await space.GetExistingChildResourcesAsync( httpClient ) );
			}

			return resources.AsReadOnly();
		}

		public static async Task<IReadOnlyCollection<Resource>> GetExistingChildResourcesAsync( this Space space,
			HttpClient httpClient )
		{
			var request = HttpMethod.Get.CreateRequest( $"resources?spaceId={space.Id}&traverse=Down" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<IReadOnlyCollection<Resource>>( content );
			}

			return new List<Resource>().AsReadOnly();
		}

		public static async Task<bool> DeleteResourceAsync( this Resource resource, HttpClient httpClient, JsonSerializerSettings jsonSerializerSettings )
		{
			Console.WriteLine( $"Deleting Resource: {JsonConvert.SerializeObject( resource, Formatting.Indented, jsonSerializerSettings )}" );
			var request = HttpMethod.Delete.CreateRequest( $"resources/{resource.Id}" );
			var response = await httpClient.SendAsync( request );
			return response.IsSuccessStatusCode;
		}

		public static Task<bool> WaitTillResourceCreationCompletedAsync( HttpClient httpClient, Guid resourceId )
		{
			return Task.Run( async () =>
			{
				while ( true )
				{
					var resource = await GetResourceAsync( httpClient, resourceId );
					if ( resource == null )
					{
						Console.WriteLine( $"Failed to find expected resource, {resourceId}" );
						return false;
					}

					if ( resource.Status.ToLower() == "provisioning" )
					{
						await Task.Delay( 5000 );
					}
					else
					{
						if ( resource.Status.ToLower() == "running" )
						{
							return true;
						}

						Console.WriteLine( $"Resource ({resourceId}) provisioning not successful. Status: {resource.Status}" );
						return false;
					}
				}
			} );
		}

		public static Task WaitTillResourceDeletionCompletedAsync( HttpClient httpClient, Guid resourceId, CancellationToken cancellationToken )
		{
			return Task.Run( async () =>
			{
				while ( true )
				{
					var resource = await GetResourceAsync( httpClient, resourceId );
					if ( resource == null )
					{
						return;
					}

					await Task.Delay( 5000, cancellationToken );
				}
			}, cancellationToken );
		}

		private static async Task<Resource> GetResourceAsync( HttpClient httpClient, Guid resourceId )
		{
			if ( resourceId == Guid.Empty )
			{
				throw new ArgumentException( $"{nameof( GetResourceAsync )} requires a non empty guid as {nameof( resourceId )}" );
			}

			var request = HttpMethod.Get.CreateRequest( $"resources/{resourceId}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				var content = await response.Content.ReadAsStringAsync();
				var resource = JsonConvert.DeserializeObject<Resource>( content );
				return resource;
			}

			return null;
		}
	}
}