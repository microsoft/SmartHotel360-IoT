using System;
using System.Collections.Generic;
using System.Text;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesSelector : Dictionary<string, object>
    {
        public string app
        {
            get
            {
                return ContainsKey(nameof(app)) ? this[nameof(app)].ToString() : null;
            }
            set
            {
                this[nameof(app)] = value;
            }
        }
    }
}
