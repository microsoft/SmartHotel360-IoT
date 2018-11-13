using System;

namespace SmartHotel.IoT.IoTHubDeviceProvisioning
{
	public class ThrottlingBacklogTimeoutException : Exception
	{
		public ThrottlingBacklogTimeoutException( string message )
		: base( message )
		{

		}
	}
}
