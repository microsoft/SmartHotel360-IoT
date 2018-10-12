using System;
using Type = SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins.Type;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class TypeDescription
    {
		public string name { get; set; }
		public string category { get; set; }

	    public Type ToDigitalTwins(Guid spaceId)
	    {
		    return new Type
		    {
			    Name = name,
				Category = category,
				SpaceId = spaceId.ToString()
		    };
	    }
	}
}