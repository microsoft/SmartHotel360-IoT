using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartHotel.Services.FacilityManagement.Models
{
    public class AzureAdOptions
    {
        public string PassThroughAuthority { get; set; }
		public string Authority { get; set; }
		public string TenantId { get; set; }
		public string ApplicationId { get; set; }
		public string ApplicationSecret { get; set; }
	}
}
