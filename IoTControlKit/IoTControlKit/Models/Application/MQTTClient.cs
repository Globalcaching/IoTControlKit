using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Models.Application
{
    [NPoco.TableName("MQTTClient")]
    public class MQTTClient: BasePoco
    {
        public string Name { get; set; }
        public string BaseTopic { get; set; }
        public bool Enabled { get; set; }
        public string MQTTType { get; set; }
        public string TcpServer { get; set; }
    }
}
