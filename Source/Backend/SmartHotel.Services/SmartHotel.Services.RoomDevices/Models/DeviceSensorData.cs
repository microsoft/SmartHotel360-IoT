using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace SmartHotel.Services.RoomDevices.Models
{
    [DataContract(Name = "deviceSensorData")]
    public class DeviceSensorData
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [BsonElement("sensorId")]
        [DataMember(Name = "sensorId")]
        public string SensorId { get; set; }
        [BsonElement("roomId")]
        [DataMember(Name = "roomId")]
        public string RoomId { get; set; }
        [BsonElement("sensorReading")]
        [DataMember(Name = "sensorReading")]
        public string SensorReading { get; set; }
        [BsonElement("sensorDataType")]
        [DataMember(Name = "sensorDataType")]
        public string SensorDataType { get; set; }
        [BsonElement("eventTimestamp")]
        [DataMember(Name = "EventTimestamp")]
        public DateTime EventTimestamp { get; set; }
    }
}
