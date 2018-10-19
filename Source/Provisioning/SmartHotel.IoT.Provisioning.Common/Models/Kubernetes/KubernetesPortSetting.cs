using System;
using System.Collections.Generic;
using System.Text;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesPortSetting : Dictionary<string, object>
    {
        public string name
        {
            get
            {
                return ContainsKey(nameof(name)) ? this[nameof(name)].ToString() : null;
            }
            set
            {
                this[nameof(name)] = value;
            }
        }

        public string containerPort
        {
            get
            {
                return ContainsKey(nameof(containerPort)) ? this[nameof(containerPort)].ToString() : null;
            }
            set
            {
                this[nameof(containerPort)] = value;
            }
        }

        public string protocol
        {
            get
            {
                return ContainsKey(nameof(protocol)) ? this[nameof(protocol)].ToString() : null;
            }
            set
            {
                this[nameof(protocol)] = value;
            }
        }

        public string port
        {
            get
            {
                return ContainsKey(nameof(port)) ? this[nameof(port)].ToString() : null;
            }
            set
            {
                this[nameof(port)] = value;
            }
        }

        public string targetPort
        {
            get
            {
                return ContainsKey(nameof(targetPort)) ? this[nameof(targetPort)].ToString() : null;
            }
            set
            {
                this[nameof(targetPort)] = value;
            }
        }
    }
}
