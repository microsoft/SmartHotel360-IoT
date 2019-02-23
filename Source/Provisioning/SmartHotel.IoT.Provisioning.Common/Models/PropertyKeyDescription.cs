using System;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class PropertyKeyDescription
	{
		public const string DeviceIdPrefixName = "DeviceIdPrefix";
		public const string DisplayOrder = "DisplayOrder";
		public const string MinTemperatureAlertThreshold = "MinTemperatureAlertThreshold";
		public const string MaxTemperatureAlertThreshold = "MaxTemperatureAlertThreshold";
		public const string ImagePath = "ImagePath";
		public const string ImageBlobId = "ImageBlobId";
		public const string DetailedImagePath = "DetailedImagePath";
		public const string DetailedImageBlobId = "DetailedImageBlobId";
		public const string Latitude = "Latitude";
		public const string Longitude = "Longitude";
		
		public string name { get; set; }
		public string primitiveDataType { get; set; }
		public string description { get; set; }
		public string validationData { get; set; }
		public string min { get; set; }
		public string max { get; set; }

		public PropertyKey ToDigitalTwins(Guid spaceId)
		{
			return new PropertyKey
			{
				Name = name,
				SpaceId = spaceId.ToString(),
				PrimitiveDataType = primitiveDataType,
				Description = description,
				ValidationData = validationData,
				Min = min,
				Max = max
			};
		}

		public class PrimitiveDataType
		{
			public const string String = "string";
			public const string Int = "int";
			public const string UInt = "uint";
		}
	}
}
