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
	}

	public class TopologyClient : ITopologyClient
	{
		private readonly Dictionary<int, string> _typesById = new Dictionary<int, string>();
		private const string HotelBrandTypeName = "HotelBrand";
		private const string HotelTypeName = "Venue";
		private const string FloorTypeName = "Floor";
		private const string RoomTypeName = "Room";
		private readonly Dictionary<string, int> _typeIdsByName = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase )
		{
			{HotelBrandTypeName, int.MinValue},
			{HotelTypeName, int.MinValue},
			{FloorTypeName, int.MinValue},
			{RoomTypeName, int.MinValue},
		};
		private readonly string ApiPath = "api/v1.0/";

		private readonly string SpacesPath = "spaces";
		private readonly string DevicesPath = "devices";
		private readonly string TypesPath = "types";

		private readonly string SpacesFilter = "includes=Parent";
		private readonly string DevicesFilter = "includes=Sensors";
		private readonly string TypesFilter = "names=HotelBrand;Venue;Floor;Room&categories=SpaceType";

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
			var httpClient = _clientFactory.CreateClient();
			string managementBaseUrl = _config["ManagementApiUrl"];
			string protectedManagementBaseUrl = managementBaseUrl.EndsWith( '/' ) ? managementBaseUrl : $"{managementBaseUrl}/";
			httpClient.BaseAddress = new Uri( protectedManagementBaseUrl );
			httpClient.DefaultRequestHeaders.Add( "Authorization", $"Bearer {AccessToken}" );

			await GetAndUpdateTypeIds( httpClient );

			var response = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}?{SpacesFilter}" );
			dynamic topology = JsonConvert.DeserializeObject( response );

			Space hotelBrandSpace = null;
			Space hotelSpace = null;
			Space floorSpace = null;

			var spacesByParentId = new Dictionary<string, List<Space>>();
			foreach ( var entry in topology )
			{
				if ( _typesById.TryGetValue( entry.typeId, out string typeName ) )
				{
					var space = new Space
					{
						Id = entry.id,
						Name = entry.name,
						Type = typeName,
						TypeId = entry.typeId,
						ParentSpaceId = entry.parent != null ? entry.parent.id : null
					};

					if ( string.Equals( HotelBrandTypeName, typeName, StringComparison.OrdinalIgnoreCase ) )
					{
						hotelBrandSpace = space;
					}
					else if ( string.Equals( HotelTypeName, typeName, StringComparison.OrdinalIgnoreCase ) )
					{
						hotelSpace = space;
					}
					else if ( string.Equals( FloorTypeName, typeName, StringComparison.OrdinalIgnoreCase ) )
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
			}

			var hierarchicalSpaces = new List<Space>();
			Space highestLevelSpace = GetHighestLevelSpace( hotelBrandSpace, hotelSpace, floorSpace );
			if ( highestLevelSpace != null )
			{
				string highestLevelParentSpaceId = highestLevelSpace.ParentSpaceId;
				hierarchicalSpaces.AddRange( spacesByParentId[highestLevelParentSpaceId] );
				ICollection<Space> roomSpaces = BuildSpaceHierarchyAndReturnRoomSpaces( hierarchicalSpaces, spacesByParentId );
				IDictionary<string, Space> roomSpacesById = roomSpaces.ToDictionary( s => s.Id );

				// Devices
				var deviceResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{DevicesPath}?{DevicesFilter}" );
				dynamic devices = JsonConvert.DeserializeObject( deviceResponse );

				foreach ( var deviceEntry in devices )
				{
					Device device = new Device();
					device.Id = deviceEntry.id;
					device.Name = deviceEntry.name;
					device.HardwareId = deviceEntry.hardwareId;
					device.SpaceId = deviceEntry.spaceId;

					foreach ( var sensorEntry in deviceEntry.sensors )
					{
						Sensor sensor = new Sensor();
						sensor.Id = sensorEntry.id;
						sensor.SpaceId = sensorEntry.spaceId;
						sensor.DataTypeId = sensorEntry.dataTypeId;
						sensor.DeviceId = sensorEntry.deviceId;

						device.Sensors.Add( sensor );
					}

					if ( roomSpacesById.TryGetValue( device.SpaceId, out Space roomSpace ) )
					{
						roomSpace.Devices.Add( device );
					}
				}
			}

			return hierarchicalSpaces;
		}

		private static ICollection<Space> BuildSpaceHierarchyAndReturnRoomSpaces( List<Space> hierarchicalSpaces, Dictionary<string, List<Space>> allSpacesByParentId )
		{
			var roomSpaces = new List<Space>();
			foreach ( Space parentSpace in hierarchicalSpaces )
			{
				if ( allSpacesByParentId.TryGetValue( parentSpace.Id, out List<Space> childSpaces ) )
				{
					parentSpace.ChildSpaces.AddRange( childSpaces );
					ICollection<Space> result = BuildSpaceHierarchyAndReturnRoomSpaces( childSpaces, allSpacesByParentId );
					roomSpaces.AddRange( result );
				}

				if ( string.Equals( RoomTypeName, parentSpace.Type, StringComparison.OrdinalIgnoreCase ) )
				{
					roomSpaces.Add( parentSpace );
				}
			}

			return roomSpaces;
		}

		private Space GetHighestLevelSpace( Space hotelBrandSpace, Space hotelSpace, Space floorSpace )
		{
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

		private async Task GetAndUpdateTypeIds( HttpClient httpClient )
		{
			string typesResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{TypesPath}?{TypesFilter}" );
			IReadOnlyCollection<DigitalTwinsType> types = JsonConvert.DeserializeObject<IReadOnlyCollection<DigitalTwinsType>>( typesResponse );

			foreach ( DigitalTwinsType type in types )
			{
				_typesById[type.id] = type.name;
				_typeIdsByName[type.name] = type.id;
			}

			var typesMissingId = _typeIdsByName.Where( kvp => int.MinValue.Equals( kvp.Value ) ).ToArray();
			if ( typesMissingId.Length > 0 )
			{
				throw new NotSupportedException( $"Missing the following type Ids: {string.Join( ", ", typesMissingId )}" );
			}
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
	}
}