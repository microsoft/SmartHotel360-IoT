using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
	[DataContract]
    public class OccupiedTelemetryMessage : TelemetryMessage
    {
		public OccupiedTelemetryMessage(bool value)
		{
			Occupied = value ? 1 : 0;
			SensorReading = value.ToString();
		}

		[DataMember]
		public int Occupied { get; set; }
	}
}
