using System.Collections.Generic;
using Newtonsoft.Json;

namespace SmartHotel.FacilityManagementWeb.Models
{
	public class AdalConfig
	{
		public string tenant { get; set; }
		public string clientId { get; set; }
		public IList<Endpoint> endpoints { get; set; }

		private string _endpointsString;
		[JsonIgnore]
		public string endpointsString
		{
			get => _endpointsString;
			set
			{
				_endpointsString = value;
				endpoints = !string.IsNullOrWhiteSpace( _endpointsString ) ? JsonConvert.DeserializeObject<IList<Endpoint>>( _endpointsString ) : null;
			}
		}
	}
}
