using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesSpec : Dictionary<string, object>
    {
        public string type
        {
            get
            {
                return ContainsKey(nameof(type)) ? this[nameof(type)].ToString() : null;
            }
            set
            {
                this[nameof(type)] = value;
            }
        }

        public KubernetesTemplate template
        {
            get
            {
                return ContainsKey(nameof(template)) ? this[nameof(template)] as KubernetesTemplate : null;
            }
            set
            {
                this[nameof(template)] = value;
            }
        }

        public IList<KubernetesContainer> containers
        {
            get
            {
                return ContainsKey(nameof(containers)) ? this[nameof(containers)] as IList<KubernetesContainer> : null;
            }
            set
            {
                this[nameof(containers)] = value;
            }
        }

        public KubernetesSelector selector
        {
            get
            {
                return ContainsKey(nameof(selector)) ? this[nameof(selector)] as KubernetesSelector : null;
            }
            set
            {
                this[nameof(selector)] = value;
            }
        }

        public IList<KubernetesPortSetting> ports
        {
            get
            {
                return ContainsKey(nameof(ports)) ? this[nameof(ports)] as IList<KubernetesPortSetting> : null;
            }
            set
            {
                this[nameof(ports)] = value;
            }
        }
    }
}
