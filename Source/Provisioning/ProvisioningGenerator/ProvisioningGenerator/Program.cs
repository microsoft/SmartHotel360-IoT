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

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine( "Press Enter to continue..." );
			Console.ReadLine();
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
				Console.WriteLine( "Site provisioning file not written, skipping brand file generation." );
				return;
			}

			for ( int i = 0; i < site.Brands.Count; i++ )
			{
				Brand brand = site.Brands[i];
				GenerateBrandProvisioningFile( brand, i + 1, site.HotelTypes );
			}
		}

		private bool GenerateSiteProvisioningFile( List<Brand> brands )
		{
			string siteFilename = $"{OutputFilePrefix}_Site_Provisioning.yaml";

			if ( File.Exists( siteFilename ) )
			{
				var overwrite = Prompt.GetYesNo( "A site provisioning file already exists," +
												" would you like to overwrite it and any brand provisioning" +
												 $" files that already exist? ({siteFilename})", false );
				if ( !overwrite )
				{
					return false;
				}
			}

			var p = new ProvisioningDescription();
			p.AddEndpoint( new EndpointDescription { type = "EventHub", eventTypes = new List<string> { "DeviceMessage" } } );
			var tenantSpace = new SpaceDescription
			{
				name = "SmartHotel 360 Tenant",
				description = "This is the root node for the SmartHotel360 IoT Demo",
				friendlyName = "SmartHotel 360 Tenant",
				type = "Tenant",
				keystoreName = "SmartHotel360 Keystore"
			};
			SpaceDescription desiredTenantSpace = tenantSpace;
			p.AddSpace( tenantSpace );

			if ( !string.IsNullOrWhiteSpace( SubTenantName ) )
			{
				var subtenantSpace = new SpaceDescription
				{
					name = SubTenantName,
					description = $"This is the root node for the {SubTenantName} sub Tenant",
					friendlyName = SubTenantName,
					type = "Tenant"
				};
				tenantSpace.AddSpace( subtenantSpace );
				desiredTenantSpace = subtenantSpace;
			}
			else
			{
				tenantSpace.AddResource( new ResourceDescription { type = "IoTHub" } );
			}

			desiredTenantSpace.AddType( new TypeDescription { name = "Classic", category = "SensorType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "HotelBrand", category = "SpaceType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "Hotel", category = "SpaceType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "VIPFloor", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "QueenRoom", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "KingRoom", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "SuiteRoom", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "VIPSuiteRoom", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "ConferenceRoom", category = "SpaceSubType" } );
			desiredTenantSpace.AddType( new TypeDescription { name = "GymRoom", category = "SpaceSubType" } );

			desiredTenantSpace.AddPropertyKey( new PropertyKeyDescription
			{
				name = PropertyKeyDescription.DeviceIdPrefixName,
				primitiveDataType = "string",
				description = "Prefix used in sending Device Method calls to the IoT Hub.",
				min = "1",
				max = "250" // NOTE: since a value is required just setting this to a high value, no other reason.
			} );

			desiredTenantSpace.AddUser( "Head Of Operations" );

			foreach ( Brand brand in brands )
			{
				string brandFilename = GetBrandProvisioningFilename( brand );
				desiredTenantSpace.AddSpaceReference( new SpaceReferenceDescription { filename = brandFilename } );
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

		private void GenerateBrandProvisioningFile( Brand brand, int brandNumber, List<HotelType> hotelTypes )
		{
			string brandFilename = GetBrandProvisioningFilename( brand );

			var brandSpaceDescription = new SpaceDescription
			{
				name = brand.Name,
				description = $"SmartHotel360 {brand.Name}",
				friendlyName = brand.Name,
				type = "HotelBrand"
			};
			brandSpaceDescription.AddUser( $"Hotel Brand {brandNumber} Manager" );

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
				hotelSpaceDescription.AddUser( $"Hotel {hotelIndex + 1} Manager" );

				string brandHotelPrefix = $"{brand.Name}_{hotel.Name}_".Replace( " ", string.Empty );

				HotelType hotelType = hotelTypes.First( t => t.Name == hotel.Type );
				int numberRegularFloors = hotelType.TotalNumberFloors - hotelType.NumberVipFloors;

				// Create the floors
				for ( int floorIndex = 0; floorIndex < hotelType.TotalNumberFloors; floorIndex++ )
				{
					bool isVipFloor = floorIndex >= numberRegularFloors;
					var floorSpaceDescription = new SpaceDescription
					{
						name = $"Floor {floorIndex + 1:D02}",
						description = $"Floor {floorIndex + 1}",
						friendlyName = $"Floor {floorIndex + 1}",
						type = "Floor"
					};
					floorSpaceDescription.AddProperty( new PropertyDescription
					{
						name = PropertyKeyDescription.DeviceIdPrefixName,
						value = brandHotelPrefix
					} );

					if ( isVipFloor )
					{
						floorSpaceDescription.subType = "VIPFloor";
					}

					if ( !isVipFloor && !string.IsNullOrEmpty( hotel.RegularFloorEmployeeUser ) )
					{
						floorSpaceDescription.AddUser( $"Hotel {hotelIndex + 1} {hotel.RegularFloorEmployeeUser}" );
					}

					int numberOfRooms = isVipFloor ? hotelType.NumberRoomsPerVipFloor : hotelType.NumberRoomsPerRegularFloor;
					// Create the rooms
					for ( int roomIndex = 0; roomIndex < numberOfRooms; roomIndex++ )
					{
						string roomType = GetRoomType( roomIndex, numberOfRooms, isVipFloor );
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + roomIndex + 1, brandHotelPrefix,
							roomType, hotel.AddDevices );
						floorSpaceDescription.AddSpace( roomSpaceDescription );
					}

					if ( floorIndex == 0 && hotelType.IncludeGym )
					{
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + numberOfRooms + 1,
							brandHotelPrefix, "GymRoom", hotel.AddDevices );
						floorSpaceDescription.AddSpace( roomSpaceDescription );
					}

					if ( floorIndex == 1 && hotelType.IncludeConferenceRoom )
					{
						SpaceDescription roomSpaceDescription = CreateRoom( 100 * ( floorIndex + 1 ) + numberOfRooms + 1,
							brandHotelPrefix, "ConferenceRoom", hotel.AddDevices );
						floorSpaceDescription.AddSpace( roomSpaceDescription );
					}

					hotelSpaceDescription.AddSpace( floorSpaceDescription );
				}

				brandSpaceDescription.AddSpace( hotelSpaceDescription );
			}

			var yamlSerializer = new SerializerBuilder()
				.Build();
			string serializedProvisioningDescription = yamlSerializer.Serialize( brandSpaceDescription );
			File.WriteAllText( brandFilename, serializedProvisioningDescription );

			Console.WriteLine( $"Successfully created brand provisioning file: {brandFilename}" );
		}

		private string GetRoomType( int roomIndex, int numberRoomsOnFloor, bool isVipFloor )
		{
			if ( isVipFloor )
			{
				int oneQuarter = (int)Math.Ceiling( (double)numberRoomsOnFloor / 4.0D );
				if ( roomIndex < oneQuarter )
				{
					return "SuiteRoom";
				}
				else
				{
					return "VIPSuiteRoom";
				}
			}
			else
			{
				int oneThird = (int)Math.Ceiling( (double)numberRoomsOnFloor / 3.0D );
				if ( roomIndex < oneThird )
				{
					return "QueenRoom";
				}
				else if ( roomIndex < 2 * oneThird )
				{
					return "KingRoom";
				}
				else
				{
					return "SuiteRoom";
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
				thermostatDevice.AddSensor( new SensorDescription { dataType = "Temperature", type = "Classic" } );
				roomSpaceDescription.AddDevice( thermostatDevice );

				var motionDevice = new DeviceDescription
				{
					name = "Motion",
					hardwareId = $"{brandHotelPrefix}{roomNumber}_M",
				};
				motionDevice.AddSensor( new SensorDescription { dataType = "Motion", type = "Classic" } );
				roomSpaceDescription.AddDevice( motionDevice );

				var lightsDevice = new DeviceDescription
				{
					name = "Light",
					hardwareId = $"{brandHotelPrefix}{roomNumber}_L",
				};
				lightsDevice.AddSensor( new SensorDescription { dataType = "Light", type = "Classic" } );
				roomSpaceDescription.AddDevice( lightsDevice );
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
				IncludeConferenceRoom = true,
				IncludeGym = true
			};

			var hotelTypeL = new HotelType
			{
				Name = "L",
				TotalNumberFloors = 10,
				NumberVipFloors = 2,
				NumberRoomsPerRegularFloor = 15,
				NumberRoomsPerVipFloor = 8,
				IncludeConferenceRoom = true,
				IncludeGym = true
			};

			var hotelTypeSH = new HotelType
			{
				Name = "SH",
				TotalNumberFloors = 5,
				NumberVipFloors = 1,
				NumberRoomsPerRegularFloor = 10,
				NumberRoomsPerVipFloor = 4,
				IncludeConferenceRoom = true,
				IncludeGym = true
			};

			var hotelTypeSL = new HotelType
			{
				Name = "SL",
				TotalNumberFloors = 5,
				NumberVipFloors = 1,
				NumberRoomsPerRegularFloor = 10,
				NumberRoomsPerVipFloor = 4,
				IncludeConferenceRoom = true,
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
