using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace SmartHotel.Services.FacilityManagement.Models
{
    [DataContract]
    public class SensorData
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
        [DataMember(Name = "eventTimestamp")]
        public DateTime EventTimestamp { get; set; }
	    [DataMember( Name = "iotHubDeviceId" )]
	    public string IoTHubDeviceId { get; set; }
    }
}
