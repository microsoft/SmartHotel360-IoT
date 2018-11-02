using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class ProvisioningDescription
    {
		public IList<EndpointDescription> endpoints { get; set; }
		public IList<SpaceDescription> spaces { get; set; }

	    public void AddEndpoint(EndpointDescription endpointDescription)
	    {
		    if (endpoints == null)
		    {
			    endpoints = new List<EndpointDescription>();
		    }

		    endpoints.Add(endpointDescription);
	    }

	    public void AddSpace(SpaceDescription spaceDescription)
	    {
		    if (spaces == null)
		    {
			    spaces = new List<SpaceDescription>();
		    }

		    spaces.Add(spaceDescription);
	    }
	}
}
