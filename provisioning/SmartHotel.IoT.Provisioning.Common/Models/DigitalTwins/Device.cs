namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class Device
    {
		public bool CreateIoTHubDevice => true;
		public string Id { get; set; }
	    public string Name { get; set; }
	    public string HardwareId { get; set; }
	    public string SpaceId { get; set; }
	    public string Status => "Active";
		public Sensor[] Sensors { get; set; }
	}
}
