using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins;
using Type = SmartHotel.IoT.Provisioning.Common.Models.DigitalTwins.Type;

namespace SmartHotel.IoT.Provisioning
{
	// Following example from https://github.com/Azure-Samples/digital-twins-samples-csharp/tree/master/occupancy-quickstart
	public class Program
	{
		private static int _spacesCreatedCount = 0;
		private const string AadInstance = "https://login.microsoftonline.com/";
		private const string DigitalTwinsResourceId = "0b07f429-9f4b-4714-9392-cc5e8e80c8b0";

		private string _directoryContainingDigitalTwinsProvisioningFile;

		private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
		{
			DefaultValueHandling = DefaultValueHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore
		};

		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		[Option( "-t|--Tenant", Description = "Azure Tenant Id" )]
		[Required]
		public string Tenant { get; }

		[Option( "-ci|--ClientId", Description = "Azure Active Directory App Id" )]
		[Required]
		public string ClientId { get; }

		[Option( "-cs|--ClientSecret", Description = "Key from the Azure Active Directory App" )]
		[Required]
		public string ClientSecret { get; }

		[Option( "-dt|--DigitalTwinsApiEndpoint", Description = "Url for your Digital Twins resource e.g. (https://{resource name}.{resource location}.azuresmartspaces.net/management/api/v1.0" )]
		[Required]
		public string DigitalTwinsApiEndpoint { get; }

		[Option( "-ehcs|--EventHubConnectionString", Description = "Connection string to the Event Hub" )]
		[Required]
		public string EventHubConnectionString { get; }

		[Option( "-ehscs|--EventHubSecondaryConnectionString", Description = "Secondary Connection string to the Event Hub" )]
		[Required]
		public string EventHubSecondaryConnectionString { get; }

		[Option( "-ehn|--EventHubName", Description = "Name of the Event Hub" )]
		[Required]
		public string EventHubName { get; }

		[Option( "-oids|--UserObjectIdsFile", Description = "Json file containing the Azure AD Object IDs for each user" )]
		[Required]
		public string UserObjectIdsFile { get; }

		[Option( "-dtpf|--DigitalTwinsProvisioningFile", Description = "Yaml file containing the tenant definition for Digital Twins provisioning" )]
		[Required]
		public string DigitalTwinsProvisioningFile { get; }

		[Option( "-o|--output", Description = "Name of the file to save provisioning data to.  This is used by SmartHotel.IoT.ProvisioningDevices to configure device settings" )]
		public string OutputFile { get; }

		private async Task OnExecuteAsync()
		{
			try
			{
				string fullUserObjectIdsFilePath = Path.GetFullPath( UserObjectIdsFile );
				string userAadObjectIdsString = await File.ReadAllTextAsync( fullUserObjectIdsFilePath );
				UserAadObjectIdsDescription userAadObjectIds =
					JsonConvert.DeserializeObject<UserAadObjectIdsDescription>( userAadObjectIdsString );
				if ( !userAadObjectIds.AreRequiredValuesFilled() )
				{
					Console.WriteLine( $"The {nameof( UserObjectIdsFile )} must have all the required properties filled." +
									   " (Head Of Operations, Hotel Brand 1 Manager, Hotel 1 Manager, and Hotel 1 Employee)" );
					return;
				}

				HttpClient httpClient = await HttpClientHelper.GetHttpClientAsync( DigitalTwinsApiEndpoint, AadInstance, Tenant,
					DigitalTwinsResourceId, ClientId, ClientSecret );

				string fullPathToDigitalTwinsProvisioningFile = Path.GetFullPath( DigitalTwinsProvisioningFile );
				_directoryContainingDigitalTwinsProvisioningFile = Path.GetDirectoryName( fullPathToDigitalTwinsProvisioningFile );

				Console.WriteLine( "Loading the provisioning files..." );

				ProvisioningDescription provisioningDescription = ProvisioningHelper.LoadSmartHotelProvisioning( fullPathToDigitalTwinsProvisioningFile );

				Console.WriteLine( "Successfully loaded provisioning files." );

				Console.WriteLine( "Creating spaces and endpoints..." );

				await CreateSpacesAsync( httpClient, provisioningDescription.spaces, Guid.Empty, Guid.Empty, userAadObjectIds );
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine( $"Created {_spacesCreatedCount} spaces..." );
				Console.WriteLine();
				Console.WriteLine();

				await CreateEndpointsAsync( httpClient, provisioningDescription.endpoints );

				if ( !string.IsNullOrEmpty( OutputFile ) )
				{
					IDictionary<string, List<DeviceDescription>> allDevices =
						provisioningDescription.spaces.GetAllDeviceDescriptionsByDeviceIdPrefix( string.Empty );

					await File.WriteAllTextAsync( OutputFile,
						JsonConvert.SerializeObject( new SortedDictionary<string, List<DeviceDescription>>( allDevices ) ) );
				}

				Console.WriteLine();
				Console.WriteLine();
			}
			catch ( Exception ex )
			{
				Console.WriteLine( $"Error occurred during Digital Twins provisioning: {ex}" );
			}
		}

