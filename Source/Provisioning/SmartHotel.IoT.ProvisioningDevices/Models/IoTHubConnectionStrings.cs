using System;
using System.Collections.Generic;

namespace SmartHotel.IoT.ProvisioningDevices.Models
{
	public class IoTHubConnectionStrings : Dictionary<string, CaseInsensitiveDictionary>
	{
		public IoTHubConnectionStrings()
			: base( StringComparer.OrdinalIgnoreCase )
		{ }
	}
}
