// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
    [DataContract(Name = "device")]
    public class Device
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "connectionString")]
        public string ConnectionString { get; set; }
        [DataMember(Name = "sensors")]
        public List<Sensor> Sensors { get; set; }
        [DataMember(Name = "friendlyName")]
        public string FriendlyName { get; set; }
        [DataMember(Name = "deviceType")]
        public string DeviceType { get; set; }
        [DataMember(Name = "deviceSubtype")]
        public string DeviceSubtype = "";
        [DataMember(Name = "hardwareId")]
        public string HardwareId { get; set; }
        [DataMember(Name = "spaceId")]
        public string SpaceId { get; set; }
        [DataMember(Name = "status")]
        public string Status { get; set; }
    }
}