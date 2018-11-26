using System.Collections.Generic;

namespace SmartHotel.Services.FacilityManagement.Models
{
    public class DigitalTwinsSpace
    {
		public string id { get; set; }
		public string name { get; set; }
		public string friendlyName { get; set; }
		public string type { get; set; }
		public int typeId { get; set; }
		public string subtype { get; set; }
		public int subtypeId { get; set; }
		public string parentSpaceId { get; set; }
		public IEnumerable<Property> properties { get; set; }
		public IEnumerable<Value> values { get; set; }
	}
}
