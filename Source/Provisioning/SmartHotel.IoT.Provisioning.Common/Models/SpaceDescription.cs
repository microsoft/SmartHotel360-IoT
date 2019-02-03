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
		[YamlMember( Order = 18 )]
		public IList<SpaceReferenceDescription> spaceReferences { get; set; }
		[YamlMember( Order = 17 )]
		public IList<SpaceDescription> spaces { get; set; }
		[YamlMember( Order = 10 )]
		public IList<DeviceDescription> devices { get; set; }
		[YamlMember( Order = 9 )]
		public IList<ResourceDescription> resources { get; set; }
		[YamlMember( Order = 7 )]
		public IList<TypeDescription> types { get; set; }
		[YamlMember( Order = 8 )]
		public IList<string> users { get; set; }
		[YamlMember( Order = 11 )]
		public IList<PropertyKeyDescription> propertyKeys { get; set; }
		[YamlMember( Order = 12 )]
		public IList<PropertyDescription> properties { get; set; }
		[YamlMember( Order = 13 )]
		public IList<MatcherDescription> matchers { get; set; }
		[YamlMember( Order = 14 )]
		public IList<UserDefinedFunctionDescription> userDefinedFunctions { get; set; }
		[YamlMember( Order = 15 )]
		public IList<RoleAssignmentDescription> roleAssignments { get; set; }
		[YamlMember( Order = 16 )]
		public IList<BlobDescription> blobs { get; set; }

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

		public void AddPropertyKey( PropertyKeyDescription propertyKeyDescription )
		{
			if ( propertyKeys == null )
			{
				propertyKeys = new List<PropertyKeyDescription>();
			}

			propertyKeys.Add( propertyKeyDescription );
		}

		public void AddProperty( PropertyDescription propertyDescription )
		{
			if ( properties == null )
			{
				properties = new List<PropertyDescription>();
			}

			properties.Add( propertyDescription );
		}

		public void AddMatcher( MatcherDescription matcherDescription )
		{
			if ( matchers == null )
			{
				matchers = new List<MatcherDescription>();
			}

			matchers.Add( matcherDescription );
		}

		public void AddUserDefinedFunction( UserDefinedFunctionDescription userDefinedFunctionDescription )
		{
			if ( userDefinedFunctions == null )
			{
				userDefinedFunctions = new List<UserDefinedFunctionDescription>();
			}

			userDefinedFunctions.Add( userDefinedFunctionDescription );
		}

		public void AddRoleAssignment( RoleAssignmentDescription roleAssignmentDescription )
		{
			if ( roleAssignments == null )
			{
				roleAssignments = new List<RoleAssignmentDescription>();
			}

			roleAssignments.Add( roleAssignmentDescription );
		}

		public void AddBlob( BlobDescription blobDescription )
		{
			if ( blobs == null )
			{
				blobs = new List<BlobDescription>();
			}

			blobs.Add( blobDescription );
		}
	}
}
