using McMaster.Extensions.CommandLineUtils;
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

        [Option("-b|--brand", Description = "Brand name")]
        [Required]
        public string BrandName { get; }

        [Option("-h|--numberH", Description = "Number of H hotels")]
        [Required]
        public int NumberHHotels { get; }

        [Option("-l|--numberL", Description = "Number of L hotels")]
        [Required]
        public int NumberLHotels { get; }

        [Option("-hf|--numberHFloors", Description = "Total number of floors in each H hotels")]
        [Required]
        public int NumberHFloors { get; }

        [Option("-hvf|--numberHVipFloors", Description = "Number of VIP floors in each H hotels")]
        [Required]
        public int NumberHVipFloors { get; }

        [Option("-lf|--numberLFloors", Description = "Total number of floors in each L hotels")]
        [Required]
        public int NumberLFloors { get; }

        [Option("-lvf|--numberLVipFloors", Description = "Number of VIP floors in each L hotels")]
        [Required]
        public int NumberLVipFloors { get; }

        [Option("-hr|--numberHRooms", Description = "Number of rooms per regular floor in H hotels")]
        [Required]
        public int NumberHRooms { get; }

        [Option("-hvr|--numberHVipRooms", Description = "Number of rooms per VIP floor in H hotels")]
        [Required]
        public int NumberHVipRooms { get; }

        [Option("-lr|--numberLRooms", Description = "Number of rooms per regular floor in L hotels")]
        [Required]
        public int NumberLRooms { get; }

        [Option("-lvr|--numberLVipRooms", Description = "Number of rooms per VIP floor in L hotels")]
        [Required]
        public int NumberLVipRooms { get; }

        [Option("-o|--outputFileName", Description = "Output file name")]
        public string OutputFileName { get; set; }

        [Option("-nf|--namesFilename", Description = "Filename of hotel names")]
        public string HotelNamesFilename { get; }

        private async Task OnExecuteAsync()
        {
            if (String.IsNullOrEmpty(OutputFileName))
            {
                OutputFileName = $"SmartHotelBrand{BrandName}Provisioning.yaml";
            }

            if (File.Exists(OutputFileName))
            {
                Console.WriteLine($"A file with the requested output name already exists: {OutputFileName}");
                return;
            }

            using (StreamWriter outputFile = new StreamWriter(OutputFileName))
            {
                var hotelNames = new List<string>();
                if (!String.IsNullOrEmpty(HotelNamesFilename))
                {
                    if (File.Exists(HotelNamesFilename))
                    {
                        using (StreamReader hotelNamesFile = new StreamReader(HotelNamesFilename))
                        {
                            string hotelName;
                            while ((hotelName = hotelNamesFile.ReadLine()) != null)
                            {
                                hotelNames.Add(hotelName);
                            }
                        }
                    }
                }

                var hotelHNames = new List<string>();
                var hotelLNames = new List<string>();

                for (int i = 0; i < NumberHHotels; i++)
                {
                    if (i < hotelNames.Count)
                    {
                        hotelHNames.Add(hotelNames[i]);
                    }
                    else
                    {
                        string hotelName = Prompt.GetString($"H Hotel {i + 1} name: ");
                        hotelHNames.Add(hotelName);
                    }
                }

                Console.WriteLine();

                for (int i = 0; i < NumberLHotels; i++)
                {
                    int nameIndex = NumberHHotels + i;
                    if (nameIndex < hotelNames.Count)
                    {
                        hotelLNames.Add(hotelNames[nameIndex]);
                    }
                    else
                    {
                        string hotelName = Prompt.GetString($"L Hotel {i + 1} name: ");
                        hotelLNames.Add(hotelName);
                    }
                }

                outputFile.WriteLine("spaces:");
                outputFile.WriteLine($"- name: {BrandName}");
                outputFile.WriteLine($"  description: SmartHotel360 {BrandName}");
                outputFile.WriteLine($"  friendlyName: {BrandName}");
                outputFile.WriteLine($"  type: Brand");
                outputFile.WriteLine($"  users:");
                outputFile.WriteLine($"  - {BrandName} Manager");
                outputFile.WriteLine($"  spaces:");

                foreach (string hotelName in hotelHNames)
                {
                    outputFile.WriteLine($"  - name: {hotelName}");
                    outputFile.WriteLine($"    description: SmartHotel360 hotel {hotelName}");
                    outputFile.WriteLine($"    friendlyName: {hotelName}");
                    outputFile.WriteLine($"    type: Venue");
                    outputFile.WriteLine($"    users:");
                    outputFile.WriteLine($"    - {hotelName} Manager");
                    outputFile.WriteLine($"    spaces:");

                    for (int i = 0; i < NumberHFloors - NumberHVipFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");
                        outputFile.WriteLine($"      users:");
                        outputFile.WriteLine($"      - {hotelName} Employee");
                        outputFile.WriteLine($"      spaces:");

                        for (int j = 0; j < NumberHRooms; j++)
                        {
                            outputFile.WriteLine($"      - name: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        description: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        friendlyName: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        type: Room");
                        }
                    }

                    for (int i = NumberHFloors - NumberHVipFloors; i < NumberHFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");
                        outputFile.WriteLine($"      spaces:");

                        for (int j = 0; j < NumberHVipRooms; j++)
                        {
                            outputFile.WriteLine($"      - name: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        description: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        friendlyName: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        type: Room");
                        }
                    }
                }

                foreach (string hotelName in hotelLNames)
                {
                    outputFile.WriteLine($"  - name: {hotelName}");
                    outputFile.WriteLine($"    description: SmartHotel360 hotel {hotelName}");
                    outputFile.WriteLine($"    friendlyName: {hotelName}");
                    outputFile.WriteLine($"    type: Venue");
                    outputFile.WriteLine($"    users:");
                    outputFile.WriteLine($"    - {hotelName} Manager");
                    outputFile.WriteLine($"    spaces:");

                    for (int i = 0; i < NumberLFloors - NumberLVipFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");
                        outputFile.WriteLine($"      users:");
                        outputFile.WriteLine($"      - {hotelName} Employee");
                        outputFile.WriteLine($"      spaces:");

                        for (int j = 0; j < NumberLRooms; j++)
                        {
                            outputFile.WriteLine($"      - name: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        description: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        friendlyName: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        type: Room");
                        }
                    }

                    for (int i = NumberLFloors - NumberLVipFloors; i < NumberLFloors; i++)
                    {
                        outputFile.WriteLine($"    - name: Floor {i + 1:D02}");
                        outputFile.WriteLine($"      description: Floor {i + 1}");
                        outputFile.WriteLine($"      friendlyName: Floor {i + 1}");
                        outputFile.WriteLine($"      type: Floor");
                        outputFile.WriteLine($"      spaces:");

                        for (int j = 0; j < NumberLVipRooms; j++)
                        {
                            outputFile.WriteLine($"      - name: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        description: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        friendlyName: Room {100 * (i + 1) + j + 1}");
                            outputFile.WriteLine($"        type: Room");
                        }
                    }
                }
            }

            Console.WriteLine($"Successfully created provisioning file: {OutputFileName}");
        }
    }
}
