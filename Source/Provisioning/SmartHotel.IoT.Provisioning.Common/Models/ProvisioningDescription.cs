using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class ProvisioningDescription
    {
		public ProvisioningDescription()
		{
			endpoints = new List<EndpointDescription>();
			spaces = new List<SpaceDescription>();
		}

		public IList<EndpointDescription> endpoints { get; set; }
		public IList<SpaceDescription> spaces { get; set; }
	}
}
