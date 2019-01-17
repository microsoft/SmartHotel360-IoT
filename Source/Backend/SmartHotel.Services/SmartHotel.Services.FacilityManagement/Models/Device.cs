using System.Collections.Generic;

namespace SmartHotel.Services.FacilityManagement.Models
{
    public class Device
    {
		public string id { get; set; }
		public string name { get; set; }
		public string hardwareId { get; set; }
		public int typeId { get; set; }
		public string type { get; set; }
		public int subtypeId { get; set; }
		public string subtype { get; set; }
		public string spaceId { get; set; }
		public string status { get; set; }
	    public IEnumerable<Sensor> sensors { get; set; }
	}
}
