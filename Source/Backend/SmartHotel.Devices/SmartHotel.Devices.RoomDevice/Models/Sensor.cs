// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
    [DataContract(Name = "sensor")]

    public class Sensor
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "dataType")]
        public string DataType { get; set; }
        [DataMember(Name = "dataUnitType")]
        public string DataUnitType { get; set; }
        [DataMember(Name = "deviceId")]
        public string DeviceId { get; set; }
        [DataMember(Name = "pollRate")]
        public int PollRate = 0;
        [DataMember(Name = "portType")]
        public string PortType { get; set; }
        [DataMember(Name = "spaceId")]
        public string SpaceId { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
    }
}