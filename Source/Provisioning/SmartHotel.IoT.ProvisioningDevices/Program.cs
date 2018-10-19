using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SmartHotel.IoT.Provisioning.Common;
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
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.ProvisioningDevices
{
    public class Program
    {
        private static readonly string DigitalTwinsManagementApiSetting = "ManagementApiUrl";
        private static readonly string MessageIntervalSetting = "MessageIntervalInMilliSeconds";
        private static readonly string SensorTypeSetting = "SensorDataType";
        private static readonly string SasTokenSetting = "SasToken";
        private static readonly string HardwareIdSetting = "HardwareId";
        private static readonly string IoTHubConnectionStringSetting = "IoTHubDeviceConnectionString";

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private const int MessageIntervalDefault = 5000;

        [Option("-i|--Input", Description = "Output from the Provisioning App")]
        [Required]
        public string ProvisioningFile { get; }

        [Option("-iot|--IoTInformation", Description = "IoTHub connection string information file")]
        [Required]
        public string IoTConnectionFile { get; }

        [Option("-dt|--DigitalTwinsApiEndpoint", Description = "Url for your Digital Twins resource e.g. (https://{resource name}.{resource location}.azuresmartspaces.net/management/api/v1.0")]
        [Required]
        public string DigitalTwinsApiEndpoint { get; }

        [Option("-mi|--MessageInterval", Description = "How often to push sensor data to Digital Twins")]
        public int MessageInterval { get; }

        [Option("-d|--Directory", Description = "The directory containing the Devices' yaml files to provision")]
        [Required]
        public string DataDirectory { get; }

        [Option("-cr|--ContainerRegistry", Description = "The name or url of your Azure Container Registry")]
        [Required]
        public string ContainerRegistry { get; }

        private string GetDockerBasePath()
        {
            var path = Path.Combine(DataDirectory, "docker-compose.yml");
            return Path.GetFullPath(path);
        }

        private string GetDockerDemoPath()
        {
            var path = Path.Combine(DataDirectory, "docker-compose.demo.yml");
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
            ConsoleSpinner spinner = new ConsoleSpinner();

            ProvisioningData provisioningData = await LoadProvisioningData();

            Console.Write("Processing Provisioning Data, please wait ");

            Task baseTask = ProcessDocker(GetDockerBasePath(), provisioningData);
            Task demoTask = ProcessDocker(GetDockerDemoPath(), provisioningData);
            Task overrideTask = ProcessDocker(GetDockerOverridePath(), provisioningData);
            Task kubernetesTask = ProcessKubernetes(GetKubernetesPath(), provisioningData);
            Task kubernetesDemoTask = ProcessKubernetes(GetKubernetesDemoPath(), provisioningData);

            await Task.WhenAll(new[] { baseTask, demoTask, overrideTask, kubernetesTask, kubernetesDemoTask });

            Console.WriteLine();
            Console.WriteLine();
        }

        private async Task<ProvisioningData> LoadProvisioningData()
        {
            string content = await File.ReadAllTextAsync(ProvisioningFile);
            return JsonConvert.DeserializeObject<ProvisioningData>(content);
        }

        #region Kubernetes
        private async Task ProcessKubernetes(string path, ProvisioningData provisioningData)
        {
            var outline = await LoadKubernetesOutline(path);

            await ProcessKubernetesProvisioningData(outline, provisioningData);

            await SaveKubernetesOutline(path, outline);
        }

        private async Task<KubernetesOutline> LoadKubernetesOutline(string path)
        {
            return await Task.Run<KubernetesOutline>(() =>
            {
                return KubernetesOutline.FromFile(path);
            });
        }

        private async Task ProcessKubernetesProvisioningData(KubernetesOutline outline, ProvisioningData provisioningData)
        {
            await Task.Run(async () =>
            {
                string iotFile = await File.ReadAllTextAsync(IoTConnectionFile);
                Dictionary<string, Dictionary<string, string>> iotInfo = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(iotFile);

                foreach (var deployment in outline.Deployments)
                {
                    try
                    {
                        var metadata = deployment.metadata;

                        if (metadata != null)
                        {
                            var qualifiers = metadata.name.Split('.');
                            var room = qualifiers.Last().ToLower();

                            if (provisioningData.ContainsKey(room))
                            {
                                foreach (var container in deployment.spec.template.spec.containers)
                                {
                                    var apiUrlSetting = container.env.FirstOrDefault(e => e.name == DigitalTwinsManagementApiSetting);
                                    if (apiUrlSetting != null)
                                    {
                                        apiUrlSetting.value = DigitalTwinsApiEndpoint;
                                    }

                                    var messageIntervalSetting = container.env.FirstOrDefault(e => e.name == MessageIntervalSetting);
                                    if (messageIntervalSetting != null)
                                    {
                                        messageIntervalSetting.value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString();
                                    }

                                    var containerImage = FormatContainerImageName(container.image);
                                    container.image = containerImage;

                                    var typeSetting = container.env.FirstOrDefault(s => s.name == SensorTypeSetting);

                                    if (typeSetting != null)
                                    {
                                        if (iotInfo.ContainsKey(room))
                                        {
                                            Dictionary<string, string> iotConnectionStrings = iotInfo[room];

                                            if (iotConnectionStrings.ContainsKey(typeSetting.value.ToLower()))
                                            {
                                                var iotSetting = container.env.FirstOrDefault(s => s.name == IoTHubConnectionStringSetting);

                                                if (iotSetting != null)
                                                {
                                                    iotSetting.value = iotConnectionStrings[typeSetting.value.ToLower()];
                                                }
                                            }
                                        }

                                        DeviceDescription device = provisioningData[room].FirstOrDefault(d => d.sensors.Any(s => s.dataType.ToLower() == typeSetting.value.ToLower()));
                                        if (device != null)
                                        {
                                            var sasTokenSetting = container.env.FirstOrDefault(e => e.name == SasTokenSetting);
                                            if (sasTokenSetting != null)
                                            {
                                                sasTokenSetting.value = device.SasToken;
                                            }

                                            var hardwareIdSetting = container.env.FirstOrDefault(e => e.name == HardwareIdSetting);
                                            if (hardwareIdSetting != null)
                                            {
                                                hardwareIdSetting.value = device.hardwareId;
                                            }
                                        }
                                    }
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

            var yamlSerializer = new SerializerBuilder().WithTypeConverter(new KubernetesTypeConverter()).Build();

            foreach (var deployment in outline.Deployments)
            {
                output += yamlSerializer.Serialize(deployment);
                output += "---\n";
            }

            await File.WriteAllTextAsync(path, output);
        }
        #endregion

        #region Docker
        private async Task ProcessDocker(string path, ProvisioningData provisioningData)
        {
            var dockerOutline = await LoadDockerOutline(path);

            await ProcessDockerProvisioningData(dockerOutline, provisioningData);

            await SaveDockerOutline(path, dockerOutline);
        }

        private async Task<DockerOutline> LoadDockerOutline(string path)
        {
            var yamlDeserializer = new Deserializer();

            string content = await File.ReadAllTextAsync(path);
            return yamlDeserializer.Deserialize<DockerOutline>(content);
        }

        private async Task SaveDockerOutline(string path, DockerOutline outline)
        {
            var yamlSerializer = new Serializer();
            var output = yamlSerializer.Serialize(outline);

            await File.WriteAllTextAsync(path, output);
        }

        private async Task ProcessDockerProvisioningData(DockerOutline outline, ProvisioningData provisioningData)
        {
            await Task.Run(async () =>
            {
                string iotFile = await File.ReadAllTextAsync(IoTConnectionFile);
                Dictionary<string, Dictionary<string, string>> iotInfo = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(iotFile);

                foreach (var kvPair in outline.services)
                {
                    var service = kvPair.Value;

                    var containerImage = FormatContainerImageName(service.image);
                    service.image = containerImage;

                    if (service.environment != null)
                    {
                        var apiUrlSetting = service.environment.FirstOrDefault(e => e.name == DigitalTwinsManagementApiSetting);
                        if (apiUrlSetting != null)
                        {
                            apiUrlSetting.value = DigitalTwinsApiEndpoint;
                        }

                        var messageIntervalSetting = service.environment.FirstOrDefault(e => e.name == MessageIntervalSetting);
                        if (messageIntervalSetting != null)
                        {
                            messageIntervalSetting.value = MessageInterval > 0 ? MessageInterval.ToString() : MessageIntervalDefault.ToString();
                        }

                        var qualifiers = kvPair.Key.Split('.');
                        var name = qualifiers.Last().ToLower();

                        if (provisioningData.ContainsKey(name))
                        {
                            var serviceType = service.environment.FirstOrDefault(e => e.name == SensorTypeSetting);
                            if (serviceType != null)
                            {
                                if (iotInfo.ContainsKey(name))
                                {
                                    Dictionary<string, string> iotConnectionStrings = iotInfo[name];

                                    if (iotConnectionStrings.ContainsKey(serviceType.value.ToLower()))
                                    {
                                        var iotSetting = service.environment.FirstOrDefault(s => s.name == IoTHubConnectionStringSetting);

                                        if (iotSetting != null)
                                        {
                                            iotSetting.value = iotConnectionStrings[serviceType.value.ToLower()];
                                        }
                                    }
                                }

                                DeviceDescription device = provisioningData[name].FirstOrDefault(d => d.sensors.Any(s => s.dataType.ToLower() == serviceType.value.ToLower()));
                                if (device != null)
                                {
                                    var sasTokenSetting = service.environment.FirstOrDefault(e => e.name == SasTokenSetting);
                                    if (sasTokenSetting != null)
                                    {
                                        sasTokenSetting.value = device.SasToken;
                                    }

                                    var hardwareIdSetting = service.environment.FirstOrDefault(e => e.name == HardwareIdSetting);
                                    if (hardwareIdSetting != null)
                                    {
                                        hardwareIdSetting.value = device.hardwareId;
                                    }
                                }
                            }
                        }
                    }
                }
            });
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
