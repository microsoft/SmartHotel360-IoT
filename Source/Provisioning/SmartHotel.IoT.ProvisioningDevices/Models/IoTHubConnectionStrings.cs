using System;
using System.Collections.Generic;

namespace SmartHotel.IoT.ProvisioningDevices.Models
{
	public class IoTHubConnectionStrings : Dictionary<string, string>
	{
		public IoTHubConnectionStrings()
			: base( StringComparer.OrdinalIgnoreCase )
		{ }
	}
}
