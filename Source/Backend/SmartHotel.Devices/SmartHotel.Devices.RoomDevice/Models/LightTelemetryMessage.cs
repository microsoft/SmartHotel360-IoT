using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
	[DataContract]
	public class LightTelemetryMessage : TelemetryMessage
	{
		public LightTelemetryMessage(double value)
		{
			Light = value;
			SensorReading = value.ToString();
		}
		[DataMember]
		public double Light { get; set; }
	}
}