		private async Task CreateEndpointsAsync( HttpClient httpClient, IList<EndpointDescription> endpointDescriptions )
		{
			IReadOnlyCollection<Endpoint> existingEndpoints = await EndpointHelpers.GetEndpointsAsync( httpClient, JsonSerializerSettings );
			foreach ( EndpointDescription endpointDescription in endpointDescriptions )
			{
				if ( existingEndpoints.Any( e => e.Type == endpointDescription.type
												 && e.EventTypes.Intersect( endpointDescription.eventTypes ).Any() ) )
				{
					// Assuming that if endpoing matching Type, Path, and EventTypes already exists then its connection strings are already correct
					continue;
				}

				Endpoint endpoint = endpointDescription.ToDigitalTwins( EventHubConnectionString, EventHubSecondaryConnectionString, EventHubName );

				Console.WriteLine( $"Creating Endpoint: {JsonConvert.SerializeObject( endpoint, Formatting.Indented, JsonSerializerSettings )}" );
				var request = HttpMethod.Post.CreateRequest( "endpoints", JsonConvert.SerializeObject( endpoint, JsonSerializerSettings ) );
				var response = await httpClient.SendAsync( request );
				if ( !response.IsSuccessStatusCode )
				{
					Console.WriteLine( "Failed to create endpoint." );
				}
			}

			Console.WriteLine();
		}

		private async Task CreateSpacesAsync( HttpClient httpClient, IList<SpaceDescription> spaceDescriptions,
			Guid parentId, Guid keystoreId, UserAadObjectIdsDescription userAadObjectIds )
		{
			foreach ( SpaceDescription spaceDescription in spaceDescriptions )
			{
				await CreateSpaceAsync( httpClient, spaceDescription, parentId, keystoreId, userAadObjectIds );
			}
		}

