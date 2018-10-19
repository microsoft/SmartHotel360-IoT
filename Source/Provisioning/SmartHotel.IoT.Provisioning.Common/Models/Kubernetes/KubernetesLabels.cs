using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesLabels : Dictionary<string, object>
    {
        public string app
        {
            get
            {
                return ContainsKey(nameof(app)) ? this[nameof(app)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(app)] = value;
            }
        }

        public string component
        {
            get
            {
                return ContainsKey(nameof(component)) ? this[nameof(component)].ToString() : string.Empty;
            }
            set
            {
                this[nameof(component)] = value;
            }
        }
    }
}
