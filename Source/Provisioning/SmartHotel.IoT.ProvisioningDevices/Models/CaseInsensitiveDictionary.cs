using System;
using System.Collections.Generic;

namespace SmartHotel.IoT.ProvisioningDevices.Models
{
    public class CaseInsensitiveDictionary : Dictionary<string, string>
    {
	    public CaseInsensitiveDictionary()
		    : base( StringComparer.OrdinalIgnoreCase )
	    { }
    }
}
