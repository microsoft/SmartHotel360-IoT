using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
	[DataContract]
	public class Space
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string ParentSpaceId { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string FriendlyName { get; set; }
		[DataMember]
		public string Type { get; set; }
		[DataMember]
		public int TypeId { get; set; }
		
		[DataMember]
		public string Subtype { get; set; }
		[DataMember]
		public int SubtypeId { get; set; }
		[DataMember]
		public List<Property> Properties { get; set; }
		[DataMember]
		public List<Space> ChildSpaces { get; set; }
		[DataMember]
		public List<Value> Values { get; set; }
	}
}
