using System;

namespace SmartHotel.Devices.RoomDevice
{
	public class ManagementApiLimitReachedException : Exception
	{
		public ManagementApiLimitReachedException( string message )
			: base( message )
		{
		}
	}
}
