using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartHotel.Services.FacilityManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartHotel.Services.FacilityManagement
{
	public interface ITopologyClient
	{
		string AccessToken { get; set; }
		Task<ICollection<Space>> GetSpaces();
		Task<Dictionary<string, string>> GetSpaceTemperatureAlerts();
	}

	public class TopologyClient : ITopologyClient
	{
		private const string TenantTypeName = "Tenant";
		private const string HotelBrandTypeName = "HotelBrand";
		private const string HotelTypeName = "Hotel";
		private const string FloorTypeName = "Floor";
		private const string RoomTypeName = "Room";
		private readonly string ApiPath = "api/v1.0/";

		private readonly string SpacesPath = "spaces";

		private const string FirstFourLevelsSpacesFilter = "maxlevel=4&minlevel=1";
		private const string FifthLevelSpacesFilter = "maxlevel=5&minlevel=5";
		private const string IncludesFilter = "includes";
		private const string PropertiesIncludesFilter = "Properties";
		private const string TypesIncludesFilter = "Types";
		private const string ValuesIncludesFilter = "Values";

		private readonly IHttpClientFactory _clientFactory;
		private readonly IConfiguration _config;

		public TopologyClient( IConfiguration config, IHttpClientFactory clientFactory )
		{
			_clientFactory = clientFactory;
			_config = config;
		}

		public string AccessToken { get; set; }

		public async Task<ICollection<Space>> GetSpaces()
		{
			HttpClient httpClient = CreateHttpClient();

			var firstFourLevelsResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}" +
				$"?{FirstFourLevelsSpacesFilter}&{IncludesFilter}={PropertiesIncludesFilter},{TypesIncludesFilter}" );
			var topology = JsonConvert.DeserializeObject<ICollection<DigitalTwinsSpace>>( firstFourLevelsResponse );

			var fifthLevelResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}" +
																			$"?{FifthLevelSpacesFilter}&{IncludesFilter}={TypesIncludesFilter}" );
			var fithLevelTopology = JsonConvert.DeserializeObject<ICollection<DigitalTwinsSpace>>( fifthLevelResponse );
			topology = topology.Union( fithLevelTopology ).ToArray();

			Space tenantSpace = null;
			Space hotelBrandSpace = null;
			Space hotelSpace = null;
			Space floorSpace = null;

			var spacesByParentId = new Dictionary<string, List<Space>>();
			foreach ( DigitalTwinsSpace dtSpace in topology )
			{
				var space = new Space
				{
					Id = dtSpace.id,
					Name = dtSpace.name,
					FriendlyName = dtSpace.friendlyName,
					Type = dtSpace.type,
					TypeId = dtSpace.typeId,
					Subtype = dtSpace.subtype,
					SubtypeId = dtSpace.subtypeId,
					ParentSpaceId = dtSpace.parentSpaceId ?? string.Empty,
					Properties = dtSpace.properties?.ToList()
				};

				if ( tenantSpace == null && TenantTypeName.Equals( dtSpace.type, StringComparison.OrdinalIgnoreCase ) )
				{
					tenantSpace = space;
				}
				else if ( tenantSpace == null
						 && hotelBrandSpace == null
						 && HotelBrandTypeName.Equals( dtSpace.type, StringComparison.OrdinalIgnoreCase ) )
				{
					hotelBrandSpace = space;
				}
				else if ( tenantSpace == null
						 && hotelBrandSpace == null
						 && hotelSpace == null
						 && HotelTypeName.Equals( dtSpace.type, StringComparison.OrdinalIgnoreCase ) )
				{
					hotelSpace = space;
				}
				else if ( tenantSpace == null
						  && hotelBrandSpace == null
						  && hotelSpace == null
						  && floorSpace == null
						  && FloorTypeName.Equals( dtSpace.type, StringComparison.OrdinalIgnoreCase ) )
				{
					floorSpace = space;
				}

				if ( !spacesByParentId.TryGetValue( space.ParentSpaceId, out List<Space> spaces ) )
				{
					spaces = new List<Space>();
					spacesByParentId.Add( space.ParentSpaceId, spaces );
				}

				spaces.Add( space );
			}

			var hierarchicalSpaces = new List<Space>();
			Space highestLevelSpace = GetHighestLevelSpace( tenantSpace, hotelBrandSpace, hotelSpace, floorSpace );
			if ( highestLevelSpace != null )
			{
				string highestLevelParentSpaceId = highestLevelSpace.ParentSpaceId;
				hierarchicalSpaces.AddRange( spacesByParentId[highestLevelParentSpaceId] );
				BuildSpaceHierarchyAndReturnRoomSpaces( hierarchicalSpaces, spacesByParentId );
			}

			if ( hierarchicalSpaces.Count == 1 && !FloorTypeName.Equals( hierarchicalSpaces[0].Type, StringComparison.OrdinalIgnoreCase ) )
			{
				// If there is only one root space, then ensuring we only send the child spaces to the client so it knows
				// to start showing those children.
				hierarchicalSpaces = hierarchicalSpaces[0].ChildSpaces;
			}


			return hierarchicalSpaces;
		}

		public async Task<Dictionary<string, string>> GetSpaceTemperatureAlerts()
		{
			HttpClient httpClient = CreateHttpClient();

			var firstFourLevelsResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}" +
																				 $"?{FirstFourLevelsSpacesFilter}&{IncludesFilter}={TypesIncludesFilter}" );
			Dictionary<string, DigitalTwinsSpace> firstFourLevelsTopology = JsonConvert.DeserializeObject<ICollection<DigitalTwinsSpace>>( firstFourLevelsResponse )
				.ToDictionary( dts => dts.id );

			var fifthLevelResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}" +
																			$"?{FifthLevelSpacesFilter}&{IncludesFilter}={ValuesIncludesFilter},{TypesIncludesFilter}" );
			var fifthLevelTopologyWithValues = JsonConvert.DeserializeObject<ICollection<DigitalTwinsSpace>>( fifthLevelResponse );
			var spacesWithTemperatureAlerts = fifthLevelTopologyWithValues
				.Where( dts => dts.values != null )
				.Select( dts => new
				{
					dts,
					value = dts.values.FirstOrDefault( v => "TemperatureAlert".Equals( v.type, StringComparison.OrdinalIgnoreCase ) )
				} )
				.Where( ta => ta.value != null )
				.ToArray();

			Dictionary<string, string> alertMessagesByFriendlySpaceIds = new Dictionary<string, string>();
			foreach ( var spaceWithAlert in spacesWithTemperatureAlerts )
			{
				string friendlyIdentifier = GetFriendlySpaceIdentifier( spaceWithAlert.dts, firstFourLevelsTopology );
				alertMessagesByFriendlySpaceIds.Add( friendlyIdentifier, spaceWithAlert.value.value );
			}

			return alertMessagesByFriendlySpaceIds;
		}

		private string GetFriendlySpaceIdentifier( DigitalTwinsSpace space, Dictionary<string, DigitalTwinsSpace> firstFourLevelsTopology )
		{
			string identifier = string.Empty;
			if ( IsTypeNeededForFriendlyIdentifier( space.type ) )
			{
				identifier = space.friendlyName;
			}

			if ( !string.IsNullOrWhiteSpace( space.parentSpaceId )
				 && firstFourLevelsTopology.TryGetValue( space.parentSpaceId, out DigitalTwinsSpace parentSpace ) )
			{
				string parentIdentifier = GetFriendlySpaceIdentifier( parentSpace, firstFourLevelsTopology );
				if ( !string.IsNullOrWhiteSpace( parentIdentifier ) )
				{
					identifier = string.IsNullOrWhiteSpace( identifier ) ? parentIdentifier : $"{parentIdentifier} -> {identifier}";
				}
			}

			return identifier;
		}

		private static void BuildSpaceHierarchyAndReturnRoomSpaces( List<Space> hierarchicalSpaces, Dictionary<string, List<Space>> allSpacesByParentId )
		{
			foreach ( Space parentSpace in hierarchicalSpaces )
			{
				if ( allSpacesByParentId.TryGetValue( parentSpace.Id, out List<Space> childSpaces ) )
				{
					parentSpace.ChildSpaces.AddRange( childSpaces );
					BuildSpaceHierarchyAndReturnRoomSpaces( childSpaces, allSpacesByParentId );
				}
			}
		}

		private Space GetHighestLevelSpace( Space tenantSpace, Space hotelBrandSpace, Space hotelSpace, Space floorSpace )
		{
			if ( tenantSpace != null )
			{
				return tenantSpace;
			}

			if ( hotelBrandSpace != null )
			{
				return hotelBrandSpace;
			}

			if ( hotelSpace != null )
			{
				return hotelSpace;
			}

			if ( floorSpace != null )
			{
				return floorSpace;
			}

			return null;
		}

		private HttpClient CreateHttpClient()
		{
			var httpClient = _clientFactory.CreateClient();
			string managementBaseUrl = _config["ManagementApiUrl"];
			string protectedManagementBaseUrl = managementBaseUrl.EndsWith( '/' ) ? managementBaseUrl : $"{managementBaseUrl}/";
			httpClient.BaseAddress = new Uri( protectedManagementBaseUrl );
			httpClient.DefaultRequestHeaders.Add( "Authorization", $"Bearer {AccessToken}" );
			return httpClient;
		}

		private async Task<string> GetFromDigitalTwins( HttpClient httpClient, string requestUri )
		{
			HttpResponseMessage httpResponse = await httpClient.GetAsync( requestUri );
			string content = await httpResponse.Content.ReadAsStringAsync();
			if ( !httpResponse.IsSuccessStatusCode )
			{
				throw new Exception( $"Error when calling Digital Twins with request ({requestUri}): {content}" );
			}

			return content;
		}

		private bool IsTypeNeededForFriendlyIdentifier( string type )
		{
			return RoomTypeName.Equals( type, StringComparison.OrdinalIgnoreCase )
				   || HotelTypeName.Equals( type, StringComparison.OrdinalIgnoreCase )
				   || HotelBrandTypeName.Equals( type, StringComparison.OrdinalIgnoreCase );
		}
	}
}