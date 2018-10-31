using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHotel.Services.FacilityManagement.Models
{
    public class DigitalTwinsSpace
    {
		public string id { get; set; }
		public string name { get; set; }
		public string friendlyName { get; set; }
		public int typeId { get; set; }
		public string parentSpaceId { get; set; }
	}
}
