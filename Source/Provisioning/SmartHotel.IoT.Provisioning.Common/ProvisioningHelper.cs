using System;
using System.Collections.Generic;
using System.IO;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common
{
    public static class ProvisioningHelper
    {
	    public static ProvisioningDescription LoadSmartHotelProvisioning()
	    {
		    var yamlDeserializer = new Deserializer();
		    string content = File.ReadAllText( Path.Combine( "Resources", "SmartHotel_Site_Provisioning.yaml" ) );
		    var provisioningDescription = yamlDeserializer.Deserialize<ProvisioningDescription>( content );

            foreach (SpaceDescription tenantDescription in provisioningDescription.spaces)
            {
                LoadBrandProvisionings(tenantDescription);
            }

            return provisioningDescription;
	    }

        public static void LoadBrandProvisionings(SpaceDescription tenantDescription)
        {
            var brandSpaces = new List<SpaceDescription>();

            foreach (SpaceReferenceDescription spaceReferenceDescription in tenantDescription.spaceReferences)
            {
                if (String.IsNullOrEmpty(spaceReferenceDescription.filename))
                {
                    throw new Exception($"Brand filename expected.");
                }

                var brandSpace = LoadBrandProvisioning(spaceReferenceDescription.filename);

                brandSpaces.Add(brandSpace);
            }

            tenantDescription.spaces = brandSpaces;
        }

        public static SpaceDescription LoadBrandProvisioning(string brandFilename)
        {
            var yamlDeserializer = new Deserializer();
            string content = File.ReadAllText(Path.Combine("Resources", brandFilename));
            return yamlDeserializer.Deserialize<SpaceDescription>(content);
        }
    }
}
