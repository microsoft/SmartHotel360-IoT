using System;
using System.Collections.Generic;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class SpaceDescription
    {
		public SpaceDescription()
		{
			spaceReferences = new List<SpaceReferenceDescription>();
			spaces = new List<SpaceDescription>();
			devices = new List<DeviceDescription>();
			resources = new List<ResourceDescription>();
			types = new List<TypeDescription>();
			users = new List<string>();
		}

		public string name { get; set; }
		public string description { get; set; }
		public string friendlyName { get; set; }
		public string type { get; set; }
		public string subType { get; set; }
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
