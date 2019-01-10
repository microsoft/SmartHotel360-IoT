using System;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
    public class BlobDescription
    {
	    public const string FileBlobType = "File";
	    public const string NoneBlobType = "None";
	    public const string JpegContentType = "image/jpeg";

		public string name { get; set; }
		public string type { get; set; }
		public string subtype { get; set; }
		public string description { get; set; }
		public string filepath { get; set; }
		public string contentType { get; set; }

		public Metadata ToDigitalTwinsMetadata(Guid spaceId)
	    {
		    return new Metadata
		    {
			    Name = name,
			    Type = type,
				SubType = subtype,
				Description = description,
				ParentId = spaceId.ToString()
		    };
	    }
	}
}
