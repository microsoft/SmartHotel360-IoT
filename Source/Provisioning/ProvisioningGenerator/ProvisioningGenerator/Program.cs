using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartHotel.IoT.Provisioning.Common.Models;
using YamlDotNet.Serialization;

namespace ProvisioningGenerator
{
	class Program
	{
		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		[Option( "-op|--outputPrefix", Description = "Prefix of the output filenames" )]
		public string OutputFilePrefix { get; } = "SmartHotel";

		[Option( "-df|--definitionFilepath", Description = "Filepath of json brand definition" )]
		public string DefinitionFilepath { get; } = Path.Combine( "SampleDefinitions", "MasterJson", "ReducedSiteDefinition.json" );

		[Option( "-st|--subTenantName", Description = "Create a sub-Tenant with the given name" )]
		public string SubTenantName { get; }

		[Option( "-ad|--allDevices", Description = "Add devices to all rooms of all hotels" )]
		public bool AllDevices { get; }

		private async Task OnExecuteAsync()
		{
			//GenerateSampleDefinition(DefinitionFilename);

			GenerateProvisioningFiles();
		}

		private void GenerateProvisioningFiles()
		{
			if ( !File.Exists( DefinitionFilepath ) )
			{
				Console.WriteLine( $"Definition file not found: {DefinitionFilepath}" );
				return;
			}

			string siteJson = File.ReadAllText( DefinitionFilepath );
			var site = JsonConvert.DeserializeObject<Site>( siteJson );

			if ( !GenerateSiteProvisioningFile( site.Brands ) )
			{
				return;
			}

			for ( int i = 0; i < site.Brands.Count; i++ )
			{
				Brand brand = site.Brands[i];
				if ( !GenerateBrandProvisioningFile( brand, i + 1, site.HotelTypes ) )
				{
					return;
				}
			}
		}

