namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class UserDefinedFunction
    {
	    public string Id { get; set; }
		// These are the Ids of the matchers
	    public string[] Matchers { get; set; }
	    public string Name { get; set; }
	    public string SpaceId { get; set; }
    }
}
