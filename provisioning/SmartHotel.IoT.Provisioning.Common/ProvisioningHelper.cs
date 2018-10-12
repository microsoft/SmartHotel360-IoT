using System.IO;
using SmartHotel.IoT.Provisioning.Common.Models;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common
{
    public static class ProvisioningHelper
    {
	    public static ProvisioningDescription LoadSmartHotelProvisioning()
	    {
		    var yamlDeserializer = new Deserializer();
		    string content = File.ReadAllText( Path.Combine( "Resources", "SmartHotelProvisioning.yaml" ) );
		    return yamlDeserializer.Deserialize<ProvisioningDescription>( content );
	    }
    }
}
