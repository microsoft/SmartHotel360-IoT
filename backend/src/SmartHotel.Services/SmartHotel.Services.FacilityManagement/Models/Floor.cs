using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class Floor
    {
        public Floor()
        {
            Rooms = new List<Room>();
            RoomDictionary = new Dictionary<string, Room>();
        }

        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<Room> Rooms { get; set; }

        public Dictionary<string, Room> RoomDictionary { get; set; }
    }
}
