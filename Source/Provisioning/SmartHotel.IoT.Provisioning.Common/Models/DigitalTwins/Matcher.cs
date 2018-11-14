namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class Matcher
    {
		public string Id { get; set; }
		public string Name { get; set; }
		public string SpaceId { get; set; }
		public Condition[] Conditions { get; set; }
	}
}
