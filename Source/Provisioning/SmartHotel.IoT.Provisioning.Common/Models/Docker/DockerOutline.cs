using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common.Models.Docker
{
    public class DockerOutline
    {
        [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string version { get; set; }

        public Dictionary<string, ServiceDescription> services { get; set; }
    }
}
