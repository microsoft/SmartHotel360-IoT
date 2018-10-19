using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class Room
    {
        public Room()
        {
            Devices = new List<Device>();
        }

        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<Device> Devices { get; set; }
    }
}
