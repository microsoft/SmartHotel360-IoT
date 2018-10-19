using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class Hotel
    {
        public Hotel()
        {
            Floors = new List<Floor>();
            FloorsDictionary = new Dictionary<string, Floor>();
        }

        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<Floor> Floors { get; set; }

        public Dictionary<string, Floor> FloorsDictionary { get; set; }
    }
}
