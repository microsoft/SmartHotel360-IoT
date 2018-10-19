using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesContainer : Dictionary<string, object>
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

        public string image
        {
            get
            {
                return ContainsKey(nameof(image)) ? this[nameof(image)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(image)] = value;
            }
        }

        public string imagePullPolicy
        {
            get
            {
                return ContainsKey(nameof(imagePullPolicy)) ? this[nameof(imagePullPolicy)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(imagePullPolicy)] = value;
            }
        }

        public IList<KubernetesEnvironmentSetting> env
        {
            get
            {
                return ContainsKey(nameof(env)) ? this[nameof(env)] as IList<KubernetesEnvironmentSetting> : null;
            }
            set
            {
                this[nameof(env)] = value;
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
