using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class ProvisioningDescription
    {
		public IList<EndpointDescription> endpoints { get; set; }
		public IList<SpaceDescription> spaces { get; set; }
	}
}
