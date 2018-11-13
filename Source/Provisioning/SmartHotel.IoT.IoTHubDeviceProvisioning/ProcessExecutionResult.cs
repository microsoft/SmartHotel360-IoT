using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHotel.IoT.IoTHubDeviceProvisioning
{
	public struct ProcessExecutionResult
	{
		public string Output { get; set; }
		public string Error { get; set; }
		public int ExitCode { get; set; }
		public bool HasError => ExitCode != 0;
	}
}
