using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace SmartHotel.IoT.Provisioning.Common.Models.Kubernetes
{
    public class KubernetesTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(String);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            if (value == null)
                return;

            int result;
            if (int.TryParse(value.ToString(), out result))
            {
                //This is needed because Kubernetes expects numbers to be quoted
                emitter.Emit(new Scalar(null, null, value.ToString(), ScalarStyle.DoubleQuoted, true, true));
            }
            else
            {
                emitter.Emit(new Scalar(null, value.ToString()));
            }
        }
    }
}
