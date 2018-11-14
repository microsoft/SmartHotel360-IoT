namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class UserDefinedFunctionWithMatchers
    {
	    public string Id { get; set; }
	    // These are the Ids of the matchers
	    public Matcher[] Matchers { get; set; }
	    public string Name { get; set; }
	    public string SpaceId { get; set; }
    }
}
