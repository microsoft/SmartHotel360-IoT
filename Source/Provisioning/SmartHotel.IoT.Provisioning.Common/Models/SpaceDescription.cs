using System;
using System.Collections.Generic;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common.Models
{
	public class SpaceDescription
	{
		[YamlMember( Order = 1 )]
		public string name { get; set; }
		[YamlMember( Order = 2 )]
		public string description { get; set; }
		[YamlMember( Order = 3 )]
		public string friendlyName { get; set; }
		[YamlMember( Order = 4 )]
		public string type { get; set; }
		[YamlMember( Order = 5 )]
		public string subType { get; set; }
		[YamlMember( Order = 6 )]
		public string keystoreName { get; set; }
		[YamlMember( Order = 12 )]
		public IList<SpaceReferenceDescription> spaceReferences { get; set; }
		[YamlMember( Order = 11 )]
		public IList<SpaceDescription> spaces { get; set; }
		[YamlMember( Order = 10 )]
		public IList<DeviceDescription> devices { get; set; }
		[YamlMember( Order = 9 )]
		public IList<ResourceDescription> resources { get; set; }
		[YamlMember( Order = 7 )]
		public IList<TypeDescription> types { get; set; }
		[YamlMember( Order = 8 )]
		public IList<string> users { get; set; }

		public Space ToDigitalTwins( Guid parentId )
		{
			return new Space
			{
				Name = name,
				Description = description,
				FriendlyName = friendlyName,
				Type = type,
				Subtype = subType,
				ParentSpaceId = parentId != Guid.Empty ? parentId.ToString() : string.Empty
			};
		}

		public void AddSpaceReference( SpaceReferenceDescription spaceReferenceDescription )
		{
			if ( spaceReferences == null )
			{
				spaceReferences = new List<SpaceReferenceDescription>();
			}

			spaceReferences.Add( spaceReferenceDescription );
		}

		public void AddSpace( SpaceDescription spaceDescription )
		{
			if ( spaces == null )
			{
				spaces = new List<SpaceDescription>();
			}

			spaces.Add( spaceDescription );
		}

		public void AddDevice( DeviceDescription deviceDescription )
		{
			if ( devices == null )
			{
				devices = new List<DeviceDescription>();
			}

			devices.Add( deviceDescription );
		}

		public void AddResource( ResourceDescription resourceDescription )
		{
			if ( resources == null )
			{
				resources = new List<ResourceDescription>();
			}

			resources.Add( resourceDescription );
		}

		public void AddType( TypeDescription typeDescription )
		{
			if ( types == null )
			{
				types = new List<TypeDescription>();
			}

			types.Add( typeDescription );
		}

		public void AddUser( string user )
		{
			if ( users == null )
			{
				users = new List<string>();
			}

			users.Add( user );
		}
	}
}
