using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningGenerator
{
    public class HotelType
    {
        public string Name { get; set; }
        public int TotalNumberFloors { get; set; }
        public int NumberVipFloors { get; set; }
        public int NumberRoomsPerRegularFloor { get; set; }
        public int NumberRoomsPerVipFloor { get; set; }
        public bool IncludeGym { get; set; }
        public bool IncludeConferenceRoom { get; set; }
    }
}