		private async Task CreateSpaceAsync( HttpClient httpClient, SpaceDescription spaceDescription,
			Guid parentId, Guid keystoreId, UserAadObjectIdsDescription userAadObjectIds )
		{
			Space spaceToCreate = spaceDescription.ToDigitalTwins( parentId );
			Space existingSpace = await SpaceHelpers.GetUniqueSpaceAsync( httpClient, spaceToCreate.Name, parentId, JsonSerializerSettings );
			var spaceId = !string.IsNullOrWhiteSpace( existingSpace?.Id )
				? Guid.Parse( existingSpace.Id )
				: await CreateSpaceAsync( httpClient, spaceToCreate );

			Console.WriteLine();

			if ( spaceId != Guid.Empty )
			{
				Guid keystoreIdToUseForChildren = keystoreId;
				// Keystore creation must happen first to ensure that devices down the tree can get their SaS tokens from it
				if ( !string.IsNullOrWhiteSpace( spaceDescription.keystoreName ) )
				{
					Keystore existingKeystore = await KeyStoresHelper.GetUniqueKeystoreAsync( httpClient,
						spaceDescription.keystoreName, spaceId, JsonSerializerSettings );
					Guid createdKeystoreId = !string.IsNullOrWhiteSpace( existingKeystore?.Id )
						? Guid.Parse( existingKeystore.Id )
						: await CreateKeystoreAsync( httpClient, spaceDescription.keystoreName, spaceId );
					if ( createdKeystoreId != Guid.Empty )
					{
						keystoreIdToUseForChildren = createdKeystoreId;
					}
				}

				// Resources must be created next to ensure that devices created down the tree will succeed
				if ( spaceDescription.resources != null )
				{
					await CreateResourcesAsync( httpClient, spaceDescription.resources, spaceId );
				}

				// Types must be created next to ensure that devices/sensors created down the tree will succeed
				if ( spaceDescription.types != null )
				{
					await CreateTypesAsync( httpClient, spaceDescription.types, spaceId );
				}

				if ( spaceDescription.devices != null )
				{
					await CreateDevicesAsync( httpClient, spaceDescription.devices, keystoreIdToUseForChildren, spaceId );
				}

				if ( spaceDescription.matchers != null )
				{
					await CreateMatchersAsync( httpClient, spaceDescription.matchers, spaceId );
				}

				if ( spaceDescription.userDefinedFunctions != null )
				{
					await CreateUserDefinedFunctionsAsync( httpClient, spaceDescription.userDefinedFunctions, spaceId );
				}

				if ( spaceDescription.roleAssignments != null )
				{
					await CreateRoleAssignmentsAsync( httpClient, spaceDescription.roleAssignments, spaceId );
				}

				if ( spaceDescription.users != null )
				{
					await CreateUserRoleAssignmentsAsync( httpClient, spaceDescription.users, spaceId, userAadObjectIds );
				}

				if ( spaceDescription.propertyKeys != null )
				{
					await CreatePropertyKeysAsync( httpClient, spaceDescription.propertyKeys, spaceId );
				}

				if ( spaceDescription.properties != null )
				{
					await CreatePropertiesAsync( httpClient, spaceDescription.properties, spaceId );
				}

				if ( spaceDescription.blobs != null )
				{
					await CreateBlobAsync( httpClient, spaceDescription.blobs, spaceId );
				}

				if ( spaceDescription.spaces != null )
				{
					await CreateSpacesAsync( httpClient, spaceDescription.spaces, spaceId, keystoreIdToUseForChildren, userAadObjectIds );
				}
			}
		}

		private static async Task<Guid> CreateSpaceAsync( HttpClient httpClient, Space space )
		{
			Console.WriteLine( $"Creating Space: {JsonConvert.SerializeObject( space, Formatting.Indented, JsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( "spaces", JsonConvert.SerializeObject( space, JsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			var id = await response.GetIdAsync();
			if ( Guid.Empty != id )
			{
				Interlocked.Increment( ref _spacesCreatedCount );
			}
			return id;
		}

		private static async Task<Guid> CreateKeystoreAsync( HttpClient httpClient, string keystoreName, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Keystore must have a {nameof( spaceId )}" );
			}

			Keystore keystore = new Keystore
			{
				Name = keystoreName,
				SpaceId = spaceId.ToString()
			};

			Console.WriteLine( $"Creating Keystore: {JsonConvert.SerializeObject( keystore, Formatting.Indented, JsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( "keystores", JsonConvert.SerializeObject( keystore, JsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			Guid keystoreId = await response.GetIdAsync();
			if ( keystoreId == Guid.Empty )
			{
				Console.WriteLine( "Failed to create keystore" );
			}
			else
			{
				Console.WriteLine( "Generating key" );
				var keyRequest = HttpMethod.Post.CreateRequest( $"keystores/{keystoreId}/keys" );
				var keyResponse = await httpClient.SendAsync( keyRequest );
				if ( !keyResponse.IsSuccessStatusCode )
				{
					Console.WriteLine( "Failed generating key." );
				}
				Console.WriteLine();
			}

			return keystoreId;
		}

		private static async Task CreateResourcesAsync( HttpClient httpClient, IList<ResourceDescription> resourceDescriptions,
			Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Resources must have a {nameof( spaceId )}" );
			}

			var space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId );
			IReadOnlyCollection<Resource> existingResources = await space.GetExistingChildResourcesAsync( httpClient );

