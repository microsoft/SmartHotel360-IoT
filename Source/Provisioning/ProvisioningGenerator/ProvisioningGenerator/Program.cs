﻿using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningGenerator
{
    class Program
    {
        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

        [Option("-op|--outputPrefix", Description = "Prefix of the output filenames")]
        public string OutputFilePrefix { get; } = "SmartHotel";

        [Option("-df|--definitionFilepath", Description = "Filepath of json brand definition")]
        public string DefinitionFilepath { get; } = Path.Combine("SampleDefinitions", "MasterJson", "ReducedSiteDefinition.json");

        [Option("-st|--subTenantName", Description = "Create a sub-Tenant with the given name")]
        public string SubTenantName { get; }

        [Option("-ad|--allDevices", Description = "Add devices to all rooms of all hotels")]
        public bool AllDevices { get; }

        private async Task OnExecuteAsync()
        {
            //GenerateSampleDefinition(DefinitionFilename);

            GenerateProvisioningFiles();
        }

        private void GenerateProvisioningFiles()
        {
            if (!File.Exists(DefinitionFilepath))
            {
                Console.WriteLine($"Definition file not found: {DefinitionFilepath}");
                return;
            }

            using (StreamReader definitionFile = new StreamReader(DefinitionFilepath))
            {
                string siteJson = definitionFile.ReadToEnd();

                var site = JsonConvert.DeserializeObject<Site>(siteJson);

                if (!GenerateSiteProvisioningFile(site.Brands))
                {
                    return;
                }

                foreach (var brand in site.Brands)
                {
                    if (!GenerateBrandProvisioningFile(brand, site.HotelTypes))
                    {
                        return;
                    }
                }
            }
        }

        private bool GenerateSiteProvisioningFile(List<Brand> brands)
        {
            string siteFilename = $"{OutputFilePrefix}_Site_Provisioning.yaml";

            if (File.Exists(siteFilename))
            {
                Console.WriteLine($"Error: A file with the requested output name already exists: {siteFilename}");
                return false;
            }

            using (StreamWriter outputFile = new StreamWriter(siteFilename))
            {
                outputFile.WriteLine("endpoints:");
                outputFile.WriteLine("- type: EventHub");
                outputFile.WriteLine("  eventTypes:");
                outputFile.WriteLine("  - DeviceMessage");

                outputFile.WriteLine("spaces:");
                outputFile.WriteLine("- name: SmartHotel 360 Tenant");
                outputFile.WriteLine("  description: This is the root node for the SmartHotel360 IoT Demo");
                outputFile.WriteLine("  friendlyName: SmartHotel 360 Tenant");
                outputFile.WriteLine("  type: Tenant");
                outputFile.WriteLine("  keystoreName: SmartHotel360 Keystore");

                string subTenantExtraSpacing;

                if (String.IsNullOrWhiteSpace(SubTenantName))
                {
                    outputFile.WriteLine("  resources:");
                    outputFile.WriteLine("  - type: IoTHub");
                    outputFile.WriteLine("  types:");
                    outputFile.WriteLine("  - name: Classic");
                    outputFile.WriteLine("    category: SensorType");
                    outputFile.WriteLine("  - name: HotelBrand");
                    outputFile.WriteLine("    category: SpaceType");
                    outputFile.WriteLine("  - name: Hotel");
                    outputFile.WriteLine("    category: SpaceType");
                    outputFile.WriteLine("  - name: VIPFloor");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: QueenRoom");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: KingRoom");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: Suite");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: VIPSuite");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: Ballroom");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  - name: Gym");
                    outputFile.WriteLine("    category: SpaceSubtype");
                    outputFile.WriteLine("  users:");
                    outputFile.WriteLine("  - Manager");
                    outputFile.WriteLine("  spaceReferences:");

                    subTenantExtraSpacing = "";
                }
                else
                {
                    outputFile.WriteLine($"  spaces:");
                    outputFile.WriteLine($"  - name: {SubTenantName}");
                    outputFile.WriteLine($"    description: This is the root node for the {SubTenantName} sub Tenant");
                    outputFile.WriteLine($"    friendlyName: {SubTenantName}");
                    outputFile.WriteLine($"    type: Tenant");
                    outputFile.WriteLine($"    types:");
                    outputFile.WriteLine($"    - name: Classic");
                    outputFile.WriteLine($"      category: SensorType");
                    outputFile.WriteLine($"    - name: HotelBrand");
                    outputFile.WriteLine($"      category: SpaceType");
                    outputFile.WriteLine($"    - name: Hotel");
                    outputFile.WriteLine($"      category: SpaceType");
                    outputFile.WriteLine($"    - name: VIPFloor");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: QueenRoom");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: KingRoom");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: Suite");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: VIPSuite");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: Ballroom");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    - name: Gym");
                    outputFile.WriteLine($"      category: SpaceSubtype");
                    outputFile.WriteLine($"    users:");
                    outputFile.WriteLine($"    - Manager");
                    outputFile.WriteLine($"    spaceReferences:");

                    subTenantExtraSpacing = "  ";
                }

                foreach (Brand brand in brands)
                {
                    string brandFilename = GetBrandProvisioningFilename(brand);

                    outputFile.WriteLine($"{subTenantExtraSpacing}  - filename: {brandFilename}");
                }
            }

            Console.WriteLine($"Successfully created site provisioning file: {siteFilename}");

            return true;
        }

        private string GetBrandProvisioningFilename(Brand brand)
        {
            return $"{OutputFilePrefix}_{brand.Name}_Provisioning.yaml";
        }

        private bool GenerateBrandProvisioningFile(Brand brand, List<HotelType> hotelTypes)
        {
            string brandFilename = GetBrandProvisioningFilename(brand);

            if (File.Exists(brandFilename))
            {
                Console.WriteLine($"Error: A file with the requested output name already exists: {brandFilename}");
                return false;
            }

            using (StreamWriter outputFile = new StreamWriter(brandFilename))
            {
                var hotelNames = new List<string>();

                outputFile.WriteLine($"name: {brand.Name}");
                outputFile.WriteLine($"description: SmartHotel360 {brand.Name}");
                outputFile.WriteLine($"friendlyName: {brand.Name}");
                outputFile.WriteLine($"type: HotelBrand");
                outputFile.WriteLine($"users:");
                outputFile.WriteLine($"- {brand.Name} Manager");
                outputFile.WriteLine($"spaces:");

                // Create the hotels
                foreach (Hotel hotel in brand.Hotels)
                {
                    string brandHotelPrefix = $"{brand.Name}_{hotel.Name}_";

                    outputFile.WriteLine($"- name: {hotel.Name}");
                    outputFile.WriteLine($"  description: SmartHotel360 hotel {hotel.Name}");
                    outputFile.WriteLine($"  friendlyName: {hotel.Name}");
                    outputFile.WriteLine($"  type: Hotel");
                    outputFile.WriteLine($"  users:");
                    outputFile.WriteLine($"  - {hotel.Name} Manager");
                    outputFile.WriteLine($"  spaces:");

                    var hotelType = hotelTypes.FirstOrDefault(t => t.Name == hotel.Type);

                    int numberRegularFloors = hotelType.TotalNumberFloors - hotelType.NumberVipFloors;

                    // Create the regular floors
                    for (int i = 0; i < numberRegularFloors; i++)
                    {
                        outputFile.WriteLine($"  - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"    description: Floor {i + 1}");
                        outputFile.WriteLine($"    friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"    type: Floor");

                        if (!String.IsNullOrEmpty(hotel.RegularFloorEmployeeUser))
                        {
                            outputFile.WriteLine($"    users:");
                            outputFile.WriteLine($"    - {hotel.Name} {hotel.RegularFloorEmployeeUser}");
                        }

                        outputFile.WriteLine($"    spaces:");

                        // Create the rooms
                        for (int j = 0; j < hotelType.NumberRoomsPerRegularFloor; j++)
                        {
                            string roomType = GetRoomType(j, hotelType.NumberRoomsPerRegularFloor, false);
                            CreateRoom(outputFile, 100 * (i + 1) + j + 1, brandHotelPrefix, roomType, hotel.AddDevices);
                        }

                        if (i == 0 && hotelType.IncludeGym)
                        {
                            CreateRoom(outputFile, 100 * (i + 1) + hotelType.NumberRoomsPerRegularFloor + 1,
                                brandHotelPrefix, "Gym", hotel.AddDevices);
                        }

                        if (i == 1 && hotelType.IncludeBallroom)
                        {
                            CreateRoom(outputFile, 100 * (i + 1) + hotelType.NumberRoomsPerRegularFloor + 1,
                                brandHotelPrefix, "Ballroom", hotel.AddDevices);
                        }
                    }

                    // Create the VIP floors
                    for (int i = numberRegularFloors; i < hotelType.TotalNumberFloors; i++)
                    {
                        outputFile.WriteLine($"  - name: VIPFloor {i + 1:D02}");
                        outputFile.WriteLine($"    description: VIPFloor {i + 1}");
                        outputFile.WriteLine($"    friendlyName: VIPFloor {i + 1}");
                        outputFile.WriteLine($"    type: VIPFloor");
                        outputFile.WriteLine($"    spaces:");

                        // Create the rooms
                        for (int j = 0; j < hotelType.NumberRoomsPerVipFloor; j++)
                        {
                            string roomType = GetRoomType(j, hotelType.NumberRoomsPerRegularFloor, true);
                            CreateRoom(outputFile, 100 * (i + 1) + j + 1, brandHotelPrefix, roomType, hotel.AddDevices);
                        }
                    }
                }
            }

            Console.WriteLine($"Successfully created brand provisioning file: {brandFilename}");

            return true;
        }

        private string GetRoomType(int roomIndex, int numberRoomsOnFloor, bool isVipFloor)
        {
            if (isVipFloor)
            {
                int oneQuarter = (int)Math.Ceiling((double)numberRoomsOnFloor / 4.0D);
                if (roomIndex < oneQuarter)
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
                int oneThird = (int)Math.Ceiling((double)numberRoomsOnFloor / 3.0D);
                if (roomIndex < oneThird)
                {
                    return "QueenRoom";
                }
                else if (roomIndex < 2 * oneThird)
                {
                    return "KingRoom";
                }
                else
                {
                    return "Suite";
                }
            }
        }

        private void CreateRoom(StreamWriter outputFile, int roomNumber, string brandHotelPrefix, string roomType, bool addDevices)
        {
            outputFile.WriteLine($"    - name: {roomType} {roomNumber}");
            outputFile.WriteLine($"      description: {roomType} {roomNumber}");
            outputFile.WriteLine($"      friendlyName: {roomType} {roomNumber}");
            outputFile.WriteLine($"      type: {roomType}");

            if (addDevices)
            {
                outputFile.WriteLine($"      devices:");
                outputFile.WriteLine($"      - name: Thermostat");
                outputFile.WriteLine($"        hardwareId: {brandHotelPrefix}{roomNumber}_T");
                outputFile.WriteLine($"        sensors:");
                outputFile.WriteLine($"        - dataType: Temperature");
                outputFile.WriteLine($"          type: Classic");
                outputFile.WriteLine($"      - name: Motion");
                outputFile.WriteLine($"        hardwareId: {brandHotelPrefix}{roomNumber}_M");
                outputFile.WriteLine($"        sensors:");
                outputFile.WriteLine($"        - dataType: Motion");
                outputFile.WriteLine($"          type: Classic");
                outputFile.WriteLine($"      - name: Light");
                outputFile.WriteLine($"        hardwareId: {brandHotelPrefix}{roomNumber}_L");
                outputFile.WriteLine($"        sensors:");
                outputFile.WriteLine($"        - dataType: Light");
                outputFile.WriteLine($"          type: Classic");
            }
        }

        private void GenerateSampleDefinition(string definitionFilename)
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

            for (int i = 0; i < 4; i++)
            {
                var hotels = new List<Hotel>();

                switch (i)
                {
                    case 0:
                        {
                            hotels.Add(CreateHotel(hotelTypeH, 1, "Employee", true));
                            hotels.Add(CreateHotel(hotelTypeL, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSH, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSH, 2, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 2, null, AllDevices));
                            break;
                        }
                    case 1:
                        {
                            hotels.Add(CreateHotel(hotelTypeSH, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSH, 2, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 1, null, AllDevices));
                            break;
                        }
                    case 2:
                        {
                            hotels.Add(CreateHotel(hotelTypeSH, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 2, null, AllDevices));
                            break;
                        }
                    case 3:
                        {
                            hotels.Add(CreateHotel(hotelTypeSH, 1, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSH, 2, null, AllDevices));
                            hotels.Add(CreateHotel(hotelTypeSL, 1, null, AllDevices));
                            break;
                        }
                }

                var brand = new Brand
                {
                    Name = $"Brand {i + 1}",
                    Hotels = hotels
                };

                brands.Add(brand);
            }

            var site = new Site
            {
                HotelTypes = hotelTypes,
                Brands = brands
            };

            string siteJson = JsonConvert.SerializeObject(site, Formatting.Indented);
            using (StreamWriter definitionFile = new StreamWriter(definitionFilename))
            {
                definitionFile.Write(siteJson);
            }
        }

        private Hotel CreateHotel(HotelType hotelType, int hotelIndex, string regularFloorEmployeeUser, bool addDevices)
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