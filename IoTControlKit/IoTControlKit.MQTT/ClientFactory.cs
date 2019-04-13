using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Plugin.MQTT
{
    public class ClientFactory
    {
        public static Client CreateClient(MQTTClient poco)
        {
            Client result = null;
            if (poco.MQTTType == "homie")
            {
                result = new HomieClient(poco);
            }
            return result;
        }
    }
}
