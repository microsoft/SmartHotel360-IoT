using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
	public class Space
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string FriendlyName { get; set; }
		public string Type { get; set; }
		public int TypeId { get; set; }
		public string ParentSpaceId { get; set; }
		public string Subtype { get; set; }
		public int SubtypeId { get; set; }
		public string Status { get; set; }
		public int StatusId { get; set; }
		public string[] SpacePaths { get; set; }

		public List<Space> Children { get; set; }
		public Property[] Properties { get; set; }
	}
}
