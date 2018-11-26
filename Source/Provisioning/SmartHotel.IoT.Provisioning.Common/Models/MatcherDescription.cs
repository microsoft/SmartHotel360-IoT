using System;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class MatcherDescription
	{
		public string name { get; set; }
		public string dataTypeValue { get; set; }

		public Matcher ToDigitalTwins( Guid spaceId )
		{
			return new Matcher
			{
				Name = name,
				SpaceId = spaceId.ToString(),
				Conditions = new[]
				{
					new Condition
					{
						Target = "Sensor",
						Path = "$.dataType",
						Value = $"\"{dataTypeValue}\"",
						Comparison = "Equals"
					}
				}
			};
		}
	}
}
