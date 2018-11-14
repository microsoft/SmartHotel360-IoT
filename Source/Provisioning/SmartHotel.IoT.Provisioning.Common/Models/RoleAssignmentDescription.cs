using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class RoleAssignmentDescription
	{
		public string objectIdType { get; set; }
		public string objectName { get; set; }
		public string roleId { get; set; }

		public RoleAssignment ToDigitalTwins( string objectId, string adTenantId, string spacePath )
		{
			return new RoleAssignment
			{
				RoleId = roleId,
				ObjectId = objectId,
				ObjectIdType = objectIdType,
				TenantId = adTenantId,
				Path = spacePath
			};
		}
	}
}
