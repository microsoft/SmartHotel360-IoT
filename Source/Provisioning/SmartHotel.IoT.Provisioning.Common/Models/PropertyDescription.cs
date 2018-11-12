using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class PropertyDescription
    {
	    private const string StringPrimitiveDataType = "string";
	    private const string IntPrimitiveDataType = "int";
	    private const string UIntPrimitiveDataType = "uint";
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
