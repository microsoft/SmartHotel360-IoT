using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningBrandGenerator
{
    public class HotelType
    {
        public string Name { get; set; }
        public int TotalNumberFloors { get; set; }
        public int NumberVipFloors { get; set; }
        public int NumberRoomsPerRegularFloor { get; set; }
        public int NumberRoomsPerVipFloor { get; set; }
    }
}
