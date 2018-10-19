using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class Device
    {
        public Device()
        {
            Sensors = new List<Sensor>();
        }

        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string HardwareId { get; set; }
        [DataMember]
        public string SpaceId { get; set; }
        [DataMember]
        public List<Sensor> Sensors { get; set; }
    }
}
