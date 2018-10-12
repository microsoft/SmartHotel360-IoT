using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class SensorDescription
    {
		public string dataType { get; set; }
		public string type { get; set; }

	    public Sensor ToDigitalTwins()
	    {
		    return new Sensor
		    {
			    DataType = dataType,
			    Type = type
		    };
	    }
	}
}
