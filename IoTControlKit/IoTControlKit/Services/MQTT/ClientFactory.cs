using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.MQTT
{
    public class ClientFactory
    {
        public static Client CreateClient(Models.Application.MQTTClient poco)
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
