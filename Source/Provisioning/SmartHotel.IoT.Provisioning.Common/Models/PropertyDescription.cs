using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class PropertyDescription
    {
		public string name { get; set; }
		public string value { get; set; }

	    public Property ToDigitalTwins()
	    {
		    return new Property
		    {
			    Name = name,
			    Value = value
		    };
	    }
	}
}