		private bool GenerateSiteProvisioningFile( List<Brand> brands )
		{
			string siteFilename = $"{OutputFilePrefix}_Site_Provisioning.yaml";

			if ( File.Exists( siteFilename ) )
			{
				Console.WriteLine( $"Error: A file with the requested output name already exists: {siteFilename}" );
				return false;
			}

			var p = new ProvisioningDescription();
			p.endpoints.Add( new EndpointDescription { type = "EventHub", eventTypes = new List<string> { "DeviceMessage" } } );
			var tenantSpace = new SpaceDescription
			{
				name = "SmartHotel 360 Tenant",
				description = "This is the root node for the SmartHotel360 IoT Demo",
				friendlyName = "SmartHotel 360 Tenant",
				type = "Tenant",
				keystoreName = "SmartHotel360 Keystore"
			};
			SpaceDescription desiredTenantSpace = tenantSpace;
			p.spaces.Add( tenantSpace );

			if ( !string.IsNullOrWhiteSpace( SubTenantName ) )
			{
				var subtenantSpace = new SpaceDescription
				{
					name = SubTenantName,
					description = $"This is the root node for the {SubTenantName} sub Tenant",
					friendlyName = SubTenantName,
					type = "Tenant"
				};
				tenantSpace.spaces.Add( subtenantSpace );
				desiredTenantSpace = subtenantSpace;
			}
			else
			{
				tenantSpace.resources.Add( new ResourceDescription { type = "IoTHub" } );
			}

			desiredTenantSpace.types.Add( new TypeDescription { name = "Classic", category = "SensorType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "HotelBrand", category = "SpaceType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "Hotel", category = "SpaceType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "VIP", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "Queen", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "King", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "Suite", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "VIPSuite", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "Ballroom", category = "SpaceSubType" } );
			desiredTenantSpace.types.Add( new TypeDescription { name = "Gym", category = "SpaceSubType" } );

			desiredTenantSpace.users.Add( "Head Of Operations" );

			foreach ( Brand brand in brands )
			{
				string brandFilename = GetBrandProvisioningFilename( brand );
				desiredTenantSpace.spaceReferences.Add( new SpaceReferenceDescription { filename = brandFilename } );
			}

			var yamlSerializer = new Serializer();
			string serializedProvisioningDescription = yamlSerializer.Serialize( p );
			File.WriteAllText( siteFilename, serializedProvisioningDescription );

			Console.WriteLine( $"Successfully created site provisioning file: {siteFilename}" );

			return true;
		}

		private string GetBrandProvisioningFilename( Brand brand )
		{
			return $"{OutputFilePrefix}_{brand.Name}_Provisioning.yaml";
		}

		private bool GenerateBrandProvisioningFile( Brand brand, int brandNumber, List<HotelType> hotelTypes )
		{
			string brandFilename = GetBrandProvisioningFilename( brand );

			if ( File.Exists( brandFilename ) )
			{
				Console.WriteLine( $"Error: A file with the requested output name already exists: {brandFilename}" );
				return false;
			}

			var brandSpaceDescription = new SpaceDescription
			{
				name = brand.Name,
				description = $"SmartHotel360 {brand.Name}",
				friendlyName = brand.Name,
				type = "HotelBrand"
			};
			brandSpaceDescription.users.Add( $"Hotel Brand {brandNumber} Manager" );

			// Create the hotels
			for ( int hotelIndex = 0; hotelIndex < brand.Hotels.Count; hotelIndex++ )
			{
				Hotel hotel = brand.Hotels[hotelIndex];
				var hotelSpaceDescription = new SpaceDescription
				{
					name = hotel.Name,
					description = $"SmartHotel360 {hotel.Name}",
					friendlyName = hotel.Name,
					type = "Hotel"
				};
				hotelSpaceDescription.users.Add( $"Hotel {hotelIndex + 1} Manager" );

				string brandHotelPrefix = $"{brand.Name}_{hotel.Name}_";

				HotelType hotelType = hotelTypes.First( t => t.Name == hotel.Type );
				int numberRegularFloors = hotelType.TotalNumberFloors - hotelType.NumberVipFloors;

				// Create the regular floors
				for ( int floorIndex = 0; floorIndex < numberRegularFloors; floorIndex++ )
				{
					var floorSpaceDescription = new SpaceDescription
					{
						name = $"Floor {floorIndex + 1:D02}",
						description = $"Floor {floorIndex + 1}",
						friendlyName = $"Floor {floorIndex + 1}",
						type = "Floor"
					};

					if ( !string.IsNullOrEmpty( hotel.RegularFloorEmployeeUser ) )
					{
						floorSpaceDescription.users.Add( $"Hotel {hotelIndex + 1} {hotel.RegularFloorEmployeeUser}" );
					}

					// Create the rooms
					for ( int roomIndex = 0; roomIndex < hotelType.NumberRoomsPerRegularFloor; roomIndex++ )
					{
						string roomType = GetRoomType( roomIndex, hotelType.NumberRoomsPerRegularFloor, false );
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + roomIndex + 1, brandHotelPrefix,
							roomType, hotel.AddDevices );
						floorSpaceDescription.spaces.Add( roomSpaceDescription );
					}

					if ( floorIndex == 0 && hotelType.IncludeGym )
					{
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + hotelType.NumberRoomsPerRegularFloor + 1,
							brandHotelPrefix, "Gym", hotel.AddDevices );
						floorSpaceDescription.spaces.Add( roomSpaceDescription );
					}

					if ( floorIndex == 1 && hotelType.IncludeBallroom )
					{
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + hotelType.NumberRoomsPerRegularFloor + 1,
							brandHotelPrefix, "Ballroom", hotel.AddDevices );
						floorSpaceDescription.spaces.Add( roomSpaceDescription );
					}
				}

				//TODO: Create the VIP Floor related spaces and devices
				// Create the VIP floors
				//for ( int i = numberRegularFloors; i < hotelType.TotalNumberFloors; i++ )
				//{
				//	outputFile.WriteLine( $"  - name: VIPFloor {i + 1:D02}" );
				//	outputFile.WriteLine( $"    description: VIPFloor {i + 1}" );
				//	outputFile.WriteLine( $"    friendlyName: VIPFloor {i + 1}" );
				//	outputFile.WriteLine( $"    type: VIPFloor" );
				//	outputFile.WriteLine( $"    spaces:" );

				//	// Create the rooms
				//	for ( int j = 0; j < hotelType.NumberRoomsPerVipFloor; j++ )
				//	{
				//		string roomType = GetRoomType( j, hotelType.NumberRoomsPerRegularFloor, true );
				//		CreateRoom( outputFile, 100 * ( i + 1 ) + j + 1, brandHotelPrefix, roomType, hotel.AddDevices );
				//	}
				//}
			}

			//TODO: serialize and save out the brand file

			Console.WriteLine( $"Successfully created brand provisioning file: {brandFilename}" );

			return true;
		}

		private string GetRoomType( int roomIndex, int numberRoomsOnFloor, bool isVipFloor )
		{
			if ( isVipFloor )
			{
				int oneQuarter = (int)Math.Ceiling( (double)numberRoomsOnFloor / 4.0D );
				if ( roomIndex < oneQuarter )
				{
					return "Suite";
				}
				else
				{
					return "VIPSuite";
				}
			}
			else
			{
				int oneThird = (int)Math.Ceiling( (double)numberRoomsOnFloor / 3.0D );
				if ( roomIndex < oneThird )
				{
					return "Queen";
				}
				else if ( roomIndex < 2 * oneThird )
				{
					return "King";
				}
				else
				{
					return "Suite";
				}
			}
		}

