namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class RoleAssignment
    {
		public string RoleId { get; set; }
		public string ObjectId { get; set; }
	    public string ObjectIdType => "UserId";
		public string Path { get; set; }
		public string TenantId { get; set; }
	}
}
