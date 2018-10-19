using System.Runtime.Serialization;

namespace SmartHotel.Services.RoomDevices.Models
{
    [DataContract(Name = "deviceSensorData")]
    public class DeviceRequest
    {
        [DataMember(Name = "roomId")]
        public string RoomId { get; set; }
        [DataMember(Name = "sensorId")]
        public string SensorId { get; set; }
        [DataMember(Name = "deviceId")]
        public string DeviceId { get; set; }
        [DataMember(Name = "methodName")]
        public string MethodName { get; set; }
        [DataMember(Name = "value")]
        public string Value { get; set; }
    }
}
