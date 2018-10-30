using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
	[DataContract]
    public class Space
    {
	    public Space()
	    {
		    ChildSpaces = new List<Space>();
	    }

	    [DataMember]
	    public string Id { get; set; }
	    [DataMember]
	    public string ParentSpaceId { get; set; }
	    [DataMember]
	    public string Name { get; set; }
	    [DataMember]
	    public string Type { get; set; }
	    [DataMember]
	    public int TypeId { get; set; }
	    [DataMember]
	    public List<Space> ChildSpaces { get; set; }
	    [DataMember]
	    public List<Device> Devices { get; set; }
    }
}
