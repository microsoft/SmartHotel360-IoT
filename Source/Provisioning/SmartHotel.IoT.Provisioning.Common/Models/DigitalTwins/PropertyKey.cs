namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class PropertyKey
    {
	    public string Name { get; set; }
	    public string PrimitiveDataType { get; set; }
	    public string Description { get; set; }
	    public string SpaceId { get; set; }
	    public string Scope => "Spaces";
	    public string ValidationData { get; set; }
	    public string Min { get; set; }
	    public string Max { get; set; }
    }
}
