using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesEnvironmentSetting : Dictionary<string, object>
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

        public string value
        {
            get
            {
                return ContainsKey(nameof(value)) ? this[nameof(value)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(this.value)] = value;
            }
        }
    }
}
