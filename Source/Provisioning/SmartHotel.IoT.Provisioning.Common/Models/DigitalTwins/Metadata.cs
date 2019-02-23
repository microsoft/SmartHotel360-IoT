using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins
{
	public class Metadata
	{
		public string ParentId { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public string SubType { get; set; }
		public string Description { get; set; }
		public string Sharing => "None";
	}
}
