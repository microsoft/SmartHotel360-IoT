using System.Collections.Generic;

namespace SmartHotel.IoT.ProvisioningGenerator
{
    public class Site
    {
		public string OutputDirectory { get; set; }
		public List<HotelType> HotelTypes { get; set; }
        public List<Brand> Brands { get; set; }
    }
}
