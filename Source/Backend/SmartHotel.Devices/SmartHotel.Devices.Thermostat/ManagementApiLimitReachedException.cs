using System;

namespace SmartHotel.Devices.Thermostat
{
	public class ManagementApiLimitReachedException : Exception
	{
		public ManagementApiLimitReachedException( string message )
		: base( message )
		{
		}
	}
}