			var resourceIds = new List<Guid>();
			var resourcesToCreate =
				resourceDescriptions.Where( rd =>
						 !existingResources.Any( er => er.Type.Equals( rd.type, StringComparison.OrdinalIgnoreCase ) ) )
					.ToArray();
			foreach ( ResourceDescription resourceDescription in resourcesToCreate )
			{
				Resource resource = resourceDescription.ToDigitalTwins( spaceId );
				Guid createdId = await CreateResourceAsync( httpClient, resource );
				if ( createdId != Guid.Empty )
				{
					resourceIds.Add( createdId );
				}
				else
				{
					Console.WriteLine( $"Failed to create resource. Please try manually: {resourceDescription.type}" );
				}
			}

			if ( resourceIds.Any() )
			{
				// wait until all the resources are created and ready to use in case downstream operations (like device creation)
				//	are dependent on it.
				Console.WriteLine( "Polling until all resources are no longer in the provisioning state." );

				IEnumerable<Task<bool>> statusVerificationTasks =
					resourceIds.Select( resourceId => ResourcesHelpers.WaitTillResourceCreationCompletedAsync( httpClient, resourceId ) );

				await Task.WhenAll( statusVerificationTasks );
			}
		}

		private static async Task<Guid> CreateResourceAsync( HttpClient httpClient, Resource resource )
		{
			Console.WriteLine( $"Creating Resource: {JsonConvert.SerializeObject( resource, Formatting.Indented, JsonSerializerSettings )}" );
			var request = HttpMethod.Post.CreateRequest( "resources", JsonConvert.SerializeObject( resource, JsonSerializerSettings ) );
			var response = await httpClient.SendAsync( request );
			return await response.GetIdAsync();
		}

		private static async Task CreateTypesAsync( HttpClient httpClient, IList<TypeDescription> typeDescriptions, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Types must have a {nameof( spaceId )}" );
			}

