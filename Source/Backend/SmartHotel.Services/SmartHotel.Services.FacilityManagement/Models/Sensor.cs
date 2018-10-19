using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class Sensor
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string DeviceId { get; set; }
        [DataMember]
        public string SpaceId { get; set; }
        [DataMember]
        public int DataTypeId { get; set; }
        [DataMember]
        public SensorData Data { get; set; }
    }
}
