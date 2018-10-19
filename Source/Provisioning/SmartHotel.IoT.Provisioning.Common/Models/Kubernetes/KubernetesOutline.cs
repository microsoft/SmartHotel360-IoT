using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesOutline
    {
        public List<KubernetesDeployment> Deployments { get; private set; }

        public KubernetesOutline()
        {
            Deployments = new List<KubernetesDeployment>();
        }

        public static KubernetesOutline FromFile(string path)
        {
            var outline = new KubernetesOutline();

            try
            {
                using (var file = File.OpenText(path))
                {
                    var stream = new YamlStream();
                    stream.Load(file);

                    foreach (var document in stream.Documents)
                    {
                        if (document.RootNode is YamlMappingNode)
                        {
                            var deployment = new KubernetesDeployment();
                            var mapping = (YamlMappingNode)document.RootNode;

                            foreach (var entry in mapping.Children)
                            {
                                ReadMapping(entry, deployment);
                            }

                            outline.Deployments.Add(deployment);
                        }
                        else if (document.RootNode is YamlSequenceNode)
                        {
                            var sequence = (YamlSequenceNode)document.RootNode;

                            foreach (var entry in sequence.Children)
                            {
                                var deployment = new KubernetesDeployment();
                                ReadNode(entry, deployment);
                                outline.Deployments.Add(deployment);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return outline;
        }

        private static void ReadMapping(KeyValuePair<YamlNode, YamlNode> mapping, IDictionary<string, object> output)
        {
            var key = ((YamlScalarNode)mapping.Key).Value;

            if (mapping.Value is YamlScalarNode)
            {
                var value = ((YamlScalarNode)mapping.Value).Value;
                output.Add(key, value);
            }
            else if (mapping.Value is YamlMappingNode)
            {
                var node = mapping.Value as YamlMappingNode;
                var dict = CreateKubernetesType(key);

                foreach (var child in node.Children)
                    ReadMapping(child, dict);

                output[key] = dict;
            }
            else if (mapping.Value is YamlSequenceNode)
            {
                var itemType = CreateKubernetesType(key);
                var node = mapping.Value as YamlSequenceNode;

                var listType = typeof(List<>).MakeGenericType(itemType.GetType());
                System.Collections.IList items = (System.Collections.IList)Activator.CreateInstance(listType);

                foreach (var child in node.Children)
                {
                    var dict = CreateKubernetesType(key);
                    ReadNode(child, dict);
                    items.Add(dict);
                }

                output[key] = items;
            }
        }

        private static void ReadNode(YamlNode node, IDictionary<string, object> output)
        {
            if (node.NodeType == YamlNodeType.Mapping)
            {
                var mapping = node as YamlMappingNode;

                foreach (var child in mapping.Children)
                    ReadMapping(child, output);
            }
        }

        private static IDictionary<string, object> CreateKubernetesType(string type)
        {
            IDictionary<string, object> obj = null;

            switch (type)
            {
                case "metadata":
                    obj = new KubernetesMetadata();
                    break;
                case "spec":
                    obj = new KubernetesSpec();
                    break;
                case "template":
                    obj = new KubernetesTemplate();
                    break;
                case "labels":
                    obj = new KubernetesLabels();
                    break;
                case "containers":
                    obj = new KubernetesContainer();
                    break;
                case "env":
                    obj = new KubernetesEnvironmentSetting();
                    break;
                case "ports":
                    obj = new KubernetesPortSetting();
                    break;
                case "selector":
                    obj = new KubernetesSelector();
                    break;
            }

            return obj;
        }
    }
}
