using System;
using System.Collections.Generic;
using System.Linq;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class DeviceDescription
	{
		public string name { get; set; }
		public string hardwareId { get; set; }
		public IList<SensorDescription> sensors { get; set; }
		public string SasToken { get; set; }
		public Guid SpaceId { get; set; }

		public Device ToDigitalTwins()
		{
			return new Device
			{
				Name = name,
				HardwareId = hardwareId,
				SpaceId = SpaceId.ToString(),
				Sensors = sensors?.Select( sd => sd.ToDigitalTwins() ).ToArray()
			};
		}

		public void AddSensor( SensorDescription sensorDescription )
		{
			if ( sensors == null )
			{
				sensors = new List<SensorDescription>();
			}

			sensors.Add( sensorDescription );
		}
	}
}
