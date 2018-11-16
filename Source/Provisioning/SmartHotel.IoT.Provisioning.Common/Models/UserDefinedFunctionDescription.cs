using System;
using System.Collections.Generic;
using System.Linq;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class UserDefinedFunctionDescription
    {
		public string name { get; set; }
	    public IList<string> matcherNames { get; set; }
	    public string script { get; set; }

	    public UserDefinedFunction ToDigitalTwins(Guid spaceId, ICollection<string> matcherIds, string id = null)
	    {
		    return new UserDefinedFunction
		    {
			    Id = id,
			    Name = name,
			    SpaceId = spaceId.ToString(),
			    Matchers = matcherIds.ToArray()
		    };
	    }
    }
}
