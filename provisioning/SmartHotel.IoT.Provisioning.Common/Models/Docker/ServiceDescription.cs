using System.Collections.Generic;

namespace SmartHotel.IoT.Provisioning.Common.Models.Docker
{
    public class ServiceDescription
    {
        public string image { get; set; }
        public IList<EnvironmentSetting> environment { get; set; }
        public BuildDescription build { get; set; }
        public IList<PortSetting> ports { get; set; }
    }
}
