using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesDeployment : Dictionary<string, object>
    {
        public string apiVersion
        {
            get
            {
                return ContainsKey(nameof(apiVersion)) ? this[nameof(apiVersion)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(apiVersion)] = value;
            }
        }

        public string kind
        {
            get
            {
                return ContainsKey(nameof(kind)) ? this[nameof(kind)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(kind)] = value;
            }
        }

        public KubernetesMetadata metadata
        {
            get
            {
                return ContainsKey(nameof(metadata)) ? this[nameof(metadata)] as KubernetesMetadata : null;
            }
            set
            {
                this[nameof(metadata)] = value;
            }
        }

        public KubernetesSpec spec
        {
            get
            {
                return ContainsKey(nameof(spec)) ? this[nameof(spec)] as KubernetesSpec : null;

            }
            set
            {
                this[nameof(spec)] = value;
            }
        }
    }
}