		private SpaceDescription CreateRoom( int roomNumber, string brandHotelPrefix, string roomType, bool addDevices )
		{
			var roomSpaceDescription = new SpaceDescription
			{
				name = $"Room {roomNumber}",
				description = $"Room {roomNumber}",
				friendlyName = $"Room {roomNumber}",
				type = "Room",
				subType = roomType
			};

			if ( addDevices )
			{
				var thermostatDevice = new DeviceDescription
				{
					name = "Thermostat",
					hardwareId = $"{brandHotelPrefix}{roomNumber}_T",
				};
				thermostatDevice.sensors.Add( new SensorDescription { dataType = "Temperature", type = "Classic" } );
				roomSpaceDescription.devices.Add( thermostatDevice );

				var motionDevice = new DeviceDescription
				{
					name = "Motion",
					hardwareId = $"{brandHotelPrefix}{roomNumber}_M",
				};
				motionDevice.sensors.Add( new SensorDescription { dataType = "Motion", type = "Classic" } );
				roomSpaceDescription.devices.Add( motionDevice );

				var lightsDevice = new DeviceDescription
				{
					name = "Light",
					hardwareId = $"{brandHotelPrefix}{roomNumber}_L",
				};
				lightsDevice.sensors.Add( new SensorDescription { dataType = "Light", type = "Classic" } );
				roomSpaceDescription.devices.Add( lightsDevice );
			}

			return roomSpaceDescription;
		}

		private void GenerateSampleDefinition( string definitionFilename )
		{
			var hotelTypeH = new HotelType
			{
				Name = "H",
				TotalNumberFloors = 10,
				NumberVipFloors = 2,
				NumberRoomsPerRegularFloor = 20,
				NumberRoomsPerVipFloor = 10,
				IncludeBallroom = true,
				IncludeGym = true
			};

			var hotelTypeL = new HotelType
			{
				Name = "L",
				TotalNumberFloors = 10,
				NumberVipFloors = 2,
				NumberRoomsPerRegularFloor = 15,
				NumberRoomsPerVipFloor = 8,
				IncludeBallroom = true,
				IncludeGym = true
			};

			var hotelTypeSH = new HotelType
			{
				Name = "SH",
				TotalNumberFloors = 5,
				NumberVipFloors = 1,
				NumberRoomsPerRegularFloor = 10,
				NumberRoomsPerVipFloor = 4,
				IncludeBallroom = true,
				IncludeGym = true
			};

			var hotelTypeSL = new HotelType
			{
				Name = "SL",
				TotalNumberFloors = 5,
				NumberVipFloors = 1,
				NumberRoomsPerRegularFloor = 10,
				NumberRoomsPerVipFloor = 4,
				IncludeBallroom = true,
				IncludeGym = true
			};

			var hotelTypes = new List<HotelType> { hotelTypeH, hotelTypeL, hotelTypeSH, hotelTypeSL };

			var brands = new List<Brand>();

			for ( int i = 0; i < 4; i++ )
			{
				var hotels = new List<Hotel>();

				switch ( i )
				{
					case 0:
						{
							hotels.Add( CreateHotel( hotelTypeH, 1, "Employee", true ) );
							hotels.Add( CreateHotel( hotelTypeL, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSH, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSH, 2, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 2, null, AllDevices ) );
							break;
						}
					case 1:
						{
							hotels.Add( CreateHotel( hotelTypeSH, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSH, 2, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 1, null, AllDevices ) );
							break;
						}
					case 2:
						{
							hotels.Add( CreateHotel( hotelTypeSH, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 2, null, AllDevices ) );
							break;
						}
					case 3:
						{
							hotels.Add( CreateHotel( hotelTypeSH, 1, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSH, 2, null, AllDevices ) );
							hotels.Add( CreateHotel( hotelTypeSL, 1, null, AllDevices ) );
							break;
						}
				}

				var brand = new Brand
				{
					Name = $"Brand {i + 1}",
					Hotels = hotels
				};

				brands.Add( brand );
			}

			var site = new Site
			{
				HotelTypes = hotelTypes,
				Brands = brands
			};

			string siteJson = JsonConvert.SerializeObject( site, Formatting.Indented );
			using ( StreamWriter definitionFile = new StreamWriter( definitionFilename ) )
			{
				definitionFile.Write( siteJson );
			}
		}

		private Hotel CreateHotel( HotelType hotelType, int hotelIndex, string regularFloorEmployeeUser, bool addDevices )
		{
			return new Hotel
			{
				Name = $"Hotel {hotelType.Name} {hotelIndex}",
				Type = hotelType.Name,
				RegularFloorEmployeeUser = regularFloorEmployeeUser,
				AddDevices = addDevices
			};
		}
	}
}
