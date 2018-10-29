using System;
using System.Collections.Generic;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class SpaceDescription
    {
		public string name { get; set; }
		public string description { get; set; }
		public string friendlyName { get; set; }
		public string type { get; set; }
	    public string keystoreName { get; set; }
        public IList<SpaceReferenceDescription> spaceReferences { get; set; }
        public IList<SpaceDescription> spaces { get; set; }
		public IList<DeviceDescription> devices { get; set; }
		public IList<ResourceDescription> resources { get; set; }
		public IList<TypeDescription> types { get; set; }
		public IList<string> users { get; set; }

		public Space ToDigitalTwins(Guid parentId)
	    {
		    return new Space
		    {
			    Name = name,
			    Description = description,
			    FriendlyName = friendlyName,
			    Type = type,
				ParentSpaceId = parentId != Guid.Empty ? parentId.ToString() : string.Empty
		    };
	    }
	}
}
