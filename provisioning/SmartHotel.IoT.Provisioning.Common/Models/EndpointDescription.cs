using System.Collections.Generic;
using System.Linq;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class EndpointDescription
	{
		public string type { get; set; }
		public IList<string> eventTypes { get; set; }

		public Endpoint ToDigitalTwins(string connectionString, string secondaryConnectionString, string path)
		{
			return new Endpoint
			{
				Type = type,
				EventTypes = eventTypes?.ToArray(),
				ConnectionString = connectionString,
				SecondaryConnectionString = secondaryConnectionString,
				Path = path
			};
		}
	}
}
