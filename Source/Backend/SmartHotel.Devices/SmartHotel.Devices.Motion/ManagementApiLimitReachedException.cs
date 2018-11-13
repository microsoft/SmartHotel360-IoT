using System;

namespace SmartHotel.Devices.Motion
{
	public class ManagementApiLimitReachedException : Exception
	{
		public ManagementApiLimitReachedException( string message )
		: base( message )
		{
		}
	}
}
