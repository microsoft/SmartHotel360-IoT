using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class UserDefinedFunctionDescription
    {
	    public string name { get; set; }
	    public IList<string> matcherNames { get; set; }
	    public string script { get; set; }
    }
}