			var space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId );
			IReadOnlyCollection<Type> existingTypes = await space.GetExistingTypesAsync( httpClient );

			var typesToCreate =
				typeDescriptions.Where( td =>
						 !existingTypes.Any( et =>
							  et.Name.Equals( td.name, StringComparison.OrdinalIgnoreCase ) &&
							  et.Category.Equals( td.category, StringComparison.OrdinalIgnoreCase ) ) )
					.ToArray();
			foreach ( TypeDescription typeDescription in typesToCreate )
			{
				Type type = typeDescription.ToDigitalTwins( spaceId );
				await type.CreateTypeAsync( httpClient, JsonSerializerSettings );
			}
		}

		private static async Task CreateDevicesAsync( HttpClient httpClient, IList<DeviceDescription> deviceDescriptions,
			Guid keystoreIdToUseForChildren, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Devices must have a {nameof( spaceId )}" );
			}

			foreach ( DeviceDescription deviceDescription in deviceDescriptions )
			{
				deviceDescription.SpaceId = spaceId;
				Device device = deviceDescription.ToDigitalTwins();

				Device existingDevice = await deviceDescription.GetSingleExistingDeviceAsync( httpClient, JsonSerializerSettings );
				Guid createdId = !string.IsNullOrWhiteSpace( existingDevice?.Id )
					? Guid.Parse( existingDevice.Id )
					: await device.CreateDeviceAsync( httpClient, JsonSerializerSettings );
				if ( createdId != Guid.Empty )
				{
					if ( keystoreIdToUseForChildren != Guid.Empty )
					{
						string sasToken = await GetDeviceSasTokenAsync( httpClient, keystoreIdToUseForChildren, deviceDescription.hardwareId );
						if ( !string.IsNullOrWhiteSpace( sasToken ) )
						{
							deviceDescription.SasToken = sasToken;
						}
					}
				}
				Console.WriteLine();
			}
		}

		private static async Task<string> GetDeviceSasTokenAsync( HttpClient httpClient, Guid keystoreId, string deviceHardwareId )
		{
			if ( keystoreId == Guid.Empty )
			{
				throw new ArgumentException( $"{nameof( GetDeviceSasTokenAsync )} requires a non empty guid as {nameof( keystoreId )}" );
			}

			if ( string.IsNullOrWhiteSpace( deviceHardwareId ) )
			{
				if ( keystoreId == Guid.Empty )
				{
					throw new ArgumentException( $"{nameof( GetDeviceSasTokenAsync )} requires a value for {nameof( deviceHardwareId )}" );
				}
			}

			var request = HttpMethod.Get.CreateRequest( $"keystores/{keystoreId}/keys/last/token?deviceMac={deviceHardwareId}" );
			var response = await httpClient.SendAsync( request );
			if ( response.IsSuccessStatusCode )
			{
				string sasToken = await response.Content.ReadAsStringAsync();
				Console.WriteLine( $"Retrieved SasToken for device. (hardwareId: {deviceHardwareId}, SaSToken: {sasToken}" );
				return sasToken.Trim( '"' );
			}

			return null;
		}

		private static async Task CreateMatchersAsync( HttpClient httpClient, IList<MatcherDescription> matchers, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Matchers must have a {nameof( spaceId )}" );
			}

			var space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId );
			IReadOnlyCollection<Matcher> existingMatchers = await space.GetExistingMatchersAsync( httpClient );

			var matchersToCreate =
				matchers.Where( md =>
						!existingMatchers.Any( em => em.Name.Equals( md.name, StringComparison.OrdinalIgnoreCase ) ) )
					.ToArray();
			foreach ( MatcherDescription matcherDescription in matchersToCreate )
			{
				Matcher matcher = matcherDescription.ToDigitalTwins( spaceId );
				await matcher.CreateMatcherAsync( httpClient, JsonSerializerSettings );
			}
		}

		private async Task CreateUserDefinedFunctionsAsync( HttpClient httpClient, IList<UserDefinedFunctionDescription> userDefinedFunctions, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"UserDefinedFunctions must have a {nameof( spaceId )}" );
			}

			foreach ( UserDefinedFunctionDescription userDefinedFunctionDescription in userDefinedFunctions )
			{
				ICollection<Matcher> matchers = await MatcherHelpers.FindMatchersAsync( httpClient, userDefinedFunctionDescription.matcherNames, spaceId );

				string scriptPath = userDefinedFunctionDescription.script;
				if ( !Path.IsPathFullyQualified( scriptPath ) )
				{
					scriptPath = Path.Combine( _directoryContainingDigitalTwinsProvisioningFile, scriptPath );
				}

				string udfText = await File.ReadAllTextAsync( scriptPath );
				if ( String.IsNullOrWhiteSpace( udfText ) )
				{
					Console.WriteLine( $"Error creating user defined function: Couldn't read from {userDefinedFunctionDescription.script}" );
				}
				else
				{
					await userDefinedFunctionDescription.CreateOrPatchUserDefinedFunctionAsync( httpClient, udfText, spaceId,
						matchers );
				}
			}
		}

		private async Task CreateRoleAssignmentsAsync( HttpClient httpClient, IList<RoleAssignmentDescription> roleAssignmentDescriptions, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"RoleAssignments must have a {nameof( spaceId )}" );
			}

			string spacePath = await SpaceHelpers.GetSpaceFullPathAsync( httpClient, spaceId );
			foreach ( RoleAssignmentDescription roleAssignmentDescription in roleAssignmentDescriptions )
			{
				string objectId;
				switch ( roleAssignmentDescription.objectIdType )
				{
					case RoleAssignment.ObjectIdTypes.UserDefinedFunctionId:
						objectId = ( await UserDefinedFunctionHelpers.FindUserDefinedFunctionAsync( httpClient,
							roleAssignmentDescription.objectName, spaceId ) )?.Id;
						break;
					default:
						throw new ArgumentOutOfRangeException(
							$"{nameof( RoleAssignment )} with {nameof( RoleAssignmentDescription.objectName )} must" +
							$" have a known {nameof( RoleAssignmentDescription.objectIdType )}" +
							$" but instead has {roleAssignmentDescription.objectIdType}" );
				}

				if ( objectId != null )
				{
					var roleAssignment = roleAssignmentDescription.ToDigitalTwins( objectId, spacePath );

					var existingRoleAssignment = await roleAssignment.GetUniqueRoleAssignmentAsync( httpClient );
					if ( existingRoleAssignment == null )
					{
						await roleAssignment.CreateRoleAssignmentAsync( httpClient, JsonSerializerSettings );
					}
					else
					{
						Console.WriteLine( $"{nameof( RoleAssignment )} already exists, so skipping creation." );
					}
				}
			}
		}

		private async Task CreateUserRoleAssignmentsAsync( HttpClient httpClient, IList<string> users, Guid spaceId,
			UserAadObjectIdsDescription userAadObjectIds )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"User Role Assignment must have a {nameof( spaceId )}" );
			}

			foreach ( string user in users )
			{
				if ( userAadObjectIds.TryGetValue( user, out string oid ) )
				{
					string spacePath = await SpaceHelpers.GetSpaceFullPathAsync( httpClient, spaceId );

					if ( string.IsNullOrWhiteSpace( spacePath ) )
					{
						Console.WriteLine( $"Unable to get the full path for the current space. Cannot create a role assignment. (Space Id: {spaceId})" );
						continue;
					}

					var roleAssignment = new RoleAssignment
					{
						RoleId = RoleAssignment.RoleIds.User,
						ObjectId = oid,
						ObjectIdType = RoleAssignment.ObjectIdTypes.UserId,
						TenantId = Tenant,
						Path = spacePath
					};

					var existingRoleAssignment = await roleAssignment.GetUniqueRoleAssignmentAsync( httpClient );
					if ( existingRoleAssignment == null )
					{
						await roleAssignment.CreateRoleAssignmentAsync( httpClient, JsonSerializerSettings );
					}
					else
					{
						Console.WriteLine( $"{nameof( RoleAssignment )} already exists, so skipping creation." );
					}
				}
			}
		}

		private async Task CreatePropertyKeysAsync( HttpClient httpClient, IList<PropertyKeyDescription> propertyKeys, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"PropertyKey must have a {nameof( spaceId )}" );
			}

			var space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId );
			IReadOnlyCollection<PropertyKey> existingPropertyKeys = await space.GetExistingPropertyKeysAsync( httpClient );

			var propertyKeysToCreate =
				propertyKeys.Where( pkd =>
						!existingPropertyKeys.Any( epk => epk.Name.Equals( pkd.name, StringComparison.OrdinalIgnoreCase ) ) )
					.ToArray();

			foreach ( PropertyKeyDescription propertyKeyDescription in propertyKeysToCreate )
			{
				PropertyKey propertyKey = propertyKeyDescription.ToDigitalTwins( spaceId );
				await propertyKey.CreatePropertyKeyAsync( spaceId, httpClient, JsonSerializerSettings );
			}
		}

		private async Task CreatePropertiesAsync( HttpClient httpClient, IList<PropertyDescription> properties, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Property must have a {nameof( spaceId )}" );
			}

			Space space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId, "Properties" );

			var propertiesToCreate =
				properties.Where( pd =>
						!space.Properties.Any( ep => ep.Name.Equals( pd.name, StringComparison.OrdinalIgnoreCase ) ) )
					.ToArray();

			foreach ( PropertyDescription propertyDescription in propertiesToCreate )
			{
				Property property = propertyDescription.ToDigitalTwins();
				await property.CreateOrUpdatePropertyAsync( spaceId, httpClient, JsonSerializerSettings );
			}
		}

		private async Task CreateBlobAsync( HttpClient httpClient, IList<BlobDescription> blobDescriptions, Guid spaceId )
		{
			if ( spaceId == Guid.Empty )
			{
				throw new ArgumentException( $"Blob must have a {nameof( spaceId )}" );
			}

			Space space = await SpaceHelpers.GetSpaceAsync( httpClient, spaceId, "Properties" );

			Property imageBlobIdProperty = space.Properties.FirstOrDefault( p => p.Name == PropertyKeyDescription.ImageBlobId );
			Property detailedImageBlobIdProperty = space.Properties.FirstOrDefault( p => p.Name == PropertyKeyDescription.DetailedImageBlobId );
			foreach ( BlobDescription blobDescription in blobDescriptions )
			{
				Property desiredBlobIdProperty;
				string desiredImagePathPropertyName;
				string desitedImageBlobIdPropertyName;
				if ( blobDescription.isPrimaryBlob )
				{
					desiredBlobIdProperty = imageBlobIdProperty;
					desiredImagePathPropertyName = PropertyKeyDescription.ImagePath;
					desitedImageBlobIdPropertyName = PropertyKeyDescription.ImageBlobId;
				}
				else
				{
					desiredBlobIdProperty = detailedImageBlobIdProperty;
					desiredImagePathPropertyName = PropertyKeyDescription.DetailedImagePath;
					desitedImageBlobIdPropertyName = PropertyKeyDescription.DetailedImageBlobId;
				}


				Metadata metadata = blobDescription.ToDigitalTwinsMetadata( spaceId );
				var multipartContent = new MultipartFormDataContent( "USER_DEFINED_BOUNDARY" );
				var metadataContent = new StringContent( JsonConvert.SerializeObject( metadata ), Encoding.UTF8, "application/json" );
				metadataContent.Headers.ContentType = MediaTypeHeaderValue.Parse( "application/json; charset=utf-8" );
				multipartContent.Add( metadataContent, "metadata" );

				string blobContentFilePath = blobDescription.filepath;
				if ( !Path.IsPathFullyQualified( blobContentFilePath ) )
				{
					blobContentFilePath = Path.Combine( _directoryContainingDigitalTwinsProvisioningFile, blobContentFilePath );
				}

				using ( FileStream s = File.OpenRead( blobContentFilePath ) )
				{
					var blobContent = new StreamContent( s );
					blobContent.Headers.ContentType = MediaTypeHeaderValue.Parse( blobDescription.contentType );
					multipartContent.Add( blobContent, "contents" );

					Console.WriteLine();
					HttpRequestMessage request;
					if ( desiredBlobIdProperty == null )
					{
						Console.WriteLine( $"Creating new blob for Space ({spaceId}) from file: {blobDescription.filepath}" );
						request = new HttpRequestMessage( HttpMethod.Post, "spaces/blobs" )
						{
							Content = multipartContent
						};
					}
					else
					{
						Console.WriteLine( $"Updating blob for Space ({spaceId}) from file: {blobDescription.filepath}" );
						request = new HttpRequestMessage( HttpMethod.Patch, $"spaces/blobs/{desiredBlobIdProperty.Value}" )
						{
							Content = multipartContent
						};
					}

					var response = await httpClient.SendAsync( request );
					Guid blobId = await response.GetIdAsync();

					if ( desiredBlobIdProperty == null )
					{
						var uriBuilder = new UriBuilder( $"{httpClient.BaseAddress}spaces/blobs/{blobId}/contents/latest" );

						var imagePathProperty = new Property
						{
							Name = desiredImagePathPropertyName,
							Value = uriBuilder.Uri.ToString()
						};

						await imagePathProperty.CreateOrUpdatePropertyAsync( spaceId, httpClient, JsonSerializerSettings );

						imageBlobIdProperty = new Property
						{
							Name = desitedImageBlobIdPropertyName,
							Value = blobId.ToString()
						};

						await imageBlobIdProperty.CreateOrUpdatePropertyAsync( spaceId, httpClient, JsonSerializerSettings );
					}
				}
			}
		}
	}
}
