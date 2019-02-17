using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;

namespace IoTControlKit.Services.MQTT
{
    public class HomieClient: Client
    {
        public HomieClient(Models.Application.MQTTClient poco)
            : base(poco)
        {
        }

        protected override void ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            base.ApplicationMessageReceived(sender, e);

            //todo
        }
    }
}
