using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesTemplate : Dictionary<string, object>
    {
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
