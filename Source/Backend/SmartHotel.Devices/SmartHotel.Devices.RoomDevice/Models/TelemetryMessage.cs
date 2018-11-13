// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
    [DataContract(Name = "TelemetryMessage")]
    public class TelemetryMessage
    {
        [DataMember(Name = "SensorId")]
        public string SensorId { get; set; }
        [DataMember(Name = "SpaceId")]
        public string SpaceId { get; set; }
        [DataMember(Name = "SensorReading")]
        public string SensorReading { get; set; }
        [DataMember(Name = "EventTimestamp")]
        public string EventTimestamp { get; set; }
        [DataMember(Name = "SensorType")]
        public string SensorType { get; set; }
        [DataMember(Name = "SensorDataType")]
        public string SensorDataType { get; set; }
        [DataMember(Name = "MessageType")]
        public readonly string MessageType = "sensor";
    }
}