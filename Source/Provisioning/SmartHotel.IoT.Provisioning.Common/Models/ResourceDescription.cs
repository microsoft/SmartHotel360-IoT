using System;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class ResourceDescription
    {
		public string type { get; set; }

	    public Resource ToDigitalTwins(Guid spaceId)
	    {
		    return new Resource
		    {
			    Type = type,
				SpaceId = spaceId.ToString()
		    };
	    }
	}
}
