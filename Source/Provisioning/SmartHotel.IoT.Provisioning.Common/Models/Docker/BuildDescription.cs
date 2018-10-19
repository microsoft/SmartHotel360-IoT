using System;
using System.Collections.Generic;
using System.Text;

namespace SmartHotel.IoT.Provisioning.Common.Models.Docker
{
    public class BuildDescription
    {
        public string context { get; set; }
        public string dockerfile { get; set; }
    }
}
