using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class DesiredData
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
        [BsonElement("desiredValue")]
        [DataMember(Name = "desiredValue")]
        public string DesiredValue { get; set; }
        [BsonElement("eventTimestamp")]
        [DataMember(Name = "EventTimestamp")]
        public DateTime EventTimestamp { get; set; }
    }
}
