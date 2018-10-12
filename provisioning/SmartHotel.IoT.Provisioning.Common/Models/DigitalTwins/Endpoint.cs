namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class Endpoint
    {
		public string Id { get; set; }
		public string Type { get; set; }
		public string[] EventTypes { get; set; }
		public string ConnectionString { get; set; }
		public string SecondaryConnectionString { get; set; }
		public string Path { get; set; }
	}
}
