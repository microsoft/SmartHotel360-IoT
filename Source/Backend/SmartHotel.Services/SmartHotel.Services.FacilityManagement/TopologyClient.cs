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
		Task<List<Hotel>> GetHotels();
	}

	public class TopologyClient : ITopologyClient
	{
		private readonly string ApiPath = "api/v1.0/";

		private readonly string SpacesPath = "spaces";
		private readonly string DevicesPath = "devices";
		private readonly string TypesPath = "types";

		private readonly string SpacesFilter = "includes=Parent";
		private readonly string DevicesFilter = "includes=Sensors";
		private readonly string TypesFilter = "names=Venue;Floor;Room&categories=SpaceType";

		private int _hotelTypeId = int.MinValue;
		private int _floorTypeId = int.MinValue;
		private int _roomTypeId = int.MinValue;

		private readonly IHttpClientFactory _clientFactory;
		private readonly IConfiguration _config;

		public TopologyClient( IConfiguration config, IHttpClientFactory clientFactory )
		{
			_clientFactory = clientFactory;
			_config = config;
		}

		public string AccessToken { get; set; }
		public async Task<List<Hotel>> GetHotels()
		{
			var httpClient = _clientFactory.CreateClient();
			string managementBaseUrl = _config["ManagementApiUrl"];
			string protectedManagementBaseUrl = managementBaseUrl.EndsWith( '/' ) ? managementBaseUrl : $"{managementBaseUrl}/";
			httpClient.BaseAddress = new Uri( protectedManagementBaseUrl );
			httpClient.DefaultRequestHeaders.Add( "Authorization", $"Bearer {AccessToken}" );

			Dictionary<string, Hotel> hotels = new Dictionary<string, Hotel>();

			// Separate for each iterations are required due to the flat and unordered nature of the topology json 
			await GetAndUpdateTypeIds( httpClient );

			var response = await GetFromDigitalTwins( httpClient, $"{ApiPath}{SpacesPath}?{SpacesFilter}" );
			dynamic topology = JsonConvert.DeserializeObject( response );

			// Hotels
			foreach ( var entry in topology )
			{
				if ( entry.parent != null && entry.parent.typeId == _hotelTypeId )
				{
					if ( !hotels.TryGetValue( entry.parent.id.ToString(), out Hotel hotel ) )
					{
						hotel = new Hotel();
						hotel.Id = entry.parent.id;
						hotel.Name = entry.parent.name;
						hotels.Add( hotel.Id, hotel );
					}
				}
			}

			// Floors
			foreach ( var entry in topology )
			{
				if ( entry.typeId == _floorTypeId )
				{
					if ( entry.parent != null && entry.parent.typeId == _hotelTypeId )
					{
						if ( hotels.TryGetValue( entry.parent.id.ToString(), out Hotel hotel ) )
						{
							if ( !hotel.FloorsDictionary.TryGetValue( entry.id.ToString(), out Floor floor ) )
							{
								floor = new Floor() { Id = entry.id, Name = entry.name };
								hotel.FloorsDictionary.Add( floor.Id, floor );
							}
						}
					}
				}
			}

			// Rooms
			Dictionary<string, Floor> floorsDictionary = hotels.SelectMany( h => h.Value.FloorsDictionary ).ToDictionary( pair => pair.Key, pair => pair.Value );

			foreach ( var entry in topology )
			{
				if ( entry.typeId == _roomTypeId )
				{
					if ( entry.parent != null && entry.parent.typeId == _floorTypeId )
					{
						if ( floorsDictionary.TryGetValue( entry.parent.id.ToString(), out Floor floor ) )
						{
							Room room = new Room() { Id = entry.id, Name = entry.name };
							floor.RoomDictionary.Add( room.Id, room );
						}
					}
				}
			}

			// Devices
			Dictionary<string, Room> roomsDictionary = hotels.SelectMany( h => h.Value.FloorsDictionary ).SelectMany( f => f.Value.RoomDictionary ).ToDictionary( pair => pair.Key, pair => pair.Value );

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

				if ( roomsDictionary.TryGetValue( device.SpaceId, out Room room ) )
				{
					room.Devices.Add( device );
				}
			}

			foreach ( var hotel in hotels.Values )
			{
				foreach ( var floor in hotel.FloorsDictionary.Values )
				{
					floor.Rooms = floor.RoomDictionary.Values.ToList();
				}

				hotel.Floors = hotel.FloorsDictionary.Values.ToList();
			}

			return hotels.Values.ToList();
		}

		private async Task GetAndUpdateTypeIds( HttpClient httpClient )
		{
			string typesResponse = await GetFromDigitalTwins( httpClient, $"{ApiPath}{TypesPath}?{TypesFilter}" );
			IReadOnlyCollection<DigitalTwinsType> types = JsonConvert.DeserializeObject<IReadOnlyCollection<DigitalTwinsType>>( typesResponse );

			foreach ( DigitalTwinsType type in types )
			{
				switch ( type.name.ToLower() )
				{
					case "venue":
						_hotelTypeId = type.id;
						break;
					case "floor":
						_floorTypeId = type.id;
						break;
					case "room":
						_roomTypeId = type.id;
						break;
				}
			}

			if ( _hotelTypeId.Equals( int.MinValue ) )
			{
				throw new NotSupportedException( "Missing the Hotel type id." );
			}

			if ( _floorTypeId.Equals( int.MinValue ) )
			{
				throw new NotSupportedException( "Missing the Floor type id." );
			}

			if ( _roomTypeId.Equals( int.MinValue ) )
			{
				throw new NotSupportedException( "Missing the Room type id." );
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