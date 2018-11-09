using System;

namespace SmartHotel.Devices.Light
{
	public class ManagementApiLimitReachedException : Exception
	{
		public ManagementApiLimitReachedException( string message )
		: base( message )
		{
		}
	}
}
