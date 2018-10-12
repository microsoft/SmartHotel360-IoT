using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesMetadata : Dictionary<string, object>
    {
        public string name
        {
            get
            {
                return ContainsKey(nameof(name)) ? this[nameof(name)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(name)] = value;
            }
        }

        public KubernetesLabels labels
        {
            get
            {
                return ContainsKey(nameof(labels)) ? this[nameof(labels)] as KubernetesLabels : null;
            }
            set
            {
                this[nameof(labels)] = value;
            }
        }
    }
}
