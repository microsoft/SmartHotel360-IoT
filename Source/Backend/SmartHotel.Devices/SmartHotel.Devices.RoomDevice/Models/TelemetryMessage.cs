// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace SmartHotel.Devices.RoomDevice.Models
{
	[DataContract( Name = "TelemetryMessage" )]
	[KnownType(typeof(LightTelemetryMessage))]
	[KnownType(typeof(TemperatureTelemetryMessage))]
	[KnownType(typeof(OccupiedTelemetryMessage))]
	public class TelemetryMessage
	{
		public const string TemperatureDataType = "Temperature";
		public const string LightDataType = "Light";
		public const string MotionDataType = "Motion";

		[DataMember]
		public string DeviceId { get; set; }
		[DataMember]
		public string SensorId { get; set; }
		[DataMember]
		public string SpaceId { get; set; }
		[DataMember]
		public string SensorReading { get; set; }
		[DataMember]
		public string EventTimestamp { get; set; }
		[DataMember]
		public string SensorType { get; set; }
		[DataMember]
		public string SensorDataType { get; set; }
		[DataMember]
		public readonly string MessageType = "sensor";
		[DataMember]
		public string IoTHubDeviceId { get; set; }

		public static TelemetryMessage Create(string deviceId, string sensorId, string sensorType, string sensorDataType, object value,
			string spaceId, string ioTHubDeviceId)
		{
			TelemetryMessage telemetryMessage;
			switch (sensorDataType)
			{
				case TemperatureDataType:
					telemetryMessage = new TemperatureTelemetryMessage((double) value);
					break;
				case LightDataType:
					telemetryMessage = new LightTelemetryMessage((double) value);
					break;
				case MotionDataType:
					telemetryMessage = new OccupiedTelemetryMessage((bool) value);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(sensorDataType));
			}

			telemetryMessage.DeviceId = deviceId;
			telemetryMessage.SensorId = sensorId;
			telemetryMessage.EventTimestamp = DateTime.UtcNow.ToString("o");
			telemetryMessage.SensorType = sensorType;
			telemetryMessage.SensorDataType = sensorDataType;
			telemetryMessage.SpaceId = spaceId;
			telemetryMessage.IoTHubDeviceId = ioTHubDeviceId;
			return telemetryMessage;
		}
	}
}