using System;
using System.Collections.Generic;
using System.IO;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common
{
	public static class ProvisioningHelper
	{
		public static ProvisioningDescription LoadSmartHotelProvisioning( string provisioningFilepath )
		{
			var yamlDeserializer = new Deserializer();
			string content = File.ReadAllText( provisioningFilepath );
			var provisioningDescription = yamlDeserializer.Deserialize<ProvisioningDescription>( content );

			string directoryPath = Path.GetDirectoryName( provisioningFilepath );

			foreach ( SpaceDescription rootDescription in provisioningDescription.spaces )
			{
				var referenceSpaces = FindAllReferenceSpaces( rootDescription );
				foreach ( var referenceSpace in referenceSpaces )
				{
					LoadReferenceProvisionings( referenceSpace, directoryPath );
				}
			}

			OutputTotalNumberOfSpaces( provisioningDescription );

			return provisioningDescription;
		}

		private static IEnumerable<SpaceDescription> FindAllReferenceSpaces( SpaceDescription parentDescription )
		{
			List<SpaceDescription> referenceSpaces = new List<SpaceDescription>();

			if ( parentDescription.spaceReferences != null )
			{
				referenceSpaces.Add( parentDescription );
			}
			else if(parentDescription.spaces != null)
			{
				foreach ( SpaceDescription childDescription in parentDescription.spaces )
				{
					referenceSpaces.AddRange( FindAllReferenceSpaces( childDescription ) );
				}
			}

			return referenceSpaces;
		}

		private static void LoadReferenceProvisionings( SpaceDescription spaceDescription, string directoryPath )
		{
			var referenceSpaces = new List<SpaceDescription>();

			foreach ( SpaceReferenceDescription spaceReferenceDescription in spaceDescription.spaceReferences )
			{
				if ( String.IsNullOrEmpty( spaceReferenceDescription.filename ) )
				{
					throw new Exception( $"SpaceReference filename expected." );
				}

				string filepath = Path.Combine( directoryPath, spaceReferenceDescription.filename );

				var referenceSpace = LoadReferenceProvisioning( filepath );

				referenceSpaces.Add( referenceSpace );
			}

			spaceDescription.spaces = referenceSpaces;
		}

		private static SpaceDescription LoadReferenceProvisioning( string referenceFilename )
		{
			var yamlDeserializer = new Deserializer();
			string content = File.ReadAllText( Path.Combine( "Resources", referenceFilename ) );
			return yamlDeserializer.Deserialize<SpaceDescription>( content );
		}

		private static void OutputTotalNumberOfSpaces( ProvisioningDescription provisioningDescription )
		{
			int numberOfSpaces = provisioningDescription.spaces.Count;
			foreach ( SpaceDescription space in provisioningDescription.spaces )
			{
				numberOfSpaces += GetTotalNumberOfChildSpaces( space );
			}

			if ( numberOfSpaces > 995 )
			{
				string warningMessage;
				if ( numberOfSpaces < 1000 )
				{
					warningMessage = "This is extremely close to the API's 1,000 object limit of the" +
									 " Digital Twins public preview and may result in the demo not working.";
				}
				else
				{
					warningMessage = "This is OVER the API's 1,000 object limit of the" +
									 " Digital Twins public preview and WILL result in the demo not working.";
				}
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine( $"Loaded {numberOfSpaces} spaces to provision." +
								   $"{Environment.NewLine}{warningMessage}{Environment.NewLine}We recommend shrinking the topology size" );
				Console.ResetColor();
			}
			else
			{
				Console.WriteLine( $"Loaded {numberOfSpaces} spaces to provision." );
			}
		}

		private static int GetTotalNumberOfChildSpaces( SpaceDescription spaceDescription )
		{
			if ( spaceDescription.spaces == null )
			{
				return 0;
			}

			int numberOfChildSpaces = spaceDescription.spaces.Count;
			foreach ( SpaceDescription childSpace in spaceDescription.spaces )
			{
				int childSpacesCount = GetTotalNumberOfChildSpaces( childSpace );
				if ( childSpacesCount > 0 )
				{
					// Uncomment this line to see how each "level" of spaces stacks up
					// Console.WriteLine( $"{childSpace.name}: {childSpacesCount} child spaces" );
				}
				numberOfChildSpaces += childSpacesCount;
			}

			return numberOfChildSpaces;
		}
	}
}
