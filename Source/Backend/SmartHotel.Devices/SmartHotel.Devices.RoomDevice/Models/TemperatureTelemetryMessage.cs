using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
	[DataContract]
    public class TemperatureTelemetryMessage : TelemetryMessage
    {
        
	    public TemperatureTelemetryMessage(double value)
	    {
		    Temperature = value;
		    SensorReading = value.ToString();
	    }
		[DataMember]
	    public double Temperature { get; set; }
    }
}
