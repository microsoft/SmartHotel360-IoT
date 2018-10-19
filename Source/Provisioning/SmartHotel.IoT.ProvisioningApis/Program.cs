using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common;
using SmartHotel.IoT.Provisioning.Common.Models.Docker;
using SmartHotel.IoT.Provisioning.Common.Models.Kubernetes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.ProvisioningApis
{
    public class Program
    {
        private static readonly string DigitalTwinsManagementApiSetting = "ManagementApiUrl";
        private static readonly string IoTHubConnectionStringSetting = "IoTHubConnectionString";
        private static readonly string MongoDBSetting = "MongoDBConnectionString";
        private static readonly string DatabaseSetting = "DatabaseConnectionString";

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        [Option("-d|--Directory", Description = "The directory containing the API(s) yaml files to provision")]
        [Required]
        public string DataDirectory { get; }

        [Option("-cr|--ContainerRegistry", Description = "The name or url of your Azure Container Registry")]
        [Required]
        public string ContainerRegistry { get; }

        [Option("-dt|--DigitalTwinsApiEndpoint", Description = "Url for your Digital Twins resource e.g. (https://{resource name}.{resource location}.azuresmartspaces.net/management/api/v1.0")]
        [Required]
        public string DigitalTwinsApiEndpoint { get; }

        [Option("-db|--DatabaseConnectionString", Description = "Connection string to your Azure Cosmos DB (MongoDB)")]
        [Required]
        public string DatabaseConnectionString { get; }

        [Option("-iot|--IoTHubConnectionString", Description = "Connection string to your IoTHub")]
        [Required]
        public string IoTHubConnectionString { get; }

        private string GetDockerBasePath()
        {
            var path = Path.Combine(DataDirectory, "docker-compose.yml");
            return Path.GetFullPath(path);
        }

        private string GetDockerOverridePath()
        {
            var path = Path.Combine(DataDirectory, "docker-compose.override.yml");
            return Path.GetFullPath(path);
        }

        private string GetKubernetesPath()
        {
            var path = Path.Combine(DataDirectory, "deployments.yaml");
            return Path.GetFullPath(path);
        }

	    private string GetKubernetesDemoPath()
	    {
		    var path = Path.Combine(DataDirectory, "deployments.demo.yaml");
		    return Path.GetFullPath(path);
	    }

        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

        private async Task OnExecuteAsync()
        {
            Console.Write("Processing Provisioning Data, please wait ");

            Task baseTask = ProcessDocker(GetDockerBasePath());
            Task overrideTask = ProcessDocker(GetDockerOverridePath());
            Task kubernetesTask = ProcessKubernetes(GetKubernetesPath());
            Task kubernetesDemoTask = ProcessKubernetes(GetKubernetesDemoPath());

            await Task.WhenAll(new[] { baseTask, overrideTask, kubernetesTask, kubernetesDemoTask });

            Console.WriteLine();
            Console.WriteLine();
        }

        #region Docker
        private async Task ProcessDocker(string path)
        {
            var dockerOutline = await LoadDockerOutline(path);

            await ProcessDockerOutline(dockerOutline);

            await SaveDockerOutline(path, dockerOutline);
        }

        private async Task<DockerOutline> LoadDockerOutline(string path)
        {
            var yamlDeserializer = new Deserializer();

            string content = await File.ReadAllTextAsync(path);
            return yamlDeserializer.Deserialize<DockerOutline>(content);
        }

        private async Task ProcessDockerOutline(DockerOutline outline)
        {
            await Task.Run(() =>
            {
                foreach (var kvPair in outline.services)
                {
                    var service = kvPair.Value;

                    var containerImage = FormatContainerImageName(service.image);
                    service.image = containerImage;

                    if (service.environment != null)
                    {
                        var dtApiSetting = service.environment.FirstOrDefault(s => s.name == DigitalTwinsManagementApiSetting);
                        if (dtApiSetting != null)
                        {
                            dtApiSetting.value = DigitalTwinsApiEndpoint;
                        }

                        var iotSetting = service.environment.FirstOrDefault(s => s.name == IoTHubConnectionStringSetting);
                        if (iotSetting != null)
                        {
                            iotSetting.value = IoTHubConnectionString;
                        }

                        var dbSetting = service.environment.FirstOrDefault(s => s.name == DatabaseSetting);
                        if (dbSetting != null)
                        {
                            dbSetting.value = DatabaseConnectionString;
                        }

                        var mongoSetting = service.environment.FirstOrDefault(s => s.name == MongoDBSetting);
                        if (mongoSetting != null)
                        {
                            mongoSetting.value = DatabaseConnectionString;
                        }
                    }
                }
            });
        }

        private async Task SaveDockerOutline(string path, DockerOutline outline)
        {
            var yamlSerializer = new Serializer();
            var output = yamlSerializer.Serialize(outline);

            await File.WriteAllTextAsync(path, output);
        }
        #endregion

        #region Kubernetes
        private async Task ProcessKubernetes(string path)
        {
            var dockerOutline = await LoadKubernetesOutline(path);

            await ProcessKubernetesOutline(dockerOutline);

            await SaveKubernetesOutline(path, dockerOutline);
        }

        private async Task<KubernetesOutline> LoadKubernetesOutline(string path)
        {
            return await Task.Run<KubernetesOutline>(() =>
            {
                return KubernetesOutline.FromFile(path);
            });
        }

        private async Task ProcessKubernetesOutline(KubernetesOutline outline)
        {
            await Task.Run(() =>
            {
                foreach (var deployment in outline.Deployments)
                {
                    try
                    {
                        if (deployment.spec.template == null)
                            continue;

                        foreach (var container in deployment.spec.template.spec.containers)
                        {
                            var containerImage = FormatContainerImageName(container.image);
                            container.image = containerImage;

                            if (container.env != null)
                            {
                                var dtApiSetting = container.env.FirstOrDefault(s => s.name == DigitalTwinsManagementApiSetting);
                                if (dtApiSetting != null)
                                {
                                    dtApiSetting.value = DigitalTwinsApiEndpoint;
                                }

                                var iotSetting = container.env.FirstOrDefault(s => s.name == IoTHubConnectionStringSetting);
                                if (iotSetting != null)
                                {
                                    iotSetting.value = IoTHubConnectionString;
                                }

                                var dbSetting = container.env.FirstOrDefault(s => s.name == DatabaseSetting);
                                if (dbSetting != null)
                                {
                                    dbSetting.value = DatabaseConnectionString;
                                }

                                var mongoSetting = container.env.FirstOrDefault(s => s.name == MongoDBSetting);
                                if (mongoSetting != null)
                                {
                                    mongoSetting.value = DatabaseConnectionString;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }

        private async Task SaveKubernetesOutline(string path, KubernetesOutline outline)
        {
            string output = string.Empty;

            var yamlSerializer = new SerializerBuilder().Build();

            foreach (var deployment in outline.Deployments)
            {
                output += yamlSerializer.Serialize(deployment);
                output += "---\n";
            }

            await File.WriteAllTextAsync(path, output);
        }
        #endregion

        private string FormatContainerImageName(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
                return image;

            var imageParts = image.Split('/');

            string registry = ContainerRegistry;

            if (!ContainerRegistry.ToLower().EndsWith(".azurecr.io"))
                registry = ContainerRegistry + ".azurecr.io";

            return $"{registry}/{imageParts[1]}";
        }
    }
}
