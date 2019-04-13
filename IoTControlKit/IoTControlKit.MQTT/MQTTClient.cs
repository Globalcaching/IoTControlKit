using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Plugin.MQTT
{
    [NPoco.TableName("MQTTClient")]
    public class MQTTClient: Framework.Models.BasePoco
    {
        public long DeviceControllerId { get; set; }
        public string Name { get; set; }
        public string BaseTopic { get; set; }
        public string MQTTType { get; set; }
        public string TcpServer { get; set; }
    }
}
