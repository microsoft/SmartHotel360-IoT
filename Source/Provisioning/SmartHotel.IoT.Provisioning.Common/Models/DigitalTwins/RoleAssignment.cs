namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
    public class RoleAssignment
    {
	    public const string UserRoleId = "b1ffdb77-c635-4e7e-ad25-948237d85b30";
		public string RoleId { get; set; }
		public string ObjectId { get; set; }
	    public string ObjectIdType => "UserId";
		public string Path { get; set; }
		public string TenantId { get; set; }
	}
}
