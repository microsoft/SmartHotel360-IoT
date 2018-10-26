using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningBrandGenerator
{
    class Program
    {
        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

        [Option("-op|--outputPrefix", Description = "Prefix of the output filenames")]
        //[Required]
        public string OutputFilePrefix { get; private set; }

        [Option("-df|--definitionFilename", Description = "Filename of json brand definition")]
        //[Required]
        public string DefinitionFilename { get; }

        private async Task OnExecuteAsync()
        {
            //GenerateSampleDefinition("SampleSiteDefinition.json");

            GenerateProvisioningFiles();
        }

        private void GenerateProvisioningFiles()
        {
            if (!File.Exists(DefinitionFilename))
            {
                Console.WriteLine($"Definition file not found: {DefinitionFilename}");
                return;
            }

            using (StreamReader definitionFile = new StreamReader(DefinitionFilename))
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
                var hotelNames = new List<string>();

                outputFile.WriteLine("endpoints:");
                outputFile.WriteLine("- type: EventHub");
                outputFile.WriteLine("  eventTypes:");
                outputFile.WriteLine("   - DeviceMessage");

                //TODO: Finish
                //outputFile.WriteLine($"  type: Brand");
                //outputFile.WriteLine($"  users:");
                //outputFile.WriteLine($"  - {brand.Name} Manager");
                //outputFile.WriteLine($"  spaces:");
            }

            return true;
        }

        private bool GenerateBrandProvisioningFile(Brand brand, List<HotelType> hotelTypes)
        {
            string brandFilename = $"{OutputFilePrefix}_{brand.Name}_Provisioning.yaml";

            if (File.Exists(brandFilename))
            {
                Console.WriteLine($"Error: A file with the requested output name already exists: {brandFilename}");
                return false;
            }

            using (StreamWriter outputFile = new StreamWriter(brandFilename))
            {
                var hotelNames = new List<string>();

                outputFile.WriteLine("spaces:");
                outputFile.WriteLine($"- name: {brand.Name}");
                outputFile.WriteLine($"  description: SmartHotel360 {brand.Name}");
                outputFile.WriteLine($"  friendlyName: {brand.Name}");
                outputFile.WriteLine($"  type: Brand");
                outputFile.WriteLine($"  users:");
                outputFile.WriteLine($"  - {brand.Name} Manager");
                outputFile.WriteLine($"  spaces:");

                // Create the hotels
                foreach (Hotel hotel in brand.Hotels)
                {
                    string brandHotelPrefix = $"{brand.Name}_{hotel.Name}_";

                    outputFile.WriteLine($"  - name: {hotel.Name}");
                    outputFile.WriteLine($"    description: SmartHotel360 hotel {hotel.Name}");
                    outputFile.WriteLine($"    friendlyName: {hotel.Name}");
                    outputFile.WriteLine($"    type: Venue");
                    outputFile.WriteLine($"    users:");
                    outputFile.WriteLine($"    - {hotel.Name} Manager");
                    outputFile.WriteLine($"    spaces:");

                    var hotelType = hotelTypes.FirstOrDefault(t => t.Name == hotel.Type);

                    int numberRegularFloors = hotelType.TotalNumberFloors - hotelType.NumberVipFloors;

                    // Create the regular floors
                    for (int i = 0; i < numberRegularFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");

                        if (!String.IsNullOrEmpty(hotel.RegularFloorEmployeeUser))
                        {
                            outputFile.WriteLine($"      users:");
                            outputFile.WriteLine($"      - {hotel.Name} {hotel.RegularFloorEmployeeUser}");
                        }

                        outputFile.WriteLine($"      spaces:");

                        // Create the rooms
                        for (int j = 0; j < hotelType.NumberRoomsPerRegularFloor; j++)
                        {
                            CreateRoom(outputFile, 100 * (i + 1) + j + 1, brandHotelPrefix, hotel.AddDevices);
                        }
                    }

                    // Create the VIP floors
                    for (int i = numberRegularFloors; i < hotelType.TotalNumberFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");
                        outputFile.WriteLine($"      spaces:");

                        // Create the rooms
                        for (int j = 0; j < hotelType.NumberRoomsPerVipFloor; j++)
                        {
                            CreateRoom(outputFile, 100 * (i + 1) + j + 1, brandHotelPrefix, hotel.AddDevices);
                        }
                    }
                }
            }

            Console.WriteLine($"Successfully created brand provisioning file: {brandFilename}");

            return true;
        }

        private void CreateRoom(StreamWriter outputFile, int roomNumber, string brandHotelPrefix, bool addDevices)
        {
            outputFile.WriteLine($"      - name: Room {roomNumber}");
            outputFile.WriteLine($"        description: Room {roomNumber}");
            outputFile.WriteLine($"        friendlyName: Room {roomNumber}");
            outputFile.WriteLine($"        type: Room");

            if (addDevices)
            {
                outputFile.WriteLine($"        devices:");
                outputFile.WriteLine($"        - name: Thermostat");
                outputFile.WriteLine($"          hardwareId: {brandHotelPrefix}{roomNumber}_T");
                outputFile.WriteLine($"          sensors:");
                outputFile.WriteLine($"          - dataType: Temperature");
                outputFile.WriteLine($"            type: Classic");
                outputFile.WriteLine($"        - name: Motion");
                outputFile.WriteLine($"          hardwareId: {brandHotelPrefix}{roomNumber}_M");
                outputFile.WriteLine($"          sensors:");
                outputFile.WriteLine($"          - dataType: Motion");
                outputFile.WriteLine($"            type: Classic");
                outputFile.WriteLine($"        - name: Light");
                outputFile.WriteLine($"          hardwareId: {brandHotelPrefix}{roomNumber}_L");
                outputFile.WriteLine($"          sensors:");
                outputFile.WriteLine($"          - dataType: Light");
                outputFile.WriteLine($"            type: Classic");
            }
        }

        private void GenerateSampleDefinition(string definitionFilename)
        {
            var hotelTypeH = new HotelType
            {
                Name = "H",
                TotalNumberFloors = 40,
                NumberVipFloors = 10,
                NumberRoomsPerRegularFloor = 73,
                NumberRoomsPerVipFloor = 19
            };

            var hotelTypeL = new HotelType
            {
                Name = "L",
                TotalNumberFloors = 25,
                NumberVipFloors = 5,
                NumberRoomsPerRegularFloor = 18,
                NumberRoomsPerVipFloor = 8
            };

            var hotelTypes = new List<HotelType> { hotelTypeH, hotelTypeL };

            var brands = new List<Brand>();

            for (int i = 0; i < 4; i++)
            {
                var hotels = new List<Hotel>();

                for (int j = 0; j < 5; j++)
                {
                    var hotel = new Hotel
                    {
                        Name = $"Hotel H {j + 1}",
                        Type = hotelTypeH.Name,
                        RegularFloorEmployeeUser = (i == 0 && j == 0) ? "Employee" : null,
                        AddDevices = j == 0
                    };

                    hotels.Add(hotel);
                }

                for (int j = 0; j < 5; j++)
                {
                    var hotel = new Hotel
                    {
                        Name = $"Hotel L {j + 1}",
                        Type = hotelTypeL.Name,
                        RegularFloorEmployeeUser = null,
                        AddDevices = false
                    };

                    hotels.Add(hotel);
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
    }
}
