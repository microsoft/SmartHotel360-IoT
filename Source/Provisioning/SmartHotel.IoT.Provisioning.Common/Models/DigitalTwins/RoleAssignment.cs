namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
	public class RoleAssignment
	{
		public string RoleId { get; set; }
		public string ObjectId { get; set; }
		public string ObjectIdType { get; set; }
		public string Path { get; set; }
		public string TenantId { get; set; }

		public class RoleIds
		{
			public const string SpaceAdmin = "98e44ad7-28d4-4007-853b-b9968ad132d1";
			public const string User = "b1ffdb77-c635-4e7e-ad25-948237d85b30";
		}

		public class ObjectIdTypes
		{
			public const string UserId = "UserId";
			public const string UserDefinedFunctionId = "UserDefinedFunctionId";
		}
	}
}
