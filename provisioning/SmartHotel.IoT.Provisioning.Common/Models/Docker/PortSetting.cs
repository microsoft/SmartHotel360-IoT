using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common.Models.Docker
{
    public class PortSetting : IYamlConvertible
    {
        public string name { get; set; }
        public string value { get; set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            var scalar = parser.Allow<Scalar>();

            if (scalar != null)
            {
                var parsedValue = scalar.Value;
                var data = parsedValue.Split(':');
                name = data[0];
                value = data[1];
            }
            else
            {
                Console.WriteLine(nestedObjectDeserializer(typeof(string)));
            }
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            emitter.Emit(new Scalar(null, null, string.Format("{0}:{1}", name, value), ScalarStyle.DoubleQuoted, true, true));
        }
    }
}
