using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common.Models;
using SmartHotel.IoT.Provisioning.Common.Models.Docker;
using SmartHotel.IoT.Provisioning.Common.Models.Kubernetes;
using SmartHotel.IoT.ProvisioningDevices.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartHotel.IoT.Provisioning.Common;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.ProvisioningDevices
{
	public class Program
	{
		private static readonly string DigitalTwinsManagementApiSetting = "ManagementApiUrl";
		private static readonly string MessageIntervalSetting = "MessageIntervalInMilliSeconds";
		private static readonly string SasTokenSetting = "SasToken";
		private static readonly string HardwareIdSetting = "HardwareId";
		private static readonly string IoTHubConnectionStringSetting = "IoTHubDeviceConnectionString";
		private static readonly string StartupDelayInSecondsSetting = "StartupDelayInSeconds";

		private const int MessageIntervalDefault = 5000;
		private const double DigitalTwinsApiCallLimiter = 75.0;

		[Option( "-i|--Input", Description = "Output from the Provisioning App" )]
		[Required]
		public string ProvisioningFile { get; }

		[Option( "-iot|--IoTInformation", Description = "IoTHub connection string information file" )]
		[Required]
		public string IoTConnectionFile { get; }

		[Option( "-dt|--DigitalTwinsApiEndpoint", Description = "Url for your Digital Twins resource e.g. (https://{resource name}.{resource location}.azuresmartspaces.net/management/api/v1.0" )]
		[Required]
		public string DigitalTwinsApiEndpoint { get; }

		[Option( "-mi|--MessageInterval", Description = "How often to push sensor data to Digital Twins" )]
		public int MessageInterval { get; }

		[Option( "-d|--Directory", Description = "The directory containing the Devices' yaml files to provision" )]
		[Required]
		public string DataDirectory { get; }

		[Option( "-cr|--ContainerRegistry", Description = "The name or url of your Azure Container Registry" )]
		[Required]
		public string ContainerRegistry { get; }

		private string GetDockerBasePath()
		{
			var path = Path.Combine( DataDirectory, "docker-compose.yml" );
			return Path.GetFullPath( path );
		}

		private string GetDockerDemoPath()
		{
			var path = Path.Combine( DataDirectory, "docker-compose.demo.yml" );
			return Path.GetFullPath( path );
		}

		private string GetDockerOverridePath()
		{
			var path = Path.Combine( DataDirectory, "docker-compose.override.yml" );
			return Path.GetFullPath( path );
		}

		private string GetKubernetesPath()
		{
			var path = Path.Combine( DataDirectory, "deployments.yaml" );
			return Path.GetFullPath( path );
		}

		private string GetKubernetesDemoPath()
		{
			var path = Path.Combine( DataDirectory, "deployments.demo.yaml" );
			return Path.GetFullPath( path );
		}

		public static async Task<int> Main( string[] args ) => await CommandLineApplication.ExecuteAsync<Program>( args );

		private async Task OnExecuteAsync()
		{
			ProvisioningData provisioningData = await LoadProvisioningData();
			IoTHubConnectionStrings iotHubConnectionStrings = await LoadIoTHubConnectionStrings();

			Console.Write( "Processing Provisioning Data, please wait " );

			Task baseTask = ProcessDocker( GetDockerBasePath(), provisioningData, iotHubConnectionStrings );
			Task overrideTask = ProcessDocker( GetDockerOverridePath(), provisioningData, iotHubConnectionStrings );
			Task demoTask = GenerateDocker( GetDockerDemoPath(), provisioningData, iotHubConnectionStrings );
			Task kubernetesTask = ProcessKubernetes( GetKubernetesPath(), provisioningData, iotHubConnectionStrings );
			Task kubernetesDemoTask = GenerateKubernetes( GetKubernetesDemoPath(), provisioningData, iotHubConnectionStrings );

			await Task.WhenAll( baseTask, demoTask, overrideTask, kubernetesTask, kubernetesDemoTask );

			Console.WriteLine();
			Console.WriteLine();
		}

		private async Task<ProvisioningData> LoadProvisioningData()
		{
			string content = await File.ReadAllTextAsync( ProvisioningFile );
			return JsonConvert.DeserializeObject<ProvisioningData>( content );
		}

		private async Task<IoTHubConnectionStrings> LoadIoTHubConnectionStrings()
		{
			string content = await File.ReadAllTextAsync( IoTConnectionFile );
			return JsonConvert.DeserializeObject<IoTHubConnectionStrings>( content );
		}

		#region Kubernetes
		private async Task ProcessKubernetes( string path, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			var outline = await LoadKubernetesOutline( path );

			await ProcessExistingKubernetesOutline( outline, provisioningData, iotHubConnectionStrings );

			await SaveKubernetesOutline( path, outline );
		}

		private async Task GenerateKubernetes( string path, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			var outline = new KubernetesOutline();

			await GenerateKubernetesOutline( outline, provisioningData, iotHubConnectionStrings );

			await SaveKubernetesOutline( path, outline );
		}

		private async Task<KubernetesOutline> LoadKubernetesOutline( string path )
		{
			return await Task.Run( () => KubernetesOutline.FromFile( path ) );
		}

		private async Task ProcessExistingKubernetesOutline( KubernetesOutline outline, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			await Task.Run( () =>
			 {
				 foreach ( var deployment in outline.Deployments )
				 {
					 try
					 {
						 var metadata = deployment.metadata;

						 if ( metadata != null )
						 {
							 var qualifiers = metadata.name.Split( '.' );
							 var room = qualifiers.Last().ToLower();

							 if ( provisioningData.ContainsKey( room ) )
							 {
								 foreach ( var container in deployment.spec.template.spec.containers )
								 {
									 var apiUrlSetting = container.env.FirstOrDefault( e => e.name == DigitalTwinsManagementApiSetting );
									 if ( apiUrlSetting != null )
									 {
										 apiUrlSetting.value = DigitalTwinsApiEndpoint;
									 }

									 var messageIntervalSetting = container.env.FirstOrDefault( e => e.name == MessageIntervalSetting );
									 if ( messageIntervalSetting != null )
									 {
										 messageIntervalSetting.value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString();
									 }

									 var containerImage = FormatContainerImageName( container.image );
									 container.image = containerImage;

									 if ( iotHubConnectionStrings.ContainsKey( room ) )
									 {
										 string connectionString = iotHubConnectionStrings[room];
										 var iotSetting = container.env.FirstOrDefault( s => s.name == IoTHubConnectionStringSetting );

										 if ( iotSetting != null )
										 {
											 iotSetting.value = connectionString;
										 }
									 }

									 DeviceDescription device = provisioningData[room].FirstOrDefault();
									 if ( device != null )
									 {
										 var sasTokenSetting = container.env.FirstOrDefault( e => e.name == SasTokenSetting );
										 if ( sasTokenSetting != null )
										 {
											 sasTokenSetting.value = device.SasToken;
										 }

										 var hardwareIdSetting = container.env.FirstOrDefault( e => e.name == HardwareIdSetting );
										 if ( hardwareIdSetting != null )
										 {
											 hardwareIdSetting.value = device.hardwareId;
										 }
									 }
								 }
							 }
						 }
					 }
					 catch ( Exception e )
					 {
						 Console.WriteLine( e );
					 }
				 }
			 } );
		}

		private async Task GenerateKubernetesOutline( KubernetesOutline outline, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			await Task.Run( () =>
			{
				int deploymentCount = 0;
				foreach ( KeyValuePair<string, List<DeviceDescription>> provisioningEntry in provisioningData )
				{
					iotHubConnectionStrings.TryGetValue( provisioningEntry.Key,
						out string iotHubConnectionString );

					foreach ( DeviceDescription device in provisioningEntry.Value )
					{
						deploymentCount++;
						string deviceType = device.name.ToLower();
						string serviceKey = $"sh.d.{deviceType}.{provisioningEntry.Key.ToLower()}";

						var container = new KubernetesContainer
						{
							name = $"device-{deviceType}",
							image = GetContainerImageName( deviceType ),
							imagePullPolicy = "Always",
							env = new List<KubernetesEnvironmentSetting>()
						};
						container.env.Add( new KubernetesEnvironmentSetting { name = HardwareIdSetting, value = device.hardwareId } );
						container.env.Add( new KubernetesEnvironmentSetting { name = IoTHubConnectionStringSetting, value = iotHubConnectionString } );
						container.env.Add( new KubernetesEnvironmentSetting { name = DigitalTwinsManagementApiSetting, value = DigitalTwinsApiEndpoint } );
						container.env.Add( new KubernetesEnvironmentSetting
						{
							name = MessageIntervalSetting,
							value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString()
						} );
						container.env.Add( new KubernetesEnvironmentSetting { name = SasTokenSetting, value = device.SasToken } );
						container.env.Add( new KubernetesEnvironmentSetting
						{
							name = StartupDelayInSecondsSetting,
							value = Math.Floor( deploymentCount / DigitalTwinsApiCallLimiter ).ToString()
						} );

						var template = new KubernetesTemplate
						{
							metadata = new KubernetesMetadata { labels = new KubernetesLabels { app = serviceKey, component = serviceKey } },
							spec = new KubernetesSpec { containers = new List<KubernetesContainer> { container } }
						};
						var spec = new KubernetesSpec
						{
							template = template
						};
						var deployment = new KubernetesDeployment
						{
							apiVersion = "extensions/v1beta1",
							kind = "Deployment",
							metadata = new KubernetesMetadata { name = serviceKey },
							spec = spec
						};

						outline.Deployments.Add( deployment );
					}
				}
			} );
		}

		private async Task SaveKubernetesOutline( string path, KubernetesOutline outline )
		{
			string output = string.Empty;

			var yamlSerializer = new SerializerBuilder().WithTypeConverter( new KubernetesTypeConverter() ).Build();

			foreach ( var deployment in outline.Deployments )
			{
				output += yamlSerializer.Serialize( deployment );
				output += "---\n";
			}

			await File.WriteAllTextAsync( path, output );
		}
		#endregion

		#region Docker
		private async Task ProcessDocker( string path, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			DockerOutline dockerOutline = await LoadDockerOutline( path );

			await ProcessExistingDockerOutline( dockerOutline, provisioningData, iotHubConnectionStrings );

			await SaveDockerOutline( path, dockerOutline );
		}

		private async Task GenerateDocker( string path, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			DockerOutline dockerOutline = new DockerOutline
			{
				version = "3.4",
				services = new Dictionary<string, ServiceDescription>()
			};

			await GenerateDockerOutline( dockerOutline, provisioningData, iotHubConnectionStrings );

			await SaveDockerOutline( path, dockerOutline );
		}

		private async Task<DockerOutline> LoadDockerOutline( string path )
		{
			var yamlDeserializer = new Deserializer();

			string content = await File.ReadAllTextAsync( path );
			return yamlDeserializer.Deserialize<DockerOutline>( content );
		}

		private async Task SaveDockerOutline( string path, DockerOutline outline )
		{
			var yamlSerializer = new Serializer();
			var output = yamlSerializer.Serialize( outline );

			await File.WriteAllTextAsync( path, output );
		}

		private async Task ProcessExistingDockerOutline( DockerOutline outline, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			await Task.Run( () =>
			 {
				 foreach ( var kvPair in outline.services )
				 {
					 var service = kvPair.Value;

					 var containerImage = FormatContainerImageName( service.image );
					 service.image = containerImage;

					 if ( service.environment != null )
					 {
						 var apiUrlSetting = service.environment.FirstOrDefault( e => e.name == DigitalTwinsManagementApiSetting );
						 if ( apiUrlSetting != null )
						 {
							 apiUrlSetting.value = DigitalTwinsApiEndpoint;
						 }

						 var messageIntervalSetting = service.environment.FirstOrDefault( e => e.name == MessageIntervalSetting );
						 if ( messageIntervalSetting != null )
						 {
							 messageIntervalSetting.value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString();
						 }

						 var qualifiers = kvPair.Key.Split( '.' );
						 var name = qualifiers.Last().ToLower();

						 if ( provisioningData.ContainsKey( name ) )
						 {
							 if ( iotHubConnectionStrings.ContainsKey( name ) )
							 {
								 string iotConnectionString = iotHubConnectionStrings[name];
								 var iotSetting = service.environment.FirstOrDefault( s => s.name == IoTHubConnectionStringSetting );

								 if ( iotSetting != null )
								 {
									 iotSetting.value = iotConnectionString;
								 }
							 }

							 DeviceDescription device = provisioningData[name].FirstOrDefault();
							 if ( device != null )
							 {
								 var sasTokenSetting = service.environment.FirstOrDefault( e => e.name == SasTokenSetting );
								 if ( sasTokenSetting != null )
								 {
									 sasTokenSetting.value = device.SasToken;
								 }

								 var hardwareIdSetting = service.environment.FirstOrDefault( e => e.name == HardwareIdSetting );
								 if ( hardwareIdSetting != null )
								 {
									 hardwareIdSetting.value = device.hardwareId;
								 }
							 }
						 }
					 }
				 }
			 } );
		}

		private async Task GenerateDockerOutline( DockerOutline outline, ProvisioningData provisioningData,
			IoTHubConnectionStrings iotHubConnectionStrings )
		{
			await Task.Run( () =>
			{
				int serviceCount = 0;
				foreach ( KeyValuePair<string, List<DeviceDescription>> provisioningEntry in provisioningData )
				{
					iotHubConnectionStrings.TryGetValue( provisioningEntry.Key,
						out string iotHubConnectionString );

					foreach ( DeviceDescription device in provisioningEntry.Value )
					{
						serviceCount++;
						string deviceType = device.name.ToLower();
						string serviceKey = $"sh.d.{deviceType}.{provisioningEntry.Key.ToLower()}";

						var service = new ServiceDescription
						{
							image = GetContainerImageName( deviceType ),
							environment = new List<EnvironmentSetting>()
						};
						service.environment.Add( new EnvironmentSetting { name = HardwareIdSetting, value = device.hardwareId } );
						service.environment.Add( new EnvironmentSetting { name = DigitalTwinsManagementApiSetting, value = DigitalTwinsApiEndpoint } );
						service.environment.Add( new EnvironmentSetting
						{
							name = MessageIntervalSetting,
							value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString()
						} );
						service.environment.Add( new EnvironmentSetting { name = SasTokenSetting, value = device.SasToken } );
						service.environment.Add( new EnvironmentSetting { name = IoTHubConnectionStringSetting, value = iotHubConnectionString } );
						service.environment.Add( new EnvironmentSetting
						{
							name = StartupDelayInSecondsSetting,
							value = Math.Floor( serviceCount / DigitalTwinsApiCallLimiter ).ToString()
						} );

						outline.services.Add( serviceKey, service );
					}
				}
			} );
		}
		#endregion

		private string FormatContainerImageName( string image )
		{
			if ( string.IsNullOrWhiteSpace( image ) )
				return image;

			var imageParts = image.Split( '/' );

			string registry = ContainerRegistry;

			if ( !ContainerRegistry.ToLower().EndsWith( ".azurecr.io" ) )
				registry = ContainerRegistry + ".azurecr.io";

			return $"{registry}/{imageParts[1]}";
		}

		private string GetContainerImageName( string deviceType )
		{
			return $"{ContainerRegistry}.azurecr.io/device-{deviceType}:public";
		}
	}
}
